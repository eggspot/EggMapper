# Performance

## How EggMapper Achieves High Performance

EggMapper is built on three core principles:

1. **Compile once, run many times** — `MapperConfiguration` compiles expression-tree delegates at construction time. Every subsequent `Map()` call is a direct delegate invocation with no reflection.
2. **Value-type dictionary key** — The internal `ConcurrentDictionary` uses a `TypePair` value-type key, eliminating boxing on every lookup.
3. **Pre-compiled child mappers** — Nested type delegates are captured in closures at compile time. There is no per-call dictionary lookup for nested objects.

---

## Benchmark Setup

The benchmark suite lives in `src/EggMapper.Benchmarks/` and uses [BenchmarkDotNet](https://benchmarkdotnet.org/).

Each class compares four mappers against the same **manual** (hand-written) baseline:

| Benchmark class | Scenario |
|---|---|
| `FlatMappingBenchmark` | 10-property flat object |
| `DeepTypeBenchmark` | Object with two nested address objects |
| `CollectionBenchmark` | `List<T>` with 100 elements |
| `ComplexTypeBenchmark` | Nested object + `List<T>` children |
| `StartupBenchmark` | Configuration / compilation time |

Columns exported per benchmark: `Mean`, `Error`, `StdDev`, `Min`, `Median`, `Max`, `Ratio`, `RatioSD`, `Rank`, `Gen0`, `Gen1`, `Gen2`, `Allocated`, `Alloc Ratio`.

---

## Running Benchmarks Locally

```bash
cd src/EggMapper.Benchmarks

# All benchmarks (default config — most accurate)
dotnet run --configuration Release -- --filter '*'

# Single benchmark class
dotnet run --configuration Release -- --filter '*FlatMapping*'

# Export to markdown + JSON
dotnet run --configuration Release -- --filter '*' --exporters markdown json

# Faster CI-style run (fewer iterations)
dotnet run --configuration Release -- --filter '*' --job short
```

Results are written to `BenchmarkDotNet.Artifacts/results/`.

---

## CI Benchmark Results

Benchmarks run automatically on every push to `main` and on every pull request via the [Benchmarks workflow](https://github.com/eggspot/EggMapper/actions/workflows/benchmarks.yml).

- **Pull requests** receive a detailed comment with all tables, system info, and column descriptions.
- **Main branch** — the `README.md` Performance section is updated in-place with the latest tables.

---

## Performance Targets

| Scenario | EggMapper target vs AutoMapper |
|---|---|
| Flat mapping | ≤ 0.4× AutoMapper time (2.5× faster) |
| Deep / nested mapping | ≤ 0.5× AutoMapper time (2× faster) |
| Collection (100 items) | ≤ 0.5× AutoMapper time (2× faster) |
| Startup / config | ≤ 1× AutoMapper time (at least as fast) |

> A **lower ratio** is better. `Ratio = 1.00` equals the hand-written Manual baseline.

---

## Tips for Best Performance in Your Application

1. **Use a singleton `MapperConfiguration`** — never construct it per-request.
2. **Register all maps upfront** — discovered maps compiled lazily still pay a one-time cost on first use.
3. **Prefer `Map<TSrc, TDst>(src)`** over the non-generic overload — the generic path has a direct delegate lookup with no boxing.
4. **Avoid mapping in tight inner loops with very small objects** — for truly hot paths, consider a hand-written projection; EggMapper is close to manual but not zero-cost.
5. **Use `AssertConfigurationIsValid()` in tests** — ensures the compiled delegate is exercised during the test run so the JIT has warmed it up before production load.
