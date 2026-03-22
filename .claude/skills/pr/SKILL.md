---
name: pr
description: Manage pull requests — list open PRs, check CI status, view details, or merge
disable-model-invocation: true
user-invocable: true
allowed-tools: Bash, Read
---

# PR Manager

List, inspect, or merge pull requests on the EggMapper repo.

## Arguments

- `$ARGUMENTS` — optional action:
  - Empty or `list` → list all open PRs with CI status
  - `<number>` → show details + CI status for that PR (e.g., `42`)
  - `merge <number>` → merge a PR after verifying CI is green
  - `ci` → show CI status for the current branch

## Steps

### List open PRs (default)
```bash
gh pr list --repo eggspot/EggMapper
```
Then for each PR, show: number, title, author, CI status, branch name.

### Show PR details
```bash
gh pr view $NUMBER --repo eggspot/EggMapper
gh pr checks $NUMBER --repo eggspot/EggMapper
```
Report: title, description summary, changed files count, CI check results (pass/fail/pending).

### Check CI on current branch
```bash
gh pr checks --repo eggspot/EggMapper
```
If no PR exists yet for the current branch, use:
```bash
gh run list --branch $(git branch --show-current) --repo eggspot/EggMapper --limit 3
```

### Merge a PR
1. Verify CI is fully green — **do not merge if any check is failing or pending**
2. Confirm the PR targets `main` (never merge to another base without asking)
3. Merge with squash:
   ```bash
   gh pr merge $NUMBER --squash --repo eggspot/EggMapper
   ```
4. Report: merged commit SHA, new version that CI will cut (patch/minor/major based on commit prefix)

## CI Check Interpretation
- `feat:` commits → minor version bump after merge
- `fix:` / `perf:` / `chore:` → patch bump
- `BREAKING CHANGE` / `feat!:` → major bump
- Always wait for ALL checks to pass before merging
