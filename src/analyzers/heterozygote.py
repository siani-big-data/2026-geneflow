"""Heterozygote detection analyzer."""

from dataclasses import dataclass

from src.analyzers.analyzer import BaseAnalyzer
from src.constants import get_iupac_code
from src.models import ChromatogramData, HeterozygoteCall, Sequence


@dataclass
class HeterozygoteResult:
    """Result of heterozygote detection."""

    calls: list[HeterozygoteCall]
    totalPositions: int
    heterozygoteCount: int
    heterozygoteRate: float


class HeterozygoteAnalyzer(BaseAnalyzer):
    """
    Analyzer for detecting heterozygote positions.

    Detects positions where two bases are present based on:
    - Chromatogram peak analysis (if available)
    - IUPAC ambiguity codes in sequence
    - Quality score patterns
    """

    # Default thresholds
    DEFAULT_MIN_RATIO = 0.3  # Secondary peak must be >= 30% of primary
    DEFAULT_MAX_RATIO = 0.7  # Secondary peak must be <= 70% of primary
    DEFAULT_MIN_CONFIDENCE = 0.5

    @property
    def name(self) -> str:
        return "heterozygote"

    def analyze(
        self,
        sequence: Sequence,
        chromatogram: ChromatogramData | None = None,
        min_ratio: float = DEFAULT_MIN_RATIO,
        max_ratio: float = DEFAULT_MAX_RATIO,
        min_confidence: float = DEFAULT_MIN_CONFIDENCE,
        **_,
    ) -> HeterozygoteResult:
        """
        Detect heterozygote positions in a sequence.

        Args:
            sequence: Sequence to analyze
            chromatogram: Optional chromatogram data for peak analysis
            min_ratio: Minimum secondary/primary peak ratio
            max_ratio: Maximum secondary/primary peak ratio
            min_confidence: Minimum confidence threshold

        Returns:
            HeterozygoteResult with detected heterozygote calls
        """
        self.validate_sequence(sequence)

        calls = []

        if chromatogram:
            # Use chromatogram peak analysis
            calls = self._detect_from_chromatogram(
                sequence,
                chromatogram,
                min_ratio,
                max_ratio,
                min_confidence,
            )
        else:
            # Fall back to IUPAC code detection
            calls = self._detect_from_iupac(sequence)

        total = len(sequence.sequence)
        count = len(calls)
        rate = round(count / total, 4) if total > 0 else 0.0

        return HeterozygoteResult(
            calls=calls,
            totalPositions=total,
            heterozygoteCount=count,
            heterozygoteRate=rate,
        )

    def _detect_from_chromatogram(
        self,
        sequence: Sequence,
        chromatogram: ChromatogramData,
        min_ratio: float,
        max_ratio: float,
        min_confidence: float,
    ) -> list[HeterozygoteCall]:
        """Detect heterozygotes from chromatogram peak analysis."""
        calls = []

        traces = {
            "A": chromatogram.traceA,
            "C": chromatogram.traceC,
            "G": chromatogram.traceG,
            "T": chromatogram.traceT,
        }

        peak_locations = chromatogram.peakLocations

        for i, pos in enumerate(peak_locations):
            if i >= len(sequence.sequence):
                break

            # Get peak heights at this position
            heights = {}
            for base, trace in traces.items():
                if pos < len(trace):
                    heights[base] = trace[pos]
                else:
                    heights[base] = 0

            # Sort by height
            sorted_bases = sorted(heights.items(), key=lambda x: x[1], reverse=True)

            if len(sorted_bases) < 2:
                continue

            primary_base, primary_height = sorted_bases[0]
            secondary_base, secondary_height = sorted_bases[1]

            if primary_height == 0:
                continue

            ratio = secondary_height / primary_height

            # Check if it's a heterozygote
            if min_ratio <= ratio <= max_ratio:
                # Calculate confidence based on ratio proximity to 0.5
                # Perfect heterozygote would have ratio = 1.0 (equal peaks)
                confidence = 1.0 - abs(ratio - 0.5) * 2

                if confidence >= min_confidence:
                    iupac = get_iupac_code({primary_base, secondary_base})

                    calls.append(
                        HeterozygoteCall(
                            position=i,
                            base1=primary_base,
                            base2=secondary_base,
                            iupacCode=iupac,
                            ratio=round(ratio, 3),
                            confidence=round(confidence, 3),
                        )
                    )

        return calls

    def _detect_from_iupac(self, sequence: Sequence) -> list[HeterozygoteCall]:
        """Detect heterozygotes from IUPAC ambiguity codes."""
        calls = []

        # IUPAC codes that represent two bases
        iupac_pairs = {
            "R": ("A", "G"),  # Purine
            "Y": ("C", "T"),  # Pyrimidine
            "S": ("G", "C"),  # Strong
            "W": ("A", "T"),  # Weak
            "K": ("G", "T"),  # Keto
            "M": ("A", "C"),  # Amino
        }

        for i, base in enumerate(sequence.sequence.upper()):
            if base in iupac_pairs:
                base1, base2 = iupac_pairs[base]

                calls.append(
                    HeterozygoteCall(
                        position=i,
                        base1=base1,
                        base2=base2,
                        iupacCode=base,
                        ratio=0.5,  # Assumed equal
                        confidence=0.8,  # Moderate confidence from sequence
                    )
                )

        return calls

    def summarize(self, result: HeterozygoteResult) -> dict:
        """Create summary statistics from heterozygote result."""
        if not result.calls:
            return {
                "count": 0,
                "rate": 0.0,
                "positions": [],
                "iupacCodes": {},
            }

        # Count IUPAC codes
        iupac_counts = {}
        for call in result.calls:
            code = call.iupacCode
            iupac_counts[code] = iupac_counts.get(code, 0) + 1

        return {
            "count": result.heterozygoteCount,
            "rate": result.heterozygoteRate,
            "positions": [c.position for c in result.calls],
            "iupacCodes": iupac_counts,
            "avgConfidence": round(sum(c.confidence for c in result.calls) / len(result.calls), 3),
        }
