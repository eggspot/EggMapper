---
name: feat
description: Implement a new feature following EggMapper's performance-first development loop
disable-model-invocation: true
user-invocable: true
allowed-tools: Bash, Read, Write, Edit, Grep, Glob, Agent, TodoWrite
---

# Feature Implementation — Performance-First Workflow

Implement a feature for EggMapper following the two-loop development process: correctness first, then performance.

## Arguments

- `$ARGUMENTS` — description of the feature to implement (e.g., "enum string conversion", "dictionary mapping", "record constructor mapping")

## Workflow

### Phase 1: Understand

1. **Check the roadmap issue** for implementation details:
   - Read the plan file if it exists: `~/.claude/plans/wise-plotting-shore.md`
   - Search GitHub issues for context: `gh issue list --search "$ARGUMENTS"`

2. **Read the affected files** before writing any code. Understand existing patterns:
   - `src/EggMapper/Execution/ExpressionBuilder.cs` — the three delegate paths
   - `src/EggMapper/Internal/TypeDetails.cs` — cached reflection metadata
   - `src/EggMapper/Internal/ReflectionHelper.cs` — type detection utilities
   - `src/EggMapper/MapperConfiguration.cs` — compilation orchestration

3. **Create a feature branch**:
   ```bash
   git checkout -b feat/{feature-name}
   ```

### Phase 2: Correctness Loop

4. **Write tests FIRST** in `src/EggMapper.UnitTests/`:
   - Follow naming: `Feature_Condition_ExpectedBehavior`
   - Use xUnit + FluentAssertions, AAA pattern
   - Cover: happy path, null/empty, edge cases, nested objects, collections

5. **Implement the feature** following these performance constraints:
   - **Zero runtime reflection** — no `PropertyInfo.GetValue/SetValue` in hot paths
   - **Zero extra allocations** — match manual code allocation
   - **No LINQ in hot paths** — use `for` loops with pre-sized collections
   - **Expression trees** — compile mapping logic at config time, not map time
   - **Three-path pattern**: If the feature can be a pure expression tree → implement in ctx-free path. If it needs runtime context → implement in flexible path. Features that can't be inlined should bail from ctx-free (`return null`) and fall to flexible path.
   - **Reuse existing patterns** — look at how nested objects, collections, and flattening are already inlined

6. **Run tests**:
   ```bash
   dotnet test --configuration Release
   ```
   Fix failures. Repeat until 100% green.

7. **Commit**: `git commit` with conventional prefix (`feat:`, `fix:`, `perf:`)

### Phase 3: Performance Loop

8. **Run benchmarks** to verify no regression:
   ```bash
   cd src/EggMapper.Benchmarks && dotnet run -c Release -f net10.0 -- --filter * --exporters json markdown
   ```

9. **Analyze**: EggMapper MUST beat all runtime mappers on every scenario. If not:
   - Profile the hot path
   - Check if the feature accidentally forces maps to the flexible path
   - Optimize expression trees (inline more, avoid delegate calls, eliminate boxing)
   - Re-benchmark until EggMapper wins

10. **If the feature adds a new mapping scenario**, add a benchmark class in `src/EggMapper.Benchmarks/` comparing EggMapper vs AutoMapper vs Mapster vs Mapperly.

### Phase 4: Ship

11. **Final checks**:
    - `dotnet build --configuration Release` — no warnings
    - `dotnet test --configuration Release` — all green
    - Benchmarks show no regression and EggMapper is fastest runtime mapper

12. **Push and notify**: Push the branch, but do NOT create PR yet (use `/ship` for that).

## Key Reminders

- NEVER push directly to `main` — always feature branch + PR
- Performance is the #1 constraint. A correct but slow implementation is not done.
- Keep changes minimal. Don't refactor unrelated code.
- Use conventional commits: `feat:` for new features, `fix:` for bugs, `perf:` for optimizations
