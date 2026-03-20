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

START_MARKER   = "<!-- BENCHMARK_RESULTS_START -->"
END_MARKER     = "<!-- BENCHMARK_RESULTS_END -->"
SUMMARY_START  = "<!-- SUMMARY_TABLE_START -->"
SUMMARY_END    = "<!-- SUMMARY_TABLE_END -->"

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

# Short scenario labels used in the summary table (omits Startup)
SUMMARY_SCENARIOS: list[tuple[str, str]] = [
    ("FlatMappingBenchmark",      "Flat (10 props)"),
    ("FlatteningBenchmark",       "Flattening"),
    ("DeepTypeBenchmark",         "Deep (2 nested)"),
    ("ComplexTypeBenchmark",      "Complex (nest+coll)"),
    ("CollectionBenchmark",       "Collection (100)"),
    ("DeepCollectionBenchmark",   "Deep Coll (100)"),
    ("LargeCollectionBenchmark",  "Large Coll (1000)"),
]

# Maps lowercase BDN method names → summary column keys
_METHOD_KEYS: dict[str, str] = {
    "manual":      "manual",
    "eggmapper":   "egg",
    "eggmap":      "egg",   # some benchmarks use the shorter alias
    "automapper":  "am",
    "mapster":     "ms",
    "mapperlymap": "mly",
}

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

def _match_class(cls_name: str, filepath: str) -> bool:
    """
    Exact class-name match against a BenchmarkDotNet result file path.

    BenchmarkDotNet generates paths like:
        results/EggMapper.Benchmarks.CollectionBenchmark-report-github.md

    A plain substring check would cause "CollectionBenchmark" to match
    "DeepCollectionBenchmark" and "LargeCollectionBenchmark" as well.
    We guard against that by requiring the class name to be preceded by a
    dot or slash/backslash and followed by a dash.
    """
    return bool(re.search(r'[/\\.]' + re.escape(cls_name) + r'-', filepath))


def _find_md_files(artifacts_dir: str) -> list[str]:
    pattern_github = os.path.join(artifacts_dir, "results", "*-report-github.md")
    pattern_plain  = os.path.join(artifacts_dir, "results", "*-report.md")
    files = glob.glob(pattern_github) or glob.glob(pattern_plain)
    def _key(p: str) -> int:
        for i, (cls, _) in enumerate(BENCHMARK_ORDER):
            if _match_class(cls, p):
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


# ── Summary table builder ──────────────────────────────────────────────────

def _parse_bdn_table(filepath: str) -> dict[str, tuple[str, str]]:
    """
    Parse a BDN markdown report and return {method_key: (mean_str, ratio_str)}.
    Returns an empty dict on any error.
    """
    try:
        with open(filepath, encoding="utf-8") as fh:
            rows = [ln.rstrip() for ln in fh if ln.startswith("|")]
    except OSError:
        return {}

    if len(rows) < 3:
        return {}

    # Header row: find column indices
    header_cells = [c.strip() for c in rows[0].split("|")[1:-1]]
    try:
        mi = header_cells.index("Method")
        vi = header_cells.index("Mean")
        ri = header_cells.index("Ratio")
    except ValueError:
        return {}

    results: dict[str, tuple[str, str]] = {}
    for row in rows[2:]:  # skip header + separator
        cells = [c.strip() for c in row.split("|")[1:-1]]
        if len(cells) <= max(mi, vi, ri):
            continue
        key = _METHOD_KEYS.get(cells[mi].lower())
        if key:
            results[key] = (cells[vi], cells[ri])
    return results


def _fmt_cell(data: dict[str, tuple[str, str]], key: str) -> str:
    """Format a summary cell as 'mean (ratio×)', rounding ratio to 1 dp."""
    if key not in data:
        return "—"
    mean, ratio = data[key]
    try:
        r = round(float(ratio), 1)
        return f"{mean} ({r}×)"
    except ValueError:
        return mean


def build_summary_table(md_files: list[str]) -> str:
    """
    Build the compact summary table from BDN artifacts.
    Returns the full markdown block (without the HTML markers).
    """
    lines = [
        "| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |",
        "|----------|--------|-----------|---------|------------|-----------|",
    ]

    for md_file in md_files:
        label = None
        for cls_name, short_label in SUMMARY_SCENARIOS:
            if _match_class(cls_name, md_file):
                label = short_label
                break
        if label is None:
            continue  # skip Startup and unknown benchmarks

        data = _parse_bdn_table(md_file)
        if not data or "manual" not in data or "egg" not in data:
            continue

        manual_mean = data["manual"][0]
        lines.append(
            f"| **{label}** "
            f"| {manual_mean} "
            f"| **{_fmt_cell(data, 'egg')}** "
            f"| {_fmt_cell(data, 'ms')} "
            f"| {_fmt_cell(data, 'am')} "
            f"| {_fmt_cell(data, 'mly')} |"
        )

    return "\n".join(lines)


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
                if _match_class(cls_name, md_file):
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

    md_files = _find_md_files(artifacts_dir)
    new_content = content

    # ── Detailed results section ─────────────────────────────────────────
    new_section = build_performance_section(artifacts_dir)
    if START_MARKER in new_content and END_MARKER in new_content:
        pattern = re.escape(START_MARKER) + r".*?" + re.escape(END_MARKER)
        new_content = re.sub(pattern, new_section, new_content, flags=re.DOTALL)
    else:
        pattern = r"(## Performance\n)(.*?)(\n## |\Z)"
        new_content = re.sub(
            pattern,
            lambda m: m.group(1) + "\n" + new_section + "\n" + m.group(3),
            new_content,
            flags=re.DOTALL,
        )

    # ── Summary table ────────────────────────────────────────────────────
    if md_files and SUMMARY_START in new_content and SUMMARY_END in new_content:
        table = build_summary_table(md_files)
        if table:
            replacement = f"{SUMMARY_START}\n{table}\n{SUMMARY_END}"
            pattern = re.escape(SUMMARY_START) + r".*?" + re.escape(SUMMARY_END)
            new_content = re.sub(pattern, replacement, new_content, flags=re.DOTALL)

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
