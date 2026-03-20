---
name: ship
description: Create a PR with performance evidence and proper conventional commit history
disable-model-invocation: true
user-invocable: true
allowed-tools: Bash, Read, Grep, Glob
---

# Ship — Create PR with Performance Evidence

Create a pull request for the current feature branch, including benchmark evidence that EggMapper remains the fastest runtime mapper.

## Arguments

- `$ARGUMENTS` — optional PR title override. If empty, auto-generate from branch name and commits.

## Steps

1. **Pre-flight checks**:
   - `dotnet build --configuration Release` — must succeed with no warnings
   - `dotnet test --configuration Release` — must be 100% green
   - Verify we are NOT on `main` branch

2. **Run key benchmarks** for performance evidence:
   ```bash
   cd src/EggMapper.Benchmarks && dotnet run -c Release -f net10.0 -- --filter *FlatMappingBenchmark* --exporters markdown
   ```
   Read the results to include in PR body.

3. **Analyze commits** on this branch vs main:
   ```bash
   git log main..HEAD --oneline
   git diff main...HEAD --stat
   ```

4. **Create PR** using `gh pr create`:
   - Title: short, under 70 chars, describes the feature
   - Body format:

   ```markdown
   ## Summary
   - What was added/changed (1-3 bullets)

   ## Performance
   | Scenario | EggMapper | AutoMapper | Mapster | Mapperly |
   |----------|-----------|------------|---------|----------|
   (include key benchmark results)

   EggMapper remains the fastest runtime mapper on all scenarios.

   ## Test plan
   - [ ] All existing tests pass
   - [ ] New tests cover happy path, edge cases, null handling
   - [ ] Benchmarks show no regression
   - [ ] EggMapper beats all runtime mappers

   Generated with [Claude Code](https://claude.com/claude-code)
   ```

5. **Return the PR URL** to the user.

## Important

- NEVER push to `main` directly
- NEVER create a PR without benchmark evidence
- If benchmarks show regression, STOP and fix before shipping
