"""Quality analyzer for sequence quality metrics."""

from src.models import QualityMetrics, Sequence
from src.analyzers.analyzer import BaseAnalyzer
from src.constants import gc_content


class QualityAnalyzer(BaseAnalyzer):
    """Analyzer for computing sequence quality metrics."""

    @property
    def name(self) -> str:
        return "quality"

    def analyze(self, sequence: Sequence, **options) -> QualityMetrics:
        """
        Compute quality metrics for a sequence.

        Args:
            sequence: Sequence with optional quality scores

        Returns:
            QualityMetrics with computed statistics
        """
        self.validate_sequence(sequence)

        seq_str = sequence.sequence.upper()
        quality = sequence.quality

        # Calculate GC content
        gc = gc_content(seq_str)

        # Count ambiguous bases (not A, C, G, T)
        ambiguous = sum(1 for b in seq_str if b not in "ACGT")

        # If no quality scores, return basic metrics
        if not quality:
            return QualityMetrics(
                meanQuality=0.0,
                q20Percentage=0.0,
                q30Percentage=0.0,
                gcContent=round(gc, 2),
                length=len(seq_str),
                ambiguousCount=ambiguous,
            )

        # Calculate quality statistics
        mean_q = sum(quality) / len(quality)
        q20_count = sum(1 for q in quality if q >= 20)
        q30_count = sum(1 for q in quality if q >= 30)

        return QualityMetrics(
            meanQuality=round(mean_q, 2),
            q20Percentage=round((q20_count / len(quality)) * 100, 2),
            q30Percentage=round((q30_count / len(quality)) * 100, 2),
            gcContent=round(gc, 2),
            length=len(seq_str),
            ambiguousCount=ambiguous,
        )

    def analyze_window(
        self, sequence: Sequence, window_size: int = 50
    ) -> list[dict]:
        """
        Compute quality metrics in sliding windows.

        Args:
            sequence: Sequence with quality scores
            window_size: Size of each window

        Returns:
            List of metrics per window
        """
        self.validate_sequence(sequence)
        self.validate_quality(sequence)

        quality = sequence.quality
        results = []

        for i in range(0, len(quality), window_size):
            window = quality[i : i + window_size]
            if not window:
                continue

            mean_q = sum(window) / len(window)
            q20_count = sum(1 for q in window if q >= 20)
            q30_count = sum(1 for q in window if q >= 30)

            results.append({
                "start": i,
                "end": min(i + window_size, len(quality)),
                "meanQuality": round(mean_q, 2),
                "q20Percentage": round((q20_count / len(window)) * 100, 2),
                "q30Percentage": round((q30_count / len(window)) * 100, 2),
            })

        return results

    def find_low_quality_regions(
        self, sequence: Sequence, threshold: int = 20, min_length: int = 5
    ) -> list[dict]:
        """
        Find regions with quality below threshold.

        Args:
            sequence: Sequence with quality scores
            threshold: Quality threshold
            min_length: Minimum region length to report

        Returns:
            List of low quality regions
        """
        self.validate_sequence(sequence)
        self.validate_quality(sequence)

        quality = sequence.quality
        regions = []
        start = None

        for i, q in enumerate(quality):
            if q < threshold:
                if start is None:
                    start = i
            else:
                if start is not None:
                    length = i - start
                    if length >= min_length:
                        region_quality = quality[start:i]
                        regions.append({
                            "start": start,
                            "end": i,
                            "length": length,
                            "meanQuality": round(sum(region_quality) / len(region_quality), 2),
                        })
                    start = None

        # Handle region at end
        if start is not None:
            length = len(quality) - start
            if length >= min_length:
                region_quality = quality[start:]
                regions.append({
                    "start": start,
                    "end": len(quality),
                    "length": length,
                    "meanQuality": round(sum(region_quality) / len(region_quality), 2),
                })

        return regions
