"""Distance calculation for phylogenetic analysis."""

import math
from dataclasses import dataclass
from enum import Enum


class DistanceMethod(str, Enum):
    """Distance calculation methods."""

    P_DISTANCE = "p_distance"
    JUKES_CANTOR = "jukes_cantor"
    KIMURA_2P = "kimura_2p"


@dataclass
class DistanceMatrix:
    """Matrix of pairwise evolutionary distances."""

    labels: list[str]
    matrix: list[list[float]]
    method: str

    def to_dict(self) -> dict:
        """Serialize to dictionary."""
        return {
            "labels": self.labels,
            "matrix": self.matrix,
            "method": self.method,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "DistanceMatrix":
        """Create from dictionary."""
        return cls(
            labels=data["labels"],
            matrix=data["matrix"],
            method=data["method"],
        )

    def get_distance(self, label1: str, label2: str) -> float:
        """Get distance between two sequences by label."""
        i = self.labels.index(label1)
        j = self.labels.index(label2)
        return self.matrix[i][j]


class DistanceCalculator:
    """
    Calculate evolutionary distances between aligned sequences.

    Supports multiple distance methods:
    - p-distance: Simple proportion of differing sites
    - Jukes-Cantor: Corrects for multiple substitutions (equal rates)
    - Kimura 2-parameter: Distinguishes transitions and transversions
    """

    PURINES = {"A", "G"}
    PYRIMIDINES = {"C", "T"}

    def calculate(
        self,
        aligned_sequences: list[str],
        labels: list[str] | None = None,
        method: DistanceMethod = DistanceMethod.JUKES_CANTOR,
    ) -> DistanceMatrix:
        """
        Calculate pairwise distance matrix.

        Args:
            aligned_sequences: List of aligned sequences (same length).
            labels: Optional sequence labels. Defaults to seq_0, seq_1, etc.
            method: Distance calculation method.

        Returns:
            DistanceMatrix with pairwise distances.

        Raises:
            ValueError: If sequences have different lengths or < 2 sequences.
        """
        if len(aligned_sequences) < 2:
            raise ValueError("At least 2 sequences required for distance calculation")

        seq_length = len(aligned_sequences[0])
        for i, seq in enumerate(aligned_sequences):
            if len(seq) != seq_length:
                raise ValueError(
                    f"Sequence {i} has length {len(seq)}, expected {seq_length}"
                )

        if labels is None:
            labels = [f"seq_{i}" for i in range(len(aligned_sequences))]
        elif len(labels) != len(aligned_sequences):
            raise ValueError("Number of labels must match number of sequences")

        n = len(aligned_sequences)
        matrix: list[list[float]] = [[0.0] * n for _ in range(n)]

        for i in range(n):
            for j in range(i + 1, n):
                distance = self._calculate_distance(
                    aligned_sequences[i],
                    aligned_sequences[j],
                    method,
                )
                matrix[i][j] = distance
                matrix[j][i] = distance

        return DistanceMatrix(
            labels=labels,
            matrix=matrix,
            method=method.value,
        )

    def _calculate_distance(
        self,
        seq1: str,
        seq2: str,
        method: DistanceMethod,
    ) -> float:
        """Calculate distance between two sequences."""
        if method == DistanceMethod.P_DISTANCE:
            return self._p_distance(seq1, seq2)
        elif method == DistanceMethod.JUKES_CANTOR:
            return self._jukes_cantor(seq1, seq2)
        elif method == DistanceMethod.KIMURA_2P:
            return self._kimura_2_parameter(seq1, seq2)
        else:
            raise ValueError(f"Unknown distance method: {method}")

    def _p_distance(self, seq1: str, seq2: str) -> float:
        """
        Calculate p-distance (proportion of differing sites).

        Gaps and ambiguous bases are excluded from comparison.
        """
        differences = 0
        comparable_sites = 0

        for b1, b2 in zip(seq1.upper(), seq2.upper()):
            if b1 in "-N" or b2 in "-N":
                continue

            comparable_sites += 1
            if b1 != b2:
                differences += 1

        if comparable_sites == 0:
            return 0.0

        return differences / comparable_sites

    def _jukes_cantor(self, seq1: str, seq2: str) -> float:
        """
        Calculate Jukes-Cantor corrected distance.

        Assumes equal substitution rates among all nucleotides.
        Formula: d = -3/4 * ln(1 - 4p/3) where p is p-distance.

        Returns infinity if p >= 0.75 (saturation).
        """
        p = self._p_distance(seq1, seq2)

        if p >= 0.75:
            return float("inf")

        try:
            distance = -0.75 * math.log(1 - (4 * p / 3))
        except ValueError:
            return float("inf")

        return distance

    def _kimura_2_parameter(self, seq1: str, seq2: str) -> float:
        """
        Calculate Kimura 2-parameter distance.

        Distinguishes between transitions (purine<->purine, pyrimidine<->pyrimidine)
        and transversions (purine<->pyrimidine).

        Formula: d = -0.5 * ln((1-2P-Q) * sqrt(1-2Q))
        where P = proportion of transitions, Q = proportion of transversions.
        """
        transitions = 0
        transversions = 0
        comparable_sites = 0

        for b1, b2 in zip(seq1.upper(), seq2.upper()):
            if b1 in "-N" or b2 in "-N":
                continue

            comparable_sites += 1

            if b1 != b2:
                if self._is_transition(b1, b2):
                    transitions += 1
                else:
                    transversions += 1

        if comparable_sites == 0:
            return 0.0

        P = transitions / comparable_sites
        Q = transversions / comparable_sites

        term1 = 1 - 2 * P - Q
        term2 = 1 - 2 * Q

        if term1 <= 0 or term2 <= 0:
            return float("inf")

        try:
            distance = -0.5 * math.log(term1 * math.sqrt(term2))
        except ValueError:
            return float("inf")

        return distance

    def _is_transition(self, base1: str, base2: str) -> bool:
        """
        Check if substitution is a transition.

        Transition: purine <-> purine (A<->G) or pyrimidine <-> pyrimidine (C<->T)
        Transversion: purine <-> pyrimidine
        """
        both_purines = base1 in self.PURINES and base2 in self.PURINES
        both_pyrimidines = base1 in self.PYRIMIDINES and base2 in self.PYRIMIDINES
        return both_purines or both_pyrimidines
