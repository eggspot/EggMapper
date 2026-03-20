---
name: bench
description: Run EggMapper benchmarks and analyze performance against competitors
disable-model-invocation: true
user-invocable: true
allowed-tools: Bash, Read, Grep, Glob
---

# Benchmark Runner

Run benchmarks and analyze EggMapper's performance against competitors (AutoMapper, Mapster, Mapperly, AgileMapper).

## Arguments

- `$ARGUMENTS` — optional benchmark filter (e.g., `FlatMapping`, `Collection`, `Deep`). If empty, run all benchmarks.

## Steps

1. **Determine the benchmark filter**:
   - If `$ARGUMENTS` is provided, use it as the `--filter` pattern (e.g., `*FlatMappingBenchmark*`)
   - If empty, use `--filter *` to run all benchmarks
   - If `$ARGUMENTS` is `short`, run all benchmarks with `--job short` for a quick smoke test

2. **Run the benchmark**:
   ```bash
   cd src/EggMapper.Benchmarks && dotnet run -c Release -f net10.0 -- --filter *{filter}* --exporters json markdown
   ```

3. **Read and analyze results**:
   - Find the latest results in `BenchmarkDotNet.Artifacts/results/`
   - Read the markdown results file
   - Compare EggMapper vs every competitor on each scenario

4. **Report with this format**:

   ### Benchmark Results: {filter}

   | Scenario | EggMapper | AutoMapper | Mapster | Mapperly | Winner |
   |----------|-----------|------------|---------|----------|--------|

   **Performance verdict**:
   - List any scenario where EggMapper is NOT the fastest runtime mapper
   - Flag any regression vs previous results if baseline exists
   - Note allocation differences (EggMapper target: zero extra allocations)

5. **CRITICAL**: EggMapper MUST be the fastest runtime mapper on every scenario. If it's not, flag it as a blocker and suggest optimization targets. Mapperly (compile-time source generator) is the only acceptable mapper to be faster than EggMapper.
