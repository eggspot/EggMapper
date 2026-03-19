#!/usr/bin/env python3
"""
parse-benchmarks.py
───────────────────
Combines all per-class BenchmarkDotNet markdown reports produced by

    dotnet run --configuration Release -- --filter '*' --exporters markdown json

into a single, comprehensive GitHub-flavoured markdown document suitable for
a PR comment or a CI summary page.

Output sections
───────────────
  • 🖥️ Environment  – host info parsed from the JSON report
  • One section per benchmark class, ordered canonically
  • Column guide legend
  • Collapsible "Notes" block

Usage
─────
    python3 scripts/parse-benchmarks.py <artifacts_dir> <output_file>

    artifacts_dir   path to BenchmarkDotNet.Artifacts/
    output_file     path where the combined .md will be written
"""

from __future__ import annotations

import glob
import json
import os
import sys
from datetime import datetime, timezone
from pathlib import Path

# ── Canonical benchmark class → display title ──────────────────────────────
BENCHMARK_ORDER: list[tuple[str, str]] = [
    ("FlatMappingBenchmark",      "🔵 Flat Mapping — 10-property object"),
    ("FlatteningBenchmark",       "🟡 Flattening — 2 nested objects → 8 flat properties"),
    ("DeepTypeBenchmark",         "🟣 Deep Mapping — 2 nested address objects"),
    ("ComplexTypeBenchmark",      "🟢 Complex Mapping — nested object + collection"),
    ("CollectionBenchmark",       "🟠 Collection — 100-item `List<T>`"),
    ("DeepCollectionBenchmark",   "🔴 Deep Collection — 100 items with nested objects"),
    ("LargeCollectionBenchmark",  "⚫ Large Collection — 1,000-item `List<T>`"),
    ("StartupBenchmark",          "⚪ Startup / Configuration time"),
]

COLUMN_LEGEND = (
    "> **Column guide:**  \n"
    "> `Mean` = average execution time &nbsp;·&nbsp; "
    "`Error` = half of 99.9 % confidence interval &nbsp;·&nbsp; "
    "`StdDev` = standard deviation &nbsp;·&nbsp; "
    "`Min` / `Median` / `Max` = statistical range &nbsp;·&nbsp; "
    "`Ratio` = vs Manual baseline (lower = closer to hand-written speed) &nbsp;·&nbsp; "
    "`RatioSD` = ratio std dev &nbsp;·&nbsp; "
    "`Rank` = 1 is fastest &nbsp;·&nbsp; "
    "`Gen0/1/2` = GC collections per 1 000 ops &nbsp;·&nbsp; "
    "`Allocated` = managed heap per operation &nbsp;·&nbsp; "
    "`Alloc Ratio` = allocation ratio vs baseline"
)


# ── Helpers ────────────────────────────────────────────────────────────────

def _find_files(artifacts_dir: str, pattern: str) -> list[str]:
    return sorted(glob.glob(os.path.join(artifacts_dir, "results", pattern)))


def _read_host_env(artifacts_dir: str) -> str:
    """Extract human-readable host environment info from the first JSON report."""
    candidates = (
        _find_files(artifacts_dir, "*-report-full.json")
        or _find_files(artifacts_dir, "*-report.json")
    )
    if not candidates:
        return ""
    try:
        with open(candidates[0], encoding="utf-8") as fh:
            data = json.load(fh)
        env: dict = data.get("HostEnvironmentInfo", {})
        rows: list[str] = []
        if v := env.get("BenchmarkDotNetVersion"):
            rows.append(f"| BenchmarkDotNet | `{v}` |")
        if v := env.get("OsVersion"):
            rows.append(f"| OS | `{v}` |")
        if v := env.get("ProcessorName"):
            rows.append(f"| CPU | `{v}` |")
        if v := env.get("RuntimeVersion"):
            rows.append(f"| Runtime | `{v}` |")
        if v := env.get("Architecture"):
            rows.append(f"| Architecture | `{v}` |")
        if v := env.get("JitModules"):
            rows.append(f"| JIT | `{v}` |")
        if not rows:
            return ""
        header = "| Property | Value |\n|---|---|"
        return header + "\n" + "\n".join(rows)
    except (OSError, json.JSONDecodeError, KeyError, TypeError) as exc:
        return f"*Could not parse host info: {exc}*"


def _extract_table(filepath: str) -> str:
    """Return only the markdown table lines from a BDN report file."""
    try:
        with open(filepath, encoding="utf-8") as fh:
            lines = fh.readlines()
        table_lines = [ln.rstrip() for ln in lines if ln.startswith("|")]
        return "\n".join(table_lines)
    except (OSError, UnicodeDecodeError) as exc:
        return f"*Error reading `{filepath}`: {exc}*"


def _sort_key(filepath: str) -> int:
    for i, (class_name, _) in enumerate(BENCHMARK_ORDER):
        if class_name in filepath:
            return i
    return len(BENCHMARK_ORDER)


# ── Main report builder ────────────────────────────────────────────────────

def build_report(artifacts_dir: str) -> str:
    timestamp = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC")

    # Build artifacts link from environment (available in GitHub Actions context)
    server_url = os.environ.get("GITHUB_SERVER_URL", "https://github.com")
    repository  = os.environ.get("GITHUB_REPOSITORY", "")
    run_id      = os.environ.get("GITHUB_RUN_ID", "")
    if repository and run_id:
        artifacts_url = f"{server_url}/{repository}/actions/runs/{run_id}/artifacts"
        artifacts_link = f"[Download full artifacts]({artifacts_url})"
    else:
        artifacts_url = f"{server_url}/{repository}/actions/workflows/benchmarks.yml" if repository else ""
        artifacts_link = (
            f"[View workflow run]({artifacts_url})" if artifacts_url
            else "Run the [Benchmarks workflow](.github/workflows/benchmarks.yml) to regenerate."
        )

    lines: list[str] = [
        "## 📊 Benchmark Results",
        "",
        f"> **Generated:** {timestamp} &nbsp;·&nbsp; {artifacts_link}",
        "",
        COLUMN_LEGEND,
        "",
    ]

    # ── Environment block ──────────────────────────────────────────────────
    env_table = _read_host_env(artifacts_dir)
    if env_table:
        lines += [
            "<details>",
            "<summary>🖥️ Host environment</summary>",
            "",
            env_table,
            "",
            "</details>",
            "",
        ]

    # ── Per-class benchmark tables ─────────────────────────────────────────
    # Try GitHub-flavoured markdown first, fall back to plain markdown.
    md_files = (
        _find_files(artifacts_dir, "*-report-github.md")
        or _find_files(artifacts_dir, "*-report.md")
    )

    if not md_files:
        lines.append("*No benchmark result files found.*")
    else:
        md_files = sorted(md_files, key=_sort_key)
        for md_file in md_files:
            # Match to canonical title
            title = Path(md_file).stem  # fallback
            for class_name, display_title in BENCHMARK_ORDER:
                if class_name in md_file:
                    title = display_title
                    break

            table = _extract_table(md_file)
            if table:
                lines += [
                    f"### {title}",
                    "",
                    table,
                    "",
                ]

    # ── Footer / notes ─────────────────────────────────────────────────────
    lines += [
        "---",
        "",
        "<details>",
        "<summary>📝 Notes</summary>",
        "",
        "- Each benchmark class is decorated with `[MemoryDiagnoser]` and `[RankColumn]`.",
        "- The global config (see `src/EggMapper.Benchmarks/Program.cs`) adds `Min`, `Median`, and `Max` columns.",
        "- **Manual** is the hand-written baseline (ratio = 1.00). A ratio < 1 means *faster* than manual.",
        "- Benchmarks run on GitHub-hosted runners — absolute times may vary between runs; focus on **Ratio** for comparisons.",
        "- To reproduce locally:",
        "  ```bash",
        "  cd src/EggMapper.Benchmarks",
        "  dotnet run --configuration Release -- --filter '*'",
        "  ```",
        "",
        "</details>",
    ]

    return "\n".join(lines)


# ── Entry point ────────────────────────────────────────────────────────────

def main() -> None:
    if len(sys.argv) < 3:
        print(f"Usage: {sys.argv[0]} <artifacts_dir> <output_file>", file=sys.stderr)
        sys.exit(1)

    artifacts_dir = sys.argv[1]
    output_file = sys.argv[2]

    report = build_report(artifacts_dir)

    with open(output_file, "w", encoding="utf-8") as fh:
        fh.write(report)

    size_kb = len(report.encode()) / 1024
    print(f"Report written to '{output_file}' ({size_kb:.1f} KB, {report.count(chr(10))} lines)")


if __name__ == "__main__":
    main()
