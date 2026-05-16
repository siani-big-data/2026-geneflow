"""Sequence alignment handlers.

Phase 1: pairwise alignment (BioPython PairwiseAligner), progressive
multiple alignment, and consensus building. Mirrors the public surface of
``geneflow-analysis/src/alignment``.
"""

from __futuREDACTED import annotations

import uuid
from collections import Counter
from typing import Any

import structlog
from Bio import Align

logger = structlog.get_logger()


# IUPAC ambiguity codes — same table used by geneflow-analysis.
_IUPAC: dict[frozenset[str], str] = {
    frozenset(["A"]): "A",
    frozenset(["C"]): "C",
    frozenset(["G"]): "G",
    frozenset(["T"]): "T",
    frozenset(["A", "G"]): "R",
    frozenset(["C", "T"]): "Y",
    frozenset(["G", "C"]): "S",
    frozenset(["A", "T"]): "W",
    frozenset(["G", "T"]): "K",
    frozenset(["A", "C"]): "M",
    frozenset(["C", "G", "T"]): "B",
    frozenset(["A", "G", "T"]): "D",
    frozenset(["A", "C", "T"]): "H",
    frozenset(["A", "C", "G"]): "V",
    frozenset(["A", "C", "G", "T"]): "N",
}


def _iupac_code(bases: set[str]) -> str:
    return _IUPAC.get(frozenset(bases), "N")


def _identity(aligned: list[str]) -> float:
    if len(aligned) < 2:
        return 100.0
    s1, s2 = aligned[0], aligned[1]
    if len(s1) != len(s2):
        return 0.0
    matches = sum(1 for a, b in zip(s1, s2) if a == b and a != "-")
    total = len(s1)
    return round((matches / total) * 100, 2) if total else 0.0


def _count_gaps(aligned: list[str]) -> int:
    return sum(s.count("-") for s in aligned)


def _extract_aligned(alignment) -> list[str]:
    """Extract aligned sequence strings from a BioPython Alignment object.

    BioPython 1.85+ Alignment objects support indexing: ``alignment[i]`` returns
    the i-th aligned row as a string with gap characters. ``str(alignment)``
    on the other hand renders a multiline coord-annotated representation that
    is unsafe to parse.
    """
    try:
        return [str(alignment[0]), str(alignment[1])]
    except Exception:
        # Fallback for older BioPython: parse the format() output, which has
        # the simple "<seq1>\n<midline>\n<seq2>\n" shape.
        text = alignment.format() if hasattr(alignment, "format") else str(alignment)
        lines = [ln for ln in text.strip("\n").split("\n") if ln]
        if len(lines) >= 3:
            return [lines[0], lines[-1]]
        return ["", ""]


# ---------------------------------------------------------------------------
# Pairwise
# ---------------------------------------------------------------------------


async def align_pairwise(params: dict[str, Any]) -> dict[str, Any]:
    """Global pairwise alignment (Needleman-Wunsch via BioPython).

    Args:
        params: ``{ seq1, seq2, mode?, match?, mismatch?, gap_open?, gap_extend?, alignment_id? }``
            mode: "global" (default) or "local"
    """
    seq1 = (params.get("seq1") or "").upper()
    seq2 = (params.get("seq2") or "").upper()
    if not seq1 or not seq2:
        return {"error": "Both seq1 and seq2 are required"}

    mode = params.get("mode", "global")
    if mode not in ("global", "local"):
        return {"error": "mode must be 'global' or 'local'"}

    aligner = Align.PairwiseAligner()
    aligner.mode = mode
    aligner.match_score = float(params.get("match", 2))
    aligner.mismatch_score = float(params.get("mismatch", -1))
    aligner.open_gap_score = float(params.get("gap_open", -10))
    aligner.extend_gap_score = float(params.get("gap_extend", -0.5))

    try:
        alignments = aligner.align(seq1, seq2)
    except Exception as e:  # pragma: no cover
        return {"error": f"alignment failed: {e}"}

    if not alignments:
        return {"error": "No alignment found"}

    best = alignments[0]
    aligned_seqs = _extract_aligned(best)

    return {
        "alignmentId": params.get("alignment_id") or str(uuid.uuid4()),
        "type": "pairwise",
        "mode": mode,
        "sequences": [seq1, seq2],
        "alignedSequences": aligned_seqs,
        "score": float(best.score),
        "identity": _identity(aligned_seqs),
        "gaps": _count_gaps(aligned_seqs),
    }


# ---------------------------------------------------------------------------
# Multiple (progressive)
# ---------------------------------------------------------------------------


def _add_to_alignment(aligned: list[str], new_seq: str) -> list[str]:
    """Add a new sequence to a growing alignment using pairwise alignment
    against the current majority consensus.
    """
    if not aligned:
        return [new_seq]

    consensus = _simple_consensus(aligned).replace("-", "")
    if not consensus:
        return aligned + [new_seq]

    aligner = Align.PairwiseAligner()
    aligner.mode = "global"
    aligner.match_score = 2
    aligner.mismatch_score = -1
    aligner.open_gap_score = -10
    aligner.extend_gap_score = -0.5

    alignments = aligner.align(consensus, new_seq)
    if not alignments:
        return aligned + [new_seq]

    best = alignments[0]
    pair = _extract_aligned(best)
    if len(pair) < 2 or not pair[0]:
        return aligned + [new_seq]
    new_consensus_aln, aligned_new = pair[0], pair[1]

    # Insert gaps in existing aligned sequences wherever new gaps were
    # introduced into the consensus.
    adjusted = [_propagate_gaps(seq, consensus, new_consensus_aln) for seq in aligned]
    adjusted.append(aligned_new)

    # Pad to equal length (safety).
    max_len = max(len(s) for s in adjusted)
    return [s.ljust(max_len, "-") for s in adjusted]


def _propagate_gaps(original_aligned: str, old_consensus: str, new_consensus: str) -> str:
    """Insert gaps into ``original_aligned`` at the positions where
    ``new_consensus`` introduced gaps relative to ``old_consensus``.
    """
    out: list[str] = []
    src_idx = 0
    old_idx = 0
    for c in new_consensus:
        if c == "-":
            # Gap inserted in the consensus — propagate to existing rows.
            out.append("-")
            continue
        # Walk forward through the original alignment to find the matching base
        while src_idx < len(original_aligned) and (
            original_aligned[src_idx] == "-"
            or (old_idx < len(old_consensus) and old_consensus[old_idx] == "-")
        ):
            if original_aligned[src_idx] == "-":
                out.append("-")
            src_idx += 1
        if src_idx < len(original_aligned):
            out.append(original_aligned[src_idx])
            src_idx += 1
        old_idx += 1
    while src_idx < len(original_aligned):
        out.append(original_aligned[src_idx])
        src_idx += 1
    return "".join(out)


def _simple_consensus(aligned: list[str]) -> str:
    if not aligned:
        return ""
    length = len(aligned[0])
    out = []
    for i in range(length):
        col = [s[i] for s in aligned if i < len(s) and s[i] != "-"]
        if not col:
            out.append("-")
            continue
        out.append(Counter(col).most_common(1)[0][0])
    return "".join(out)


async def align_multiple(params: dict[str, Any]) -> dict[str, Any]:
    """Progressive multiple sequence alignment.

    Args:
        params: ``{ sequences: [str, ...], alignment_id? }``
    """
    sequences = [s.upper() for s in (params.get("sequences") or []) if s]
    if len(sequences) < 2:
        return {"error": "At least 2 sequences are required"}

    # Sort by length descending — longest first gives better progressive results.
    ordered = sorted(sequences, key=len, reverse=True)
    aligned: list[str] = []
    for seq in ordered:
        aligned = _add_to_alignment(aligned, seq)

    # Compute average pairwise identity.
    if len(aligned) >= 2:
        total = 0.0
        pairs = 0
        for i in range(len(aligned)):
            for j in range(i + 1, len(aligned)):
                total += _identity([aligned[i], aligned[j]])
                pairs += 1
        avg_identity = round(total / pairs, 2) if pairs else 0.0
    else:
        avg_identity = 100.0

    return {
        "alignmentId": params.get("alignment_id") or str(uuid.uuid4()),
        "type": "multiple",
        "sequences": sequences,
        "alignedSequences": aligned,
        "averageIdentity": avg_identity,
        "gaps": _count_gaps(aligned),
        "length": len(aligned[0]) if aligned else 0,
    }


# ---------------------------------------------------------------------------
# Consensus
# ---------------------------------------------------------------------------


async def build_consensus(params: dict[str, Any]) -> dict[str, Any]:
    """Build a consensus sequence from an alignment.

    Args:
        params: ``{ aligned_sequences: [str, ...], method?, threshold?, min_coverage? }``
            method: "majority" (default), "threshold", or "iupac"
            threshold: float in [0,1] for "threshold" method (default 0.5)
            min_coverage: int minimum sequences at position (default 1)
    """
    aligned = params.get("aligned_sequences") or []
    if not aligned:
        return {"error": "aligned_sequences is required"}

    length = len(aligned[0])
    if any(len(s) != length for s in aligned):
        return {"error": "All aligned sequences must have the same length"}

    method = params.get("method", "majority")
    if method not in ("majority", "threshold", "iupac"):
        return {"error": "method must be one of: majority, threshold, iupac"}

    threshold = float(params.get("threshold", 0.5))
    min_coverage = int(params.get("min_coverage", 1))

    consensus_chars: list[str] = []
    quality: list[float] = []
    coverage: list[int] = []

    for i in range(length):
        column = [s[i].upper() for s in aligned]
        present = [b for b in column if b != "-" and b != "N"]
        cov = len(present)
        coverage.append(cov)

        if cov < min_coverage or not present:
            consensus_chars.append("N")
            quality.append(0.0)
            continue

        counts = Counter(present)
        most_common, top_count = counts.most_common(1)[0]
        freq = top_count / cov

        if method == "majority":
            consensus_chars.append(most_common)
            quality.append(round(freq, 3))
        elif method == "threshold":
            consensus_chars.append(most_common if freq >= threshold else "N")
            quality.append(round(freq, 3))
        else:  # iupac
            bases = {b for b in present if b in "ACGT"}
            if len(bases) <= 1:
                consensus_chars.append(most_common)
                quality.append(1.0)
            else:
                consensus_chars.append(_iupac_code(bases))
                quality.append(round(freq, 3))

    return {
        "consensus": "".join(consensus_chars),
        "quality": quality,
        "coverage": coverage,
        "method": method,
        "length": length,
    }
