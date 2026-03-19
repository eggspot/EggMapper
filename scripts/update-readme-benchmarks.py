#!/usr/bin/env python3
"""
update-readme-benchmarks.py
────────────────────────────
Replaces the content between

    <!-- BENCHMARK_RESULTS_START -->
    ...
    <!-- BENCHMARK_RESULTS_END -->

in README.md with fresh benchmark tables from BenchmarkDotNet markdown reports.

On the first run (markers not present yet) the script falls back to replacing
everything inside the `## Performance` section.

Usage
─────
    python3 scripts/update-readme-benchmarks.py <artifacts_dir> <readme_path>

    artifacts_dir   path to BenchmarkDotNet.Artifacts/
    readme_path     path to the README.md file to update in-place
"""

from __future__ import annotations

import glob
import os
import re
import sys
from datetime import datetime, timezone
from pathlib import Path

START_MARKER = "<!-- BENCHMARK_RESULTS_START -->"
END_MARKER   = "<!-- BENCHMARK_RESULTS_END -->"

# Canonical order and short labels for the README summary
BENCHMARK_ORDER: list[tuple[str, str]] = [
    ("FlatMappingBenchmark",      "🔵 Flat Mapping (10 properties)"),
    ("FlatteningBenchmark",       "🟡 Flattening"),
    ("DeepTypeBenchmark",         "🟣 Deep Mapping (2 nested objects)"),
    ("ComplexTypeBenchmark",      "🟢 Complex Mapping (nested + collection)"),
    ("CollectionBenchmark",       "🟠 Collection (100 items)"),
    ("DeepCollectionBenchmark",   "🔴 Deep Collection (100 items, nested)"),
    ("LargeCollectionBenchmark",  "⚫ Large Collection (1,000 items)"),
    ("StartupBenchmark",          "⚪ Startup / Config"),
]

COLUMN_LEGEND = (
    "> **Column guide:** "
    "`Mean` = avg time · "
    "`Error` = ½ CI · "
    "`StdDev` = std dev · "
    "`Min`/`Median`/`Max` = range · "
    "`Ratio` = vs Manual baseline · "
    "`Rank` = 1 is fastest · "
    "`Allocated` = heap / op"
)

COMPETITORS_NOTE = (
    "> **Competitors tested:** EggMapper, AutoMapper, Mapster, "
    "Mapperly (source-gen), AgileMapper"
)

# Absolute URL — works from README, NuGet, and anywhere else
_REPO_URL = "https://github.com/eggspot/EggMapper"
_WORKFLOW_URL = f"{_REPO_URL}/actions/workflows/benchmarks.yml"


# ── Helpers ────────────────────────────────────────────────────────────────

def _find_md_files(artifacts_dir: str) -> list[str]:
    pattern_github = os.path.join(artifacts_dir, "results", "*-report-github.md")
    pattern_plain  = os.path.join(artifacts_dir, "results", "*-report.md")
    files = glob.glob(pattern_github) or glob.glob(pattern_plain)
    def _key(p: str) -> int:
        for i, (cls, _) in enumerate(BENCHMARK_ORDER):
            if cls in p:
                return i
        return len(BENCHMARK_ORDER)
    return sorted(files, key=_key)


def _extract_table(filepath: str) -> str:
    try:
        with open(filepath, encoding="utf-8") as fh:
            lines = fh.readlines()
        return "\n".join(ln.rstrip() for ln in lines if ln.startswith("|"))
    except (OSError, UnicodeDecodeError) as exc:
        return f"*Error reading report: {exc}*"


# ── Section builder ────────────────────────────────────────────────────────

def build_performance_section(artifacts_dir: str) -> str:
    md_files = _find_md_files(artifacts_dir)
    has_results = bool(md_files)

    parts: list[str] = [
        START_MARKER,
        "",
    ]

    # Only include a timestamp when there are real benchmark results so that
    # no-op runs (benchmarks unchanged) don't produce a spurious diff/commit.
    if has_results:
        timestamp = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC")
        parts += [
            f"> ⏱ **Last updated:** {timestamp}",
            "",
        ]

    parts += [
        COMPETITORS_NOTE,
        "",
        COLUMN_LEGEND,
        "",
    ]

    if not has_results:
        parts.append(
            f"*Benchmark results not yet available — run the "
            f"[Benchmarks workflow]({_WORKFLOW_URL}).*"
        )
    else:
        for md_file in md_files:
            label = Path(md_file).stem  # fallback
            for cls_name, display_label in BENCHMARK_ORDER:
                if cls_name in md_file:
                    label = display_label
                    break

            table = _extract_table(md_file)
            if table:
                parts += [
                    f"#### {label}",
                    "",
                    table,
                    "",
                ]

    parts += [
        "---",
        "",
        f"*Benchmarks run automatically on every push to `main` with .NET 10. "
        f"[See workflow]({_WORKFLOW_URL})*",
        "",
        END_MARKER,
    ]

    return "\n".join(parts)


# ── README updater ─────────────────────────────────────────────────────────

def update_readme(artifacts_dir: str, readme_path: str) -> None:
    with open(readme_path, encoding="utf-8") as fh:
        content = fh.read()

    new_section = build_performance_section(artifacts_dir)

    if START_MARKER in content and END_MARKER in content:
        # Precise marker-based replacement — always preferred.
        pattern = re.escape(START_MARKER) + r".*?" + re.escape(END_MARKER)
        new_content = re.sub(pattern, new_section, content, flags=re.DOTALL)
    else:
        # Fallback: replace everything from the ## Performance heading until
        # the next ## heading (or end of file).
        pattern = r"(## Performance\n)(.*?)(\n## |\Z)"
        new_content = re.sub(
            pattern,
            lambda m: m.group(1) + "\n" + new_section + "\n" + m.group(3),
            content,
            flags=re.DOTALL,
        )

    with open(readme_path, "w", encoding="utf-8") as fh:
        fh.write(new_content)

    changed = content != new_content
    print(f"{'Updated' if changed else 'No changes in'} '{readme_path}'")


# ── Entry point ────────────────────────────────────────────────────────────

def main() -> None:
    if len(sys.argv) < 3:
        print(f"Usage: {sys.argv[0]} <artifacts_dir> <readme_path>", file=sys.stderr)
        sys.exit(1)

    update_readme(sys.argv[1], sys.argv[2])


if __name__ == "__main__":
    main()
