"""Consensus sequence builder from multiple sequence alignment."""

from dataclasses import dataclass
from enum import Enum

from src.constants import get_iupac_code


class ConsensusMethod(str, Enum):
    """Method for building consensus."""

    MAJORITY = "majority"
    THRESHOLD = "threshold"
    IUPAC = "iupac"


@dataclass
class ConsensusResult:
    """Result of consensus building."""

    consensus: str
    quality: list[float]
    coverage: list[int]
    method: ConsensusMethod


class ConsensusBuilder:
    """
    Build consensus sequence from aligned sequences.

    Supports multiple methods:
    - MAJORITY: Most frequent base at each position
    - THRESHOLD: Base must meet minimum frequency threshold
    - IUPAC: Use IUPAC ambiguity codes for mixed positions
    """

    def build(
        self,
        aligned_sequences: list[str],
        method: ConsensusMethod = ConsensusMethod.MAJORITY,
        threshold: float = 0.5,
        min_coverage: int = 1,
    ) -> ConsensusResult:
        """
        Build consensus from aligned sequences.

        Args:
            aligned_sequences: List of aligned sequences (same length)
            method: Consensus method to use
            threshold: Minimum frequency for THRESHOLD method (0.0-1.0)
            min_coverage: Minimum sequences required at position

        Returns:
            ConsensusResult with consensus sequence and quality info
        """
        if not aligned_sequences:
            return ConsensusResult(
                consensus="",
                quality=[],
                coverage=[],
                method=method,
            )

        length = len(aligned_sequences[0])
        for seq in aligned_sequences:
            if len(seq) != length:
                raise ValueError("All aligned sequences must have the same length")

        consensus = []
        quality = []
        coverage = []

        for i in range(length):
            column = [seq[i].upper() for seq in aligned_sequences]
            base, freq, cov = self._consensus_at_position(column, method, threshold, min_coverage)
            consensus.append(base)
            quality.append(freq)
            coverage.append(cov)

        return ConsensusResult(
            consensus="".join(consensus),
            quality=quality,
            coverage=coverage,
            method=method,
        )

    def _consensus_at_position(
        self,
        column: list[str],
        method: ConsensusMethod,
        threshold: float,
        min_coverage: int,
    ) -> tuple[str, float, int]:
        """
        Determine consensus base at a single position.

        Returns:
            Tuple of (base, frequency, coverage)
        """
        bases = [b for b in column if b != "-"]
        coverage = len(bases)

        if coverage < min_coverage:
            return ("N", 0.0, coverage)

        if not bases:
            return ("-", 0.0, 0)

        base_counts = {}
        for base in bases:
            base_counts[base] = base_counts.get(base, 0) + 1

        most_common = max(base_counts.keys(), key=lambda b: base_counts[b])
        frequency = base_counts[most_common] / len(bases)

        if method == ConsensusMethod.MAJORITY:
            return (most_common, round(frequency, 3), coverage)

        elif method == ConsensusMethod.THRESHOLD:
            if frequency >= threshold:
                return (most_common, round(frequency, 3), coverage)
            else:
                return ("N", round(frequency, 3), coverage)

        elif method == ConsensusMethod.IUPAC:
            present_bases = set(bases)

            if len(present_bases) == 1:
                return (most_common, 1.0, coverage)

            iupac = get_iupac_code(present_bases)
            return (iupac, round(frequency, 3), coverage)

        return (most_common, round(frequency, 3), coverage)

    def build_profile(self, aligned_sequences: list[str]) -> list[dict[str, float]]:
        """
        Build position-specific scoring matrix (PSSM) profile.

        Args:
            aligned_sequences: List of aligned sequences

        Returns:
            List of dicts mapping base -> frequency at each position
        """
        if not aligned_sequences:
            return []

        length = len(aligned_sequences[0])
        profile = []

        for i in range(length):
            column = [seq[i].upper() for seq in aligned_sequences if i < len(seq)]
            bases = [b for b in column if b != "-"]

            if not bases:
                profile.append({"A": 0.0, "C": 0.0, "G": 0.0, "T": 0.0, "-": 1.0})
                continue

            total = len(bases)
            freq = {
                "A": bases.count("A") / total,
                "C": bases.count("C") / total,
                "G": bases.count("G") / total,
                "T": bases.count("T") / total,
                "-": column.count("-") / len(column),
            }
            profile.append({k: round(v, 3) for k, v in freq.items()})

        return profile

    def calculate_conservation(self, aligned_sequences: list[str]) -> list[float]:
        """
        Calculate conservation score at each position.

        Uses Shannon entropy-based conservation:
        - 1.0 = fully conserved
        - 0.0 = maximum diversity

        Args:
            aligned_sequences: List of aligned sequences

        Returns:
            List of conservation scores (0.0-1.0)
        """
        import math

        if not aligned_sequences:
            return []

        length = len(aligned_sequences[0])
        conservation = []

        for i in range(length):
            column = [seq[i].upper() for seq in aligned_sequences if i < len(seq)]
            bases = [b for b in column if b != "-"]

            if not bases:
                conservation.append(0.0)
                continue

            total = len(bases)
            freq = {}
            for base in bases:
                freq[base] = freq.get(base, 0) + 1

            entropy = 0.0
            for count in freq.values():
                p = count / total
                if p > 0:
                    entropy -= p * math.log2(p)

            max_entropy = math.log2(4)
            conservation_score = 1.0 - (entropy / max_entropy)

            conservation.append(round(conservation_score, 3))

        return conservation
