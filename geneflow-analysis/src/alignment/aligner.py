"""Base aligner interface."""

from abc import ABC, abstractmethod

from src.models import AlignmentResult, AlignmentType


class BaseAligner(ABC):
    """Abstract base class for sequence aligners."""

    @property
    @abstractmethod
    def alignment_type(self) -> AlignmentType:
        """Type of alignment this aligner performs."""
        pass

    @abstractmethod
    def align(self, sequences: list[str], **options) -> AlignmentResult:
        """
        Align sequences.

        Args:
            sequences: List of sequences to align
            **options: Aligner-specific options

        Returns:
            AlignmentResult with aligned sequences
        """
        pass

    def validate_sequences(self, sequences: list[str], min_count: int = 2) -> None:
        """Validate input sequences."""
        if len(sequences) < min_count:
            raise ValueError(f"Need at least {min_count} sequences, got {len(sequences)}")

        for i, seq in enumerate(sequences):
            if not seq:
                raise ValueError(f"Empty sequence at index {i}")

    def calculate_identity(self, aligned_seqs: list[str]) -> float:
        """Calculate percent identity between aligned sequences."""
        if len(aligned_seqs) < 2:
            return 100.0

        seq1, seq2 = aligned_seqs[0], aligned_seqs[1]
        if len(seq1) != len(seq2):
            raise ValueError("Aligned sequences must have equal length")

        matches = sum(1 for a, b in zip(seq1, seq2) if a == b and a != "-")
        total = len(seq1)

        return round((matches / total) * 100, 2) if total > 0 else 0.0

    def count_gaps(self, aligned_seqs: list[str]) -> int:
        """Count total gaps in aligned sequences."""
        return sum(seq.count("-") for seq in aligned_seqs)
