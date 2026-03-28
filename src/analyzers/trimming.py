"""Trimming analyzer for sequence quality trimming."""

from src.analyzers.analyzer import BaseAnalyzer
from src.models import Sequence, TrimmingAlgorithm, TrimmingResult


class TrimmingAnalyzer(BaseAnalyzer):
    """Analyzer for trimming low-quality ends from sequences."""

    # Default parameters
    DEFAULT_QUALITY_THRESHOLD = 20
    DEFAULT_WINDOW_SIZE = 10
    DEFAULT_MOTT_CUTOFF = 0.05  # Error probability cutoff

    @property
    def name(self) -> str:
        return "trimming"

    def analyze(
        self,
        sequence: Sequence,
        algorithm: TrimmingAlgorithm = TrimmingAlgorithm.MODIFIED_MOTT,
        **options,
    ) -> TrimmingResult:
        """
        Trim low-quality ends from a sequence.

        Args:
            sequence: Sequence with quality scores
            algorithm: Trimming algorithm to use
            **options: Algorithm-specific options

        Returns:
            TrimmingResult with trimmed sequence
        """
        self.validate_sequence(sequence)
        self.validate_quality(sequence)

        if algorithm == TrimmingAlgorithm.MODIFIED_MOTT:
            return self._trim_modified_mott(sequence, **options)
        elif algorithm == TrimmingAlgorithm.SLIDING_WINDOW:
            return self._trim_sliding_window(sequence, **options)
        elif algorithm == TrimmingAlgorithm.QUALITY_THRESHOLD:
            return self._trim_quality_threshold(sequence, **options)
        else:
            raise ValueError(f"Unknown algorithm: {algorithm}")

    def _trim_modified_mott(
        self, sequence: Sequence, cutoff: float = DEFAULT_MOTT_CUTOFF, **_
    ) -> TrimmingResult:
        """
        Trim using Modified Mott algorithm.

        Based on error probabilities derived from Phred scores.
        Finds the segment with highest cumulative score where:
        score = cutoff - error_probability

        Args:
            sequence: Sequence with quality scores
            cutoff: Error probability cutoff (default 0.05)

        Returns:
            TrimmingResult
        """
        quality = sequence.quality
        seq_str = sequence.sequence

        # Convert quality scores to error probabilities
        # Phred: Q = -10 * log10(P), so P = 10^(-Q/10)
        error_probs = [10 ** (-q / 10) for q in quality]

        # Calculate scores: cutoff - error_probability
        scores = [cutoff - p for p in error_probs]

        # Find maximum scoring segment using Kadane-like algorithm
        # Track cumulative score and best segment
        best_start = 0
        best_end = len(scores)
        best_score = float("-inf")

        for start in range(len(scores)):
            cumsum = 0.0
            for end in range(start, len(scores)):
                cumsum += scores[end]
                if cumsum > best_score:
                    best_score = cumsum
                    best_start = start
                    best_end = end + 1

        # Handle case where no good segment found
        if best_score <= 0:
            # Return empty trimmed sequence
            return TrimmingResult(
                originalLength=len(seq_str),
                trimmedLength=0,
                trimStart=0,
                trimEnd=0,
                trimmedSequence="",
                trimmedQuality=[],
                algorithm=TrimmingAlgorithm.MODIFIED_MOTT.value,
            )

        return TrimmingResult(
            originalLength=len(seq_str),
            trimmedLength=best_end - best_start,
            trimStart=best_start,
            trimEnd=best_end,
            trimmedSequence=seq_str[best_start:best_end],
            trimmedQuality=quality[best_start:best_end],
            algorithm=TrimmingAlgorithm.MODIFIED_MOTT.value,
        )

    def _trim_sliding_window(
        self,
        sequence: Sequence,
        window_size: int = DEFAULT_WINDOW_SIZE,
        threshold: int = DEFAULT_QUALITY_THRESHOLD,
        **_,
    ) -> TrimmingResult:
        """
        Trim using sliding window algorithm.

        Trims from each end until a window with average quality
        above threshold is found.

        Args:
            sequence: Sequence with quality scores
            window_size: Window size
            threshold: Quality threshold

        Returns:
            TrimmingResult
        """
        quality = sequence.quality
        seq_str = sequence.sequence
        n = len(quality)

        if n < window_size:
            # Sequence too short, check overall average
            avg = sum(quality) / n
            if avg >= threshold:
                return TrimmingResult(
                    originalLength=n,
                    trimmedLength=n,
                    trimStart=0,
                    trimEnd=n,
                    trimmedSequence=seq_str,
                    trimmedQuality=quality,
                    algorithm=TrimmingAlgorithm.SLIDING_WINDOW.value,
                )
            else:
                return TrimmingResult(
                    originalLength=n,
                    trimmedLength=0,
                    trimStart=0,
                    trimEnd=0,
                    trimmedSequence="",
                    trimmedQuality=[],
                    algorithm=TrimmingAlgorithm.SLIDING_WINDOW.value,
                )

        # Find start: first position where window average >= threshold
        trim_start = 0
        for i in range(n - window_size + 1):
            window = quality[i : i + window_size]
            if sum(window) / window_size >= threshold:
                trim_start = i
                break
        else:
            # No good window found
            return TrimmingResult(
                originalLength=n,
                trimmedLength=0,
                trimStart=0,
                trimEnd=0,
                trimmedSequence="",
                trimmedQuality=[],
                algorithm=TrimmingAlgorithm.SLIDING_WINDOW.value,
            )

        # Find end: last position where window average >= threshold
        trim_end = n
        for i in range(n - window_size, -1, -1):
            window = quality[i : i + window_size]
            if sum(window) / window_size >= threshold:
                trim_end = i + window_size
                break

        # Ensure valid range
        if trim_end <= trim_start:
            return TrimmingResult(
                originalLength=n,
                trimmedLength=0,
                trimStart=0,
                trimEnd=0,
                trimmedSequence="",
                trimmedQuality=[],
                algorithm=TrimmingAlgorithm.SLIDING_WINDOW.value,
            )

        return TrimmingResult(
            originalLength=n,
            trimmedLength=trim_end - trim_start,
            trimStart=trim_start,
            trimEnd=trim_end,
            trimmedSequence=seq_str[trim_start:trim_end],
            trimmedQuality=quality[trim_start:trim_end],
            algorithm=TrimmingAlgorithm.SLIDING_WINDOW.value,
        )

    def _trim_quality_threshold(
        self, sequence: Sequence, threshold: int = DEFAULT_QUALITY_THRESHOLD, **_
    ) -> TrimmingResult:
        """
        Trim using simple quality threshold.

        Trims from each end while quality is below threshold.

        Args:
            sequence: Sequence with quality scores
            threshold: Quality threshold

        Returns:
            TrimmingResult
        """
        quality = sequence.quality
        seq_str = sequence.sequence
        n = len(quality)

        # Find start: first position with quality >= threshold
        trim_start = 0
        for i, q in enumerate(quality):
            if q >= threshold:
                trim_start = i
                break
        else:
            # All bases below threshold
            return TrimmingResult(
                originalLength=n,
                trimmedLength=0,
                trimStart=0,
                trimEnd=0,
                trimmedSequence="",
                trimmedQuality=[],
                algorithm=TrimmingAlgorithm.QUALITY_THRESHOLD.value,
            )

        # Find end: last position with quality >= threshold
        trim_end = n
        for i in range(n - 1, -1, -1):
            if quality[i] >= threshold:
                trim_end = i + 1
                break

        # Ensure valid range
        if trim_end <= trim_start:
            return TrimmingResult(
                originalLength=n,
                trimmedLength=0,
                trimStart=0,
                trimEnd=0,
                trimmedSequence="",
                trimmedQuality=[],
                algorithm=TrimmingAlgorithm.QUALITY_THRESHOLD.value,
            )

        return TrimmingResult(
            originalLength=n,
            trimmedLength=trim_end - trim_start,
            trimStart=trim_start,
            trimEnd=trim_end,
            trimmedSequence=seq_str[trim_start:trim_end],
            trimmedQuality=quality[trim_start:trim_end],
            algorithm=TrimmingAlgorithm.QUALITY_THRESHOLD.value,
        )
