---
name: perf-check
description: Verify no performance regression after code changes — compares before/after benchmarks
disable-model-invocation: true
user-invocable: true
allowed-tools: Bash, Read, Grep, Glob
---

# Performance Regression Check

Verify that code changes haven't introduced performance regressions. This is the gate that must pass before any PR is shipped.

## Steps

1. **Check for uncommitted changes** — run `git status` to understand what was modified.

2. **Identify affected benchmark scenarios** by analyzing which files changed:
   - `ExpressionBuilder.cs` changes → run ALL benchmarks (core compilation)
   - `Mapper.cs` changes → run `FlatMapping`, `Collection`, `DeepType` benchmarks
   - `MapperConfiguration.cs` changes → run ALL benchmarks
   - `TypeDetails.cs` / `ReflectionHelper.cs` → run ALL benchmarks
   - New feature files only → run the most relevant benchmark + `FlatMapping` as baseline

3. **Run affected benchmarks**:
   ```bash
   cd src/EggMapper.Benchmarks && dotnet run -c Release -f net10.0 -- --filter *{scenario}* --exporters json markdown
   ```

4. **Analyze results against constraints**:
   - EggMapper MUST be fastest runtime mapper (AutoMapper, Mapster, AgileMapper) on every scenario
   - EggMapper MUST have zero or minimal extra allocations vs manual code
   - Mapperly (source generator) is the only acceptable faster mapper
   - Any regression where EggMapper loses to a runtime competitor is a **blocker**

5. **Report verdict**:

   ### Perf Check: PASS / FAIL

   **Changed files**: list files
   **Benchmarks run**: list scenarios
   **Results**: table comparing EggMapper vs competitors

   If FAIL:
   - Which scenario regressed
   - Which competitor is now faster
   - Suggested optimization approach

6. **IMPORTANT**: Do NOT approve changes that regress performance. Performance is the #1 priority for EggMapper.
