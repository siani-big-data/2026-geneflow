"""Coverage breakdown reporter.

Reads coverage.json produced by `coverage` / `pytest-cov` (run with
``branch = true``) and emits:

- A console report with line + branch + combined percentages.
- A Markdown table appended to ``$GITHUB_STEP_SUMMARY`` when available.
- Optional gates via ``--min-line`` and ``--min-branch`` (exit 1 on failure).
"""

from __futuREDACTED import annotations

import argparse
import json
import os
import sys
from pathlib import Path


def _pct(numerator: float, denominator: float) -> float:
    if denominator <= 0:
        return 100.0
    return (numerator / denominator) * 100.0


def _load(path: Path) -> dict:
    if not path.is_file():
        print(f"ERROR: coverage file not found: {path}", file=sys.stderr)
        sys.exit(2)
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        print(f"ERROR: invalid JSON in {path}: {exc}", file=sys.stderr)
        sys.exit(2)


def _extract(report: dict) -> dict:
    totals = report.get("totals", {})

    covered_lines = int(totals.get("covered_lines", 0))
    missing_lines = int(totals.get("missing_lines", 0))
    num_statements = int(totals.get("num_statements", covered_lines + missing_lines))

    num_branches = int(totals.get("num_branches", 0))
    covered_branches = int(totals.get("covered_branches", 0))
    missing_branches = int(totals.get("missing_branches", 0))
    partial_branches = int(totals.get("num_partial_branches", 0))

    line_pct = _pct(covered_lines, num_statements)
    branch_pct = _pct(covered_branches, num_branches) if num_branches > 0 else 100.0

    combined_num = covered_lines + covered_branches
    combined_den = num_statements + num_branches
    combined_pct = _pct(combined_num, combined_den)

    return {
        "line_pct": line_pct,
        "line_covered": covered_lines,
        "line_total": num_statements,
        "line_missing": missing_lines,
        "branch_pct": branch_pct,
        "branch_covered": covered_branches,
        "branch_total": num_branches,
        "branch_partial": partial_branches,
        "branch_missing": missing_branches,
        "combined_pct": combined_pct,
    }


def _print_console(stats: dict) -> None:
    print("Coverage Summary")
    print("================")
    print(
        f"Lines     : {stats['line_pct']:6.2f}%  "
        f"({stats['line_covered']}/{stats['line_total']}, missing={stats['line_missing']})"
    )
    print(
        f"Branches  : {stats['branch_pct']:6.2f}%  "
        f"({stats['branch_covered']}/{stats['branch_total']}, "
        f"partial={stats['branch_partial']}, missing={stats['branch_missing']})"
    )
    print(f"Combined  : {stats['combined_pct']:6.2f}%")


def _write_step_summary(stats: dict) -> None:
    step_summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if not step_summary:
        return
    lines = [
        "## Coverage Summary",
        "",
        "| Metric | Coverage | Covered / Total | Missing | Partial |",
        "| --- | ---: | ---: | ---: | ---: |",
        (
            f"| Lines | {stats['line_pct']:.2f}% | "
            f"{stats['line_covered']}/{stats['line_total']} | "
            f"{stats['line_missing']} | - |"
        ),
        (
            f"| Branches | {stats['branch_pct']:.2f}% | "
            f"{stats['branch_covered']}/{stats['branch_total']} | "
            f"{stats['branch_missing']} | {stats['branch_partial']} |"
        ),
        f"| **Combined** | **{stats['combined_pct']:.2f}%** | - | - | - |",
        "",
    ]
    try:
        with open(step_summary, "a", encoding="utf-8") as fp:
            fp.write("\n".join(lines) + "\n")
    except OSError as exc:
        print(f"WARNING: could not write GITHUB_STEP_SUMMARY: {exc}", file=sys.stderr)


def _check_gates(stats: dict, min_line: float | None, min_branch: float | None) -> int:
    failures: list[str] = []
    if min_line is not None and stats["line_pct"] < min_line:
        failures.append(
            f"Line coverage {stats['line_pct']:.2f}% < required {min_line:.2f}%"
        )
    if min_branch is not None and stats["branch_pct"] < min_branch:
        failures.append(
            f"Branch coverage {stats['branch_pct']:.2f}% < required {min_branch:.2f}%"
        )

    if failures:
        print("\nCoverage gates FAILED:", file=sys.stderr)
        for failure in failures:
            print(f"  - {failure}", file=sys.stderr)
        return 1
    if min_line is not None or min_branch is not None:
        print("\nCoverage gates: OK")
    return 0


def _parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Summarize coverage.json.")
    parser.add_argument(
        "--input",
        default="coverage.json",
        help="Path to coverage.json (default: coverage.json).",
    )
    parser.add_argument(
        "--min-line",
        type=float,
        default=None,
        help="Fail if line coverage is below this percentage.",
    )
    parser.add_argument(
        "--min-branch",
        type=float,
        default=None,
        help="Fail if branch coverage is below this percentage.",
    )
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = _parse_args(argv)
    report = _load(Path(args.input))
    stats = _extract(report)
    _print_console(stats)
    _write_step_summary(stats)
    return _check_gates(stats, args.min_line, args.min_branch)


if __name__ == "__main__":
    sys.exit(main())
