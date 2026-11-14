# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What is EggMapper

EggMapper is a high-performance .NET object-to-object mapping library targeting **zero runtime reflection** and **zero extra allocations**. All mapping delegates are compiled as expression trees, each type pair on its first use (or all at once via `MapperConfiguration.CompileMappings()`). The goal is to be **the fastest runtime mapper** — faster than AutoMapper, Mapster, and AgileMapper on every benchmark scenario.

## Claude Skills

Shorthand slash commands available in this project:

| Skill | Description |
|-------|-------------|
| `/test [filter]` | Run unit tests, optionally filtered by class/method name |
| `/bench [filter]` | Run benchmarks and compare EggMapper vs all competitors |
| `/feat <description>` | Implement a new feature following the correctness→performance loop |
| `/perf-check` | Verify no regression after code changes |
| `/ship` | Create a PR with benchmark evidence |
| `/expr-debug <types>` | Inspect the compiled expression tree for a type pair |
| `/pr [action]` | List open PRs, check CI, or merge |

## Commands

```bash
# Build
dotnet build --configuration Release

# Run all unit tests
dotnet test --configuration Release

# Run tests for a specific class (by fully qualified name or filter)
dotnet test src/EggMapper.UnitTests/EggMapper.UnitTests.csproj --configuration Release --filter "FullyQualifiedName~BasicFlatteningTests"

# Run all benchmarks on .NET 10 (takes several minutes)
cd src/EggMapper.Benchmarks && dotnet run -c Release -f net10.0 -- --filter * --exporters json markdown

# Run a single benchmark class
cd src/EggMapper.Benchmarks && dotnet run -c Release -f net10.0 -- --filter *FlatMappingBenchmark*

# Quick smoke-test benchmark (short runs, not accurate for perf comparison)
cd src/EggMapper.Benchmarks && dotnet run -c Release -f net10.0 -- --filter *FlatMappingBenchmark* --job short
```

## Architecture

### Compilation Pipeline (Lazy, per Type Pair)

1. `MapperConfiguration` constructor registers user-defined maps (`CreateMap<S,D>()`, profiles) and resolves
   `IncludeAllDerived` / `IncludeBase` chains. It compiles nothing (startup ~tens of µs).
2. The first request for a pair (`Map`, `MapList` element fallback, `GetMappingExpressionText`, a nested lookup)
   calls `MapperConfiguration.TryGetOrCompile(key)`: under one per-configuration lock it compiles the pair into one of
   three delegate types and publishes it in an immutable `MapSnapshot` (copy-on-write, swapped through one volatile
   field, so readers keep plain `Dictionary` lookups):
   - **Context-free path** (`TryBuildCtxFreeDelegate`): `Func<TSource, TDestination, TDestination>` with fully inlined
     nested objects, collections, and flattening — used by both `Map<S,D>()` and `MapList<>()`
   - **Fast typed path** (`TryBuildTypedDelegate`): single expression tree block with inlined nested maps and
     flattening — no boxing, no per-property delegates
   - **Flexible path** (`BuildFlexibleDelegate`): per-property action arrays for complex features (conditions, hooks,
     inheritance, MaxDepth)
3. Child maps needed while building a parent compile depth-first in the same batch (a cycle hits the in-progress set
   and is resolved at run time). Delegates look children up at run time through `CompiledMapStore`, which compiles a
   registered pair on demand, so a lookup never silently misses.
4. A compile failure is cached and rethrown (same exception) on later requests for that pair; other pairs still work.
5. `CompileMappings()` compiles everything eagerly: context-free builds in parallel, then the rest in topological
   order; it throws the single failure or an `AggregateException`.
6. `TryBuildCtxFreeListDelegate()` compiles `Func<IList<TSource>, List<TDestination>>` lazily on first `MapList`.

Path eligibility (`ExpressionBuilder`): a map falls back from the context-free / typed paths to the flexible path
when it has validation rules (`Validate`), a constructor that needs the `ResolutionContext`, member conditions
(`Condition`, `PreCondition`), `NullSubstitute`, `UseDestinationValue`, value resolvers, `BeforeMap`/`AfterMap`,
`MaxDepth` or `IncludeBase`. Collection elements are inlined only when their element pair has a registered map.
Constructor matching (records, parameterized constructors) requires at least one parameter matched to a source
member. `UseDestinationValue` leaves the existing destination value in place. When a nested value has no registered
map, the runtime helpers fall through to type conversion.

### Runtime (Map Time)

- `Mapper.Map<S,D>()` checks `FastCache<S,D>` first (static generic class — zero dict lookup after warm-up)
- Falls back to `FrozenCtxFreeMaps` for typed `Func<TSource, TDestination>` (zero boxing)
- Falls back to `FrozenMaps` with thread-static `ResolutionContext` pooling
- Base-type walk + interface walk in `MapInternal` for EF Core proxy / derived type resolution
- `MapList<S,D>()` checks `FastListCache<S,D>` → `FrozenCtxFreeListMaps` for fully-inlined collection delegates
- `IList<T>` sources use index-based `for` loops (avoids enumerator allocation)
- `FastCache`, `FastListCache` and `PatchCache` are static single-slot caches holding one immutable entry (delegate +
  configuration `Generation`) behind a single volatile reference, so the delegate and generation are always read
  together. The generation belongs to `MapperConfiguration`, not `Mapper`: every transient/scoped `Mapper` built from
  the same configuration shares the cache, and a second configuration simply misses and repopulates it.
- Only context-free delegates are cached in the typed caches; maps that need a `ResolutionContext` get it per call
  from `GetContext()`, which carries the calling `Mapper`'s `IServiceProvider` and `Items`.

### Key Performance Techniques

- **Inlined nested maps**: Child type property assignments are emitted directly into the parent expression tree (no delegate call, no boxing)
- **Inlined flattening**: `dest.AddressStreet = src.Address.Street` compiled as direct typed property access
- **Inlined collection loops**: Entire `List<T>` mapping loop compiled as single expression tree with inline element mapping
- **Static generic caching**: `FastCache<TSource, TDestination>` and `FastListCache<TSource, TDestination>` eliminate dict lookups

### Key Files

| File | Role |
|------|------|
| `src/EggMapper/Execution/ExpressionBuilder.cs` | Core compilation — builds all delegate paths, inlining, flattening |
| `src/EggMapper/Mapper.cs` | Public mapping API, static generic caches, thread-static context |
| `src/EggMapper/MapperConfiguration.cs` | Orchestrates compilation, stores frozen delegate dictionaries |
| `src/EggMapper/MapperConfigurationExpression.cs` | Fluent configuration API (`CreateMap`, profiles, `ForMember`) |
| `src/EggMapper/Internal/TypePair.cs` | Value-type dictionary key for source/dest type lookups |
| `src/EggMapper/Internal/TypeDetails.cs` | Cached reflection metadata (properties, constructors) |
| `src/EggMapper/Internal/ReflectionHelper.cs` | Utility: numeric/collection type detection, flattening |
| `src/EggMapper/ServiceCollectionExtensions.cs` | DI registration (`AddEggMapper`) — transient `IMapper`, singleton config |
| `src/EggMapper/ResolutionContext.cs` | Thread-static pooled context with DI `ServiceProvider` + cycle cache |

### Benchmark Competitors

| Library | Type | Package |
|---------|------|---------|
| AutoMapper 16.x | Runtime | `AutoMapper` |
| Mapster 7.x | Runtime | `Mapster` |
| Mapperly 4.x | Source generator (compile-time) | `Riok.Mapperly` |
| AgileMapper 1.x | Runtime | `AgileObjects.AgileMapper` |

Competitors are referenced only by `EggMapper.Benchmarks` (`IsPackable=false`), so they are never shipped in an EggMapper package. AutoMapper 16.x is licensed under RPL 1.5, which is acceptable only because it stays a benchmark-only dependency. Keep competitor versions free of known vulnerabilities, since Dependabot reports them against this repo.

## Development Loops

### Loop 1 — Correctness
Write code → write tests → `dotnet test --configuration Release` → fix failures → repeat until 100% green.

### Loop 2 — Performance
Record baseline benchmark → optimize → re-benchmark → compare → repeat until EggMapper beats all competitors on every scenario.

## Constraints

- **Zero runtime reflection** — no `PropertyInfo.GetValue/SetValue` in hot paths
- **Zero extra allocations** — match manual code allocation in every scenario
- **No LINQ in hot paths** — use `for` loops with pre-sized collections
- **`AggressiveInlining`** on `Map<S,D>()` and delegate lookup methods
- **Value-type `TypePair`** for dictionary keys (no boxing)
- **Inlined child mappers** embedded in parent expression trees (no delegate call overhead)
- **EggMapper must be fastest runtime mapper** on every benchmark before merging
- **Default benchmark target**: .NET 10 (`-f net10.0`)

## Testing Conventions

- Framework: **xUnit** + **FluentAssertions**
- Pattern: **AAA** (Arrange / Act / Assert)
- Naming: `Feature_Condition_ExpectedBehavior` (e.g., `Map_NullSource_ReturnsDefault`)
- Tests live in `src/EggMapper.UnitTests/`

## Release Process

Fully automatic tag-based versioning — every push to `main` triggers a release:

1. Push / merge to `main`
2. CI analyzes commit messages since last tag using conventional commits:
   - `fix:` / `perf:` / `chore:` → **patch** bump (1.1.0 → 1.1.1)
   - `feat:` → **minor** bump (1.1.0 → 1.2.0)
   - `BREAKING CHANGE` / `feat!:` → **major** bump (1.1.0 → 2.0.0)
3. Version derived from last git tag (not csproj) → builds with `-p:Version=` → tests → packs → publishes to NuGet
4. Creates git tag `v<version>` + GitHub Release with artifacts
5. Concurrency group serializes publish runs to prevent tag race conditions

No manual version editing needed. No commits pushed back to main. Just use conventional commit prefixes.

Publishing credentials: the publish workflow uses NuGet Trusted Publishing (GitHub OIDC exchanged for a 1-hour API
key via `NuGet/login`) when the repository variable `NUGET_USER` holds the nuget.org username (profile name, not
email) of the account that created the trusted-publishing policy for the `eggspot` organization. If the variable is
missing or the login fails, it falls back to the `NUGET_API_KEY` repository secret.

## Working Style

- **NEVER push directly to `main`** — always create a feature branch and open a PR
- Break complex tasks into smaller incremental commits
- A single PR should focus on one logical change
- Commit and push to the feature branch after each verified, self-contained unit of work
- Use conventional commit prefixes (`feat:`, `fix:`, `perf:`, `chore:`, `docs:`) — the publish pipeline auto-detects version bumps from these
