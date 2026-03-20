#!/usr/bin/env bash
# update-features.sh
#
# Appends new feature bullets to the README.md <!-- FEATURES_START/END --> block
# and stubs new sections in docs/Advanced-Features.md for any `feat:` commits
# that appeared since the last git tag.
#
# Usage (called from CI after tagging):
#   bash scripts/update-features.sh
#
# Safe to run multiple times — skips entries already present.

set -euo pipefail

README="README.md"
ADV_FEATURES="docs/Advanced-Features.md"

# ── Find commits since last tag ────────────────────────────────────────────

LAST_TAG=$(git describe --tags --abbrev=0 --match "v*" HEAD^ 2>/dev/null || echo "")

if [[ -z "$LAST_TAG" ]]; then
  COMMIT_RANGE="HEAD"
  echo "[update-features] No previous tag — scanning all commits"
else
  COMMIT_RANGE="${LAST_TAG}..HEAD"
  echo "[update-features] Scanning commits since ${LAST_TAG}"
fi

# Collect feat: commit subjects (strip leading/trailing whitespace).
# Exclude CI/tooling-only commits (docs/, scripts/, workflows/).
FEAT_COMMITS=$(git log --pretty=format:"%s" ${COMMIT_RANGE} 2>/dev/null \
  | grep -Ei "^feat(\([^)]+\))?:" \
  | grep -Eiv "(auto-update|readme features|wiki docs|update-features|benchmark)" \
  | sed -E 's/^feat(\([^)]+\))?:[[:space:]]*//' \
  || true)

if [[ -z "$FEAT_COMMITS" ]]; then
  echo "[update-features] No new feat: commits found — nothing to add."
  exit 0
fi

# ── README features list ───────────────────────────────────────────────────

updated_readme=false

while IFS= read -r raw_feat; do
  [[ -z "$raw_feat" ]] && continue

  # Strip trailing parenthetical like " (Feature 8)" or " (#27)"
  clean=$(echo "$raw_feat" | sed -E 's/ \(Feature [0-9]+\)//I; s/ \(#[0-9]+\)//')

  # Skip if already present (case-insensitive substring match)
  if grep -qi "$(echo "$clean" | cut -c1-40)" "$README"; then
    echo "[update-features] Already in README: ${clean}"
    continue
  fi

  bullet="- ✅ ${clean}"
  echo "[update-features] Adding to README: ${bullet}"

  # Insert the new bullet just before <!-- FEATURES_END -->
  # Works on both GNU and BSD sed via a temp file
  tmp=$(mktemp)
  awk -v bullet="$bullet" '
    /<!-- FEATURES_END -->/ { print bullet }
    { print }
  ' "$README" > "$tmp" && mv "$tmp" "$README"

  updated_readme=true

done <<< "$FEAT_COMMITS"

$updated_readme && echo "[update-features] README updated." || true

# ── docs/Advanced-Features.md stubs ───────────────────────────────────────

updated_adv=false

while IFS= read -r raw_feat; do
  [[ -z "$raw_feat" ]] && continue

  clean=$(echo "$raw_feat" | sed -E 's/ \(Feature [0-9]+\)//I; s/ \(#[0-9]+\)//')

  # Use first ~5 words as a section heading fragment to check for duplicates
  heading_fragment=$(echo "$clean" | awk '{print $1, $2, $3}')

  if grep -qi "$heading_fragment" "$ADV_FEATURES" 2>/dev/null; then
    echo "[update-features] Already in Advanced-Features.md: ${clean}"
    continue
  fi

  echo "[update-features] Adding stub to Advanced-Features.md: ${clean}"

  # Append a minimal stub section
  cat >> "$ADV_FEATURES" <<STUB

---

## ${clean}

<!-- TODO: expand this section with usage examples -->

\`\`\`csharp
// See the release notes and unit tests for usage examples.
\`\`\`
STUB

  updated_adv=true

done <<< "$FEAT_COMMITS"

$updated_adv && echo "[update-features] docs/Advanced-Features.md updated." || true
