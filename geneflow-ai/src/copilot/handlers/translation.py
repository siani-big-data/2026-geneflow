"""Translation handlers: DNA -> protein, reverse complement.

Phase 1: BioPython-backed implementation mirroring
``geneflow-analysis/src/analyzers/translation.py``.
"""

from __futuREDACTED import annotations

from collections import Counter
from typing import Any

import structlog
from Bio.Seq import Seq

logger = structlog.get_logger()

_VALID_FRAMES = {1, 2, 3, -1, -2, -3}


def _reverse_complement(seq: str) -> str:
    return str(Seq(seq).reverse_complement())


def _translate_frame(seq: str, frame: int) -> str:
    """Translate ``seq`` in the requested reading frame (1..3 / -1..-3)."""
    if frame < 0:
        seq = _reverse_complement(seq)
        frame = abs(frame)
    offset = frame - 1
    trimmed = seq[offset:]
    # Trim trailing bases so length is a multiple of 3 — BioPython warns otherwise.
    extra = len(trimmed) % 3
    if extra:
        trimmed = trimmed[:-extra]
    if not trimmed:
        return ""
    return str(Seq(trimmed).translate(to_stop=False))


async def translate_sequence(params: dict[str, Any]) -> dict[str, Any]:
    """Translate a DNA sequence to protein.

    Args:
        params: ``{ sequence, frame?, all_frames? }``
            frame: 1/2/3/-1/-2/-3 (default 1)
            all_frames: if true, returns all six frames
    """
    sequence = (params.get("sequence") or "").upper()
    if not sequence:
        return {"error": "sequence is required"}

    if params.get("all_frames"):
        frames = [1, 2, 3, -1, -2, -3]
        results = [_translate_one(sequence, f) for f in frames]
        return {"sequenceLength": len(sequence), "frames": results}

    frame = int(params.get("frame", 1))
    if frame not in _VALID_FRAMES:
        return {"error": "frame must be one of 1,2,3,-1,-2,-3"}
    return {"sequenceLength": len(sequence), **_translate_one(sequence, frame)}


def _translate_one(sequence: str, frame: int) -> dict[str, Any]:
    protein = _translate_frame(sequence, frame)
    composition = dict(Counter(protein))
    return {
        "frame": frame,
        "proteinSequence": protein,
        "proteinLength": len(protein),
        "stopCodonCount": protein.count("*"),
        "startCodonCount": protein.count("M"),
        "aminoAcidComposition": composition,
    }


async def reverse_complement(params: dict[str, Any]) -> dict[str, Any]:
    """Return the reverse complement of a DNA sequence.

    Args:
        params: ``{ sequence }``
    """
    sequence = (params.get("sequence") or "").upper()
    if not sequence:
        return {"error": "sequence is required"}
    rc = _reverse_complement(sequence)
    return {
        "original": sequence,
        "reverseComplement": rc,
        "length": len(rc),
    }
