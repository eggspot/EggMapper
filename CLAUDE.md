# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What is EggMapper

EggMapper is a high-performance .NET object-to-object mapping library targeting **zero runtime reflection** and **zero extra allocations**. All mapping delegates are compiled as expression trees during `MapperConfiguration` construction. The goal is to be **the fastest runtime mapper** — faster than AutoMapper, Mapster, and AgileMapper on every benchmark scenario.

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

### Compilation Pipeline (Configuration Time)

1. `MapperConfiguration` constructor receives user-defined maps via `CreateMap<S,D>()`
2. `TopologicalOrder()` sorts type maps by dependency (child types first)
3. `ExpressionBuilder.BuildMappingDelegate()` compiles each type map into one of three delegate types:
   - **Fast typed path** (`TryBuildTypedDelegate`): Single expression tree block with inlined nested maps and flattening — no boxing, no per-property delegates
   - **Flexible path** (`BuildFlexibleDelegate`): Per-property action arrays for complex features (conditions, hooks, inheritance, MaxDepth)
   - **Context-free path** (`TryBuildCtxFreeDelegate`): `Func<TSource, TDestination>` with fully inlined nested objects, collections, and flattening — used by both `Map<S,D>()` and `MapList<>()`
4. `TryBuildCtxFreeListDelegate()` compiles `Func<IList<TSource>, List<TDestination>>` — entire collection loop + element mapping as a single expression tree
5. Compiled delegates stored in `FrozenMaps`, `FrozenCtxFreeMaps`, and `FrozenCtxFreeListMaps`

### Runtime (Map Time)

- `Mapper.Map<S,D>()` checks `TypePairCache<S,D>` first (static generic class — zero dict lookup after warm-up)
- Falls back to `FrozenCtxFreeMaps` for typed `Func<TSource, TDestination>` (zero boxing)
- Falls back to `FrozenMaps` with thread-static `ResolutionContext` pooling
- `MapList<S,D>()` checks `ListCache<S,D>` → `FrozenCtxFreeListMaps` for fully-inlined collection delegates
- `IList<T>` sources use index-based `for` loops (avoids enumerator allocation)

### Key Performance Techniques

- **Inlined nested maps**: Child type property assignments are emitted directly into the parent expression tree (no delegate call, no boxing)
- **Inlined flattening**: `dest.AddressStreet = src.Address.Street` compiled as direct typed property access
- **Inlined collection loops**: Entire `List<T>` mapping loop compiled as single expression tree with inline element mapping
- **Static generic caching**: `TypePairCache<TSource, TDestination>` and `ListCache<TSource, TDestination>` eliminate dict lookups

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

### Benchmark Competitors

| Library | Type | Package |
|---------|------|---------|
| AutoMapper 16.x | Runtime | `AutoMapper` |
| Mapster 7.x | Runtime | `Mapster` |
| Mapperly 4.x | Source generator (compile-time) | `Riok.Mapperly` |
| AgileMapper 1.x | Runtime | `AgileObjects.AgileMapper` |

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

Fully automatic — every push to `main` triggers a release with version bump detected from commit messages:

1. Push / merge to `main`
2. CI analyzes commit messages since last tag using conventional commits:
   - `fix:` / `perf:` / `chore:` → **patch** bump (1.1.0 → 1.1.1)
   - `feat:` → **minor** bump (1.1.0 → 1.2.0)
   - `BREAKING CHANGE` / `feat!:` → **major** bump (1.1.0 → 2.0.0)
3. Bumps version in csproj → builds → tests → packs → publishes to NuGet
4. Creates git tag `v<version>` + GitHub Release with artifacts
5. Commits the bumped csproj back to main with `[skip ci]`

No manual version editing needed. Just use conventional commit prefixes.

## Working Style

- **NEVER push directly to `main`** — always create a feature branch and open a PR
- Break complex tasks into smaller incremental commits
- A single PR should focus on one logical change
- Commit and push to the feature branch after each verified, self-contained unit of work
- Use conventional commit prefixes (`feat:`, `fix:`, `perf:`, `chore:`, `docs:`) — the publish pipeline auto-detects version bumps from these
