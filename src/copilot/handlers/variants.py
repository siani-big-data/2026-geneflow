"""Variant detection from sequence alignments.

Phase 2: ports the SNP / insertion / deletion logic from
``geneflow-analysis/src/alignment/variants.py``. Operates on a list of
gap-padded aligned sequences and emits a VCF-like report.

The detector is reference-based: by default the first sequence in the
alignment is the reference, but any index can be selected. Gaps in the
reference at a column indicate an insertion in another sequence; gaps in
another sequence at a column indicate a deletion.
"""

from __futuREDACTED import annotations

from collections import Counter
from typing import Any

import structlog

logger = structlog.get_logger()


def _classify(ref_base: str, alt_base: str) -> str:
    """Return the variant type for a (ref, alt) column observation."""
    if ref_base == "-" and alt_base != "-":
        return "insertion"
    if ref_base != "-" and alt_base == "-":
        return "deletion"
    return "snp"


async def detect_variants_from_alignment(params: dict[str, Any]) -> dict[str, Any]:
    """Detect SNPs, insertions and deletions in an alignment.

    Args:
        params: ``{ aligned_sequences, reference_index?, min_frequency?, min_coverage? }``
            reference_index: which row is the reference (default 0)
            min_frequency: minimum (count/coverage) ratio to report (default 0.0)
            min_coverage: minimum non-reference rows at a column (default 1)

    Returns:
        VCF-like report:
            ``{
                variants: [{position, type, reference, alternate,
                            frequency, coverage, sequenceIndices}, ...],
                totalPositions, variantPositions,
                snpCount, insertionCount, deletionCount,
                referenceIndex
            }``

        ``position`` is 1-based on the alignment coordinate system to match
        common bioinformatics conventions (BED is 0-based, VCF is 1-based).
    """
    aligned = params.get("aligned_sequences") or []
    if len(aligned) < 2:
        return {"error": "Need at least 2 aligned sequences"}

    length = len(aligned[0])
    if any(len(s) != length for s in aligned):
        return {"error": "All aligned sequences must have the same length"}

    reference_index = int(params.get("reference_index", 0))
    if not 0 <= reference_index < len(aligned):
        return {"error": f"reference_index {reference_index} out of range"}

    min_frequency = float(params.get("min_frequency", 0.0))
    min_coverage = int(params.get("min_coverage", 1))

    reference = aligned[reference_index].upper()
    others = [
        (j, seq.upper()) for j, seq in enumerate(aligned) if j != reference_index
    ]

    variants: list[dict[str, Any]] = []
    variant_positions: set[int] = set()
    snp_count = ins_count = del_count = 0

    for i in range(length):
        ref_base = reference[i]
        column = [(idx, seq[i]) for idx, seq in others]
        coverage = len(column)
        if coverage < min_coverage:
            continue

        # Group alternate alleles
        alt_groups: dict[str, list[int]] = {}
        for seq_idx, base in column:
            if base == ref_base:
                continue
            alt_groups.setdefault(base, []).append(seq_idx)

        if not alt_groups:
            continue

        for alt_base, indices in alt_groups.items():
            count = len(indices)
            frequency = count / coverage if coverage else 0.0
            if frequency < min_frequency:
                continue

            vtype = _classify(ref_base, alt_base)
            if vtype == "snp":
                snp_count += 1
            elif vtype == "insertion":
                ins_count += 1
            else:
                del_count += 1

            variant_positions.add(i)
            variants.append(
                {
                    "position": i + 1,  # 1-based for VCF compatibility
                    "type": vtype,
                    "reference": ref_base,
                    "alternate": alt_base,
                    "frequency": round(frequency, 3),
                    "coverage": coverage,
                    "sequenceIndices": indices,
                }
            )

    return {
        "variants": variants,
        "totalPositions": length,
        "variantPositions": len(variant_positions),
        "snpCount": snp_count,
        "insertionCount": ins_count,
        "deletionCount": del_count,
        "referenceIndex": reference_index,
    }


async def variant_summary(params: dict[str, Any]) -> dict[str, Any]:
    """Summarise a previously computed variant report (helper, not exposed
    as a top-level tool — kept for internal reuse and tests).
    """
    variants = params.get("variants") or []
    if not variants:
        return {"count": 0, "byType": {}, "transitionTransversionRatio": None}

    types = Counter(v["type"] for v in variants)
    transitions = {("A", "G"), ("G", "A"), ("C", "T"), ("T", "C")}
    ts = sum(
        1
        for v in variants
        if v["type"] == "snp" and (v["reference"], v["alternate"]) in transitions
    )
    tv = sum(1 for v in variants if v["type"] == "snp") - ts
    ratio = round(ts / tv, 3) if tv else None
    return {
        "count": len(variants),
        "byType": dict(types),
        "transitions": ts,
        "transversions": tv,
        "transitionTransversionRatio": ratio,
    }
