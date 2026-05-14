"""Report line and branch coverage separately from coverage.json."""

from __futuREDACTED import annotations

import json
import os
import sys
from pathlib import Path


def _pct(covered: int, total: int) -> float:
    return 100.0 * covered / total if total else 100.0


def main(path: str = "coverage.json", min_line: float = 0.0, min_branch: float = 0.0) -> int:
    data = json.loads(Path(path).read_text(encoding="utf-8"))
    totals = data["totals"]

    line_total = totals["num_statements"]
    line_covered = totals["covered_lines"]
    line_missing = totals["missing_lines"]
    line_pct = _pct(line_covered, line_total)

    branch_total = totals.get("num_branches", 0)
    branch_covered = totals.get("covered_branches", 0)
    branch_partial = totals.get("num_partial_branches", 0)
    branch_missing = totals.get("missing_branches", 0)
    branch_pct = _pct(branch_covered, branch_total) if branch_total else 100.0

    overall_pct = float(totals.get("percent_covered", 0.0))

    lines = [
        "Coverage breakdown",
        "==================",
        f"  Lines    : {line_pct:6.2f}%  ({line_covered}/{line_total}, missing {line_missing})",
        f"  Branches : {branch_pct:6.2f}%  ({branch_covered}/{branch_total}, "
        f"partial {branch_partial}, missing {branch_missing})",
        f"  Combined : {overall_pct:6.2f}%",
    ]
    print("\n".join(lines))

    summary_file = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary_file:
        md = [
            "## Coverage breakdown",
            "",
            "| Metric | Coverage | Detail |",
            "| --- | --- | --- |",
            f"| Lines | **{line_pct:.2f}%** | {line_covered} / {line_total} "
            f"(missing {line_missing}) |",
            f"| Branches | **{branch_pct:.2f}%** | {branch_covered} / {branch_total} "
            f"(partial {branch_partial}, missing {branch_missing}) |",
            f"| Combined | **{overall_pct:.2f}%** | line + branch |",
            "",
        ]
        Path(summary_file).write_text("\n".join(md), encoding="utf-8")

    failed = []
    if line_pct < min_line:
        failed.append(f"line coverage {line_pct:.2f}% < required {min_line:.2f}%")
    if branch_pct < min_branch:
        failed.append(f"branch coverage {branch_pct:.2f}% < required {min_branch:.2f}%")

    if failed:
        print("\nCoverage gate FAILED:", file=sys.stderr)
        for line in failed:
            print(f"  - {line}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--path", default="coverage.json", help="Path to coverage.json")
    parser.add_argument("--min-line", type=float, default=0.0, help="Required line coverage %")
    parser.add_argument("--min-branch", type=float, default=0.0, help="Required branch coverage %")
    args = parser.parse_args()
    sys.exit(main(args.path, args.min_line, args.min_branch))
