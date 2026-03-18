# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What is EggMapper

EggMapper is a high-performance .NET object-to-object mapping library targeting **zero runtime reflection**. All mapping delegates are compiled as expression trees during `MapperConfiguration` construction. The goal is to be **faster than AutoMapper and Mapster** on every benchmark scenario.

## Commands

```bash
# Build
dotnet build --configuration Release

# Run all unit tests
dotnet test --configuration Release

# Run tests for a specific class (by fully qualified name or filter)
dotnet test src/EggMapper.UnitTests/EggMapper.UnitTests.csproj --configuration Release --filter "FullyQualifiedName~BasicFlatteningTests"

# Run all benchmarks (takes several minutes)
cd src/EggMapper.Benchmarks && dotnet run -c Release -- --filter * --exporters json markdown

# Run a single benchmark class
cd src/EggMapper.Benchmarks && dotnet run -c Release -- --filter *FlatMappingBenchmark*

# Quick smoke-test benchmark (short runs, not accurate for perf comparison)
cd src/EggMapper.Benchmarks && dotnet run -c Release -- --filter *FlatMappingBenchmark* --job short
```

## Architecture

### Compilation Pipeline (Configuration Time)

1. `MapperConfiguration` constructor receives user-defined maps via `CreateMap<S,D>()`
2. `TopologicalOrder()` sorts type maps by dependency (child types first)
3. `ExpressionBuilder.BuildMappingDelegate()` compiles each type map into one of three delegate types:
   - **Fast typed path** (`TryBuildTypedDelegate`): Single expression tree block for simple maps — no boxing, no per-property delegates
   - **Flexible path** (`BuildFlexibleDelegate`): Per-property action arrays for complex features (conditions, hooks, inheritance)
   - **Context-free path** (`TryBuildCtxFreeDelegate`): `Func<TSource, TDestination>` for simple flat maps, used by `MapList<>` to skip `ResolutionContext` entirely
4. Compiled delegates stored in `FrozenMaps` (regular `Dictionary<TypePair, Delegate>`) and `FrozenCtxFreeMaps`

### Runtime (Map Time)

- `Mapper.Map<S,D>()` looks up the compiled delegate via `TypePair` (value-type struct key, no boxing)
- Thread-static `ResolutionContext` is pooled and reused (lazy allocation for cycle detection cache)
- `MapList<S,D>()` checks `FrozenCtxFreeMaps` first for zero-context-overhead collection mapping
- `IList<T>` sources use index-based `for` loops (avoids enumerator allocation)

### Key Files

| File | Role |
|------|------|
| `src/EggMapper/Execution/ExpressionBuilder.cs` | Core compilation — builds all three delegate paths |
| `src/EggMapper/Mapper.cs` | Public mapping API, thread-static context pooling |
| `src/EggMapper/MapperConfiguration.cs` | Orchestrates compilation, stores frozen delegate dictionaries |
| `src/EggMapper/MapperConfigurationExpression.cs` | Fluent configuration API (`CreateMap`, profiles, `ForMember`) |
| `src/EggMapper/Internal/TypePair.cs` | Value-type dictionary key for source/dest type lookups |
| `src/EggMapper/Internal/TypeDetails.cs` | Cached reflection metadata (properties, constructors) |
| `src/EggMapper/Internal/ReflectionHelper.cs` | Utility: numeric/collection type detection, flattening |

## Development Loops

### Loop 1 — Correctness
Write code → write tests → `dotnet test --configuration Release` → fix failures → repeat until 100% green.

### Loop 2 — Performance
Record baseline benchmark → optimize → re-benchmark → compare → repeat until EggMapper beats all competitors on every scenario.

## Constraints

- **Zero runtime reflection** — no `PropertyInfo.GetValue/SetValue` in hot paths
- **No LINQ in hot paths** — use `for` loops with pre-sized collections
- **`AggressiveInlining`** on `Map<S,D>()` and delegate lookup methods
- **Value-type `TypePair`** for dictionary keys (no boxing)
- **Pre-compiled child mappers** embedded in parent expression trees
- **EggMapper must be fastest** on every benchmark before merging

## Testing Conventions

- Framework: **xUnit** + **FluentAssertions**
- Pattern: **AAA** (Arrange / Act / Assert)
- Naming: `Feature_Condition_ExpectedBehavior` (e.g., `Map_NullSource_ReturnsDefault`)
- Tests live in `src/EggMapper.UnitTests/`

## Working Style

- Break complex tasks into smaller incremental commits
- A single PR should focus on one logical change
- Commit and push after each verified, self-contained unit of work
