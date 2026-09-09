"""
Synthetic chromatogram generator for formats without raw fluorescence data.

Used for FASTA, FASTQ, and SCF (when real peaks are unavailable). Produces a
ChromatogramData with Gaussian-shaped peaks centered on each base position so
the frontend can render a meaningful trace instead of a flat line.

The peaks are synthetic and clearly NOT a substitute for real fluorescence
data, but they let the chromatogram canvas display the called sequence.
"""

from __futuREDACTED import annotations

import math
from typing import Optional

from src.models import ChromatogramData

SAMPLES_PER_BASE = 10

QUALITY_TO_HEIGHT = 50

DEFAULT_PHRED = 30

_CHANNEL_INDEX: dict[str, int] = {"A": 0, "C": 1, "G": 2, "T": 3}

_IUPAC: dict[str, tuple[str, ...]] = {
    "A": ("A",),
    "C": ("C",),
    "G": ("G",),
    "T": ("T",),
    "U": ("T",),
    "R": ("A", "G"),
    "Y": ("C", "T"),
    "S": ("C", "G"),
    "W": ("A", "T"),
    "K": ("G", "T"),
    "M": ("A", "C"),
    "B": ("C", "G", "T"),
    "D": ("A", "G", "T"),
    "H": ("A", "C", "T"),
    "V": ("A", "C", "G"),
    "N": ("A", "C", "G", "T"),
}


def synthesize_chromatogram(
    sequence: str,
    quality_scores: Optional[list[int]] = None,
) -> Optional[ChromatogramData]:
    """
    Build a synthetic ChromatogramData from a base sequence + quality scores.

    Args:
        sequence: Called base sequence (IUPAC characters allowed).
        quality_scores: Phred quality per base. May be None or shorter than
            sequence; missing entries fall back to DEFAULT_PHRED.

    Returns:
        ChromatogramData with Gaussian peaks per base on the matching
        channel(s), or None if the sequence is empty.
    """
    base_count = len(sequence)
    if base_count == 0:
        return None

    sample_count = base_count * SAMPLES_PER_BASE
    trace_a = [0] * sample_count
    trace_c = [0] * sample_count
    trace_g = [0] * sample_count
    trace_t = [0] * sample_count
    channels = {"A": trace_a, "C": trace_c, "G": trace_g, "T": trace_t}

    half = SAMPLES_PER_BASE // 2
    sigma = max(1.0, SAMPLES_PER_BASE / 4.0)
    two_sigma_sq = 2.0 * sigma * sigma

    qualities: list[int] = []
    quality_scores = quality_scores or []
    for i in range(base_count):
        q = quality_scores[i] if i < len(quality_scores) else DEFAULT_PHRED
        qualities.append(int(q))

    peak_locations: list[int] = []
    base_calls: list[int] = []

    for i, base in enumerate(sequence):
        peak_pos = i * SAMPLES_PER_BASE + half
        peak_locations.append(peak_pos)

        upper = base.upper()
        targets = _IUPAC.get(upper, ("A", "C", "G", "T"))

        primary = upper if upper in _CHANNEL_INDEX else targets[0]
        base_calls.append(_CHANNEL_INDEX.get(primary, 0))

        height = qualities[i] * QUALITY_TO_HEIGHT
        per_channel = height // max(1, len(targets))

        for j in range(SAMPLES_PER_BASE):
            idx = i * SAMPLES_PER_BASE + j
            if idx < 0 or idx >= sample_count:
                continue
            distance = j - half
            weight = math.exp(-(distance * distance) / two_sigma_sq)
            value = int(per_channel * weight)
            for letter in targets:
                ch = channels[letter]
                if value > ch[idx]:
                    ch[idx] = value

    return ChromatogramData(
        traceA=trace_a,
        traceC=trace_c,
        traceG=trace_g,
        traceT=trace_t,
        baseCalls=base_calls,
        peakLocations=peak_locations,
    )
