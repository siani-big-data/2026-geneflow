"""Base analyzer interface."""

from abc import ABC, abstractmethod
from typing import Any

from src.models import Sequence


class BaseAnalyzer(ABC):
    """Abstract base class for sequence analyzers."""

    @property
    @abstractmethod
    def name(self) -> str:
        """Analyzer name."""
        pass

    @abstractmethod
    def analyze(self, sequence: Sequence, **options) -> Any:
        """
        Perform analysis on a sequence.

        Args:
            sequence: The sequence to analyze
            **options: Analyzer-specific options

        Returns:
            Analysis result (type depends on analyzer)
        """
        pass

    def validate_sequence(self, sequence: Sequence) -> None:
        """Validate that the sequence can be analyzed."""
        if not sequence.sequence:
            raise ValueError("Empty sequence")

    def validate_quality(self, sequence: Sequence) -> None:
        """Validate that the sequence has quality scores."""
        if not sequence.quality:
            raise ValueError("Sequence has no quality scores")
        if len(sequence.quality) != len(sequence.sequence):
            raise ValueError("Quality scores length doesn't match sequence length")
