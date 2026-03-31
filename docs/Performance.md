---
layout: default
title: Performance
nav_order: 6
---

# Performance

## How EggMapper Achieves High Performance

EggMapper is the **fastest .NET runtime object-to-object mapper**, achieving near-manual mapping speed through these techniques:

1. **Compile once, run many times** — `MapperConfiguration` compiles expression-tree delegates at construction time. Every subsequent `Map()` call is a direct delegate invocation with no reflection.
2. **Context-free typed delegates** — For flat and nested maps, EggMapper compiles `Func<TSource, TDestination>` delegates with zero boxing. Nested object mappings are **inlined directly** into the parent expression tree.
3. **Static generic caching** — `TypePairCache<TSource, TDestination>` eliminates dictionary lookups after the first call for each type pair.
4. **Inlined collection loops** — `MapList<>()` uses compiled `Func<IList<TSource>, List<TDestination>>` delegates where the entire loop + element mapping is a single expression tree.
5. **Zero extra allocations** — EggMapper matches hand-written code allocation in every scenario.

---

## Benchmark Setup

The benchmark suite lives in `src/EggMapper.Benchmarks/` and uses [BenchmarkDotNet](https://benchmarkdotnet.org/).

Each class compares **six mappers** against the same **manual** (hand-written) baseline:

| Benchmark class | Scenario |
|---|---|
| `FlatMappingBenchmark` | 10-property flat object |
| `FlatteningBenchmark` | Flattening 2 nested objects into 8 properties |
| `DeepTypeBenchmark` | Object with two nested address objects |
| `ComplexTypeBenchmark` | Nested object + `List<T>` children |
| `CollectionBenchmark` | `List<T>` with 100 elements |
| `DeepCollectionBenchmark` | 100 elements with 2 nested objects each |
| `LargeCollectionBenchmark` | `List<T>` with 1,000 elements |
| `StartupBenchmark` | Configuration / compilation time |

**Competitors tested:** EggMapper, AutoMapper, Mapster, Mapperly (source-gen), AgileMapper.

---

## Running Benchmarks Locally

```bash
cd src/EggMapper.Benchmarks

# All benchmarks on .NET 10 (recommended)
dotnet run -c Release -f net10.0 -- --filter '*'

# Single benchmark class
dotnet run -c Release -f net10.0 -- --filter '*FlatMapping*'

# Export to markdown + JSON
dotnet run -c Release -f net10.0 -- --filter '*' --exporters markdown json

# Faster CI-style run (fewer iterations)
dotnet run -c Release -f net10.0 -- --filter '*' --job short
```

Results are written to `BenchmarkDotNet.Artifacts/results/`.

---

## CI Benchmark Results

Benchmarks run automatically on every push to `main` and on every pull request via the [Benchmarks workflow](https://github.com/eggspot/EggMapper/actions/workflows/benchmarks.yml).

- **Pull requests** receive a detailed comment with all tables, system info, and column descriptions.
- **Main branch** — the `README.md` Performance section is updated in-place with the latest tables.

---

## Performance Targets

| Scenario | Target |
|---|---|
| Flat mapping | Faster than Mapster |
| Deep / nested mapping | Faster than Mapster |
| Flattening | Faster than Mapster |
| Collection (100 items) | Within 10% of Mapster |
| All scenarios | 1.5–2.5× faster than AutoMapper |
| All scenarios | Zero extra allocations vs manual |

> A **lower ratio** is better. `Ratio = 1.00` equals the hand-written Manual baseline.

---

## Tips for Best Performance in Your Application

1. **Use a singleton `MapperConfiguration`** — never construct it per-request.
2. **Register all maps upfront** — discovered maps compiled lazily still pay a one-time cost on first use.
3. **Prefer `Map<TSrc, TDst>(src)`** over the non-generic overload — the generic path uses the static generic cache with zero dictionary lookups.
4. **Use `MapList<TSrc, TDst>(source)`** for collections — it uses a fully inlined compiled loop that's near-manual speed.
5. **Use `AssertConfigurationIsValid()` in tests** — ensures the compiled delegate is exercised during the test run so the JIT has warmed it up before production load.
