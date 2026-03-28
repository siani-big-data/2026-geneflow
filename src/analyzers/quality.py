"""Quality analyzer for sequence quality metrics."""

import math
from typing import Optional

from src.models import QualityMetrics, Sequence, ChromatogramData
from src.analyzers.analyzer import BaseAnalyzer
from src.constants import gc_content


class QualityAnalyzer(BaseAnalyzer):
    """Analyzer for computing sequence quality metrics."""

    @property
    def name(self) -> str:
        return "quality"

    def analyze(
        self,
        sequence: Sequence,
        chromatogram: Optional[ChromatogramData] = None,
        **options,
    ) -> QualityMetrics:
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

        # Calculate SNR if chromatogram is available
        snr = None
        if chromatogram is not None:
            snr = self.calculate_snr(chromatogram)

        # If no quality scores, return basic metrics
        if not quality:
            return QualityMetrics(
                meanQuality=0.0,
                q20Percentage=0.0,
                q30Percentage=0.0,
                gcContent=round(gc, 2),
                length=len(seq_str),
                ambiguousCount=ambiguous,
                snr=snr,
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
            snr=snr,
        )

    def calculate_snr(self, chromatogram: ChromatogramData) -> float:
        """
        Calculate Signal-to-Noise Ratio from chromatogram data.

        SNR measures the quality of chromatogram peaks relative to background noise.
        Higher values indicate cleaner, more reliable data.

        Formula: SNR = mean(peak_signals) / std(noise)

        Args:
            chromatogram: Chromatogram data with trace channels

        Returns:
            SNR value (typically 10-100 for good quality data)
        """
        traces = [
            chromatogram.traceA,
            chromatogram.traceC,
            chromatogram.traceG,
            chromatogram.traceT,
        ]

        if not chromatogram.peakLocations:
            return 0.0

        # Calculate signal: max intensity at each peak position
        peak_signals = []
        for peak_pos in chromatogram.peakLocations:
            if 0 <= peak_pos < len(traces[0]):
                max_intensity = max(trace[peak_pos] for trace in traces)
                peak_signals.append(max_intensity)

        if not peak_signals:
            return 0.0

        mean_signal = sum(peak_signals) / len(peak_signals)

        # Calculate noise: standard deviation of non-peak regions
        # Sample points between peaks for noise estimation
        noise_values = []
        peak_set = set(chromatogram.peakLocations)

        for i in range(len(traces[0])):
            # Skip peak positions and nearby (±2 positions)
            if any(abs(i - p) <= 2 for p in peak_set):
                continue
            # Get minimum value across channels (background)
            min_val = min(trace[i] for trace in traces)
            noise_values.append(min_val)

        if len(noise_values) < 2:
            # Not enough noise samples, estimate from peak variation
            if len(peak_signals) < 2:
                return mean_signal  # No noise reference
            noise_std = math.sqrt(
                sum((x - mean_signal) ** 2 for x in peak_signals) / len(peak_signals)
            )
        else:
            noise_mean = sum(noise_values) / len(noise_values)
            noise_std = math.sqrt(
                sum((x - noise_mean) ** 2 for x in noise_values) / len(noise_values)
            )

        if noise_std == 0:
            return float("inf") if mean_signal > 0 else 0.0

        snr = mean_signal / noise_std
        return round(snr, 2)

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
