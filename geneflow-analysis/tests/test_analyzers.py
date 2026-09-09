"""Tests for sequence analyzers."""

import pytest

from src.analyzers import QualityAnalyzer, TrimmingAnalyzer
from src.models import ChromatogramData, Sequence, TrimmingAlgorithm


class TestQualityAnalyzer:
    """Tests for QualityAnalyzer."""

    def test_analyze_with_quality(self):
        analyzer = QualityAnalyzer()
        seq = Sequence(
            id="test",
            sequence="ATGCATGC",
            quality=[30, 35, 40, 25, 30, 35, 40, 25],
        )

        result = analyzer.analyze(seq)

        assert result.length == 8
        assert result.gcContent == 50.0
        assert result.meanQuality == 32.5
        assert result.ambiguousCount == 0

    def test_analyze_without_quality(self):
        analyzer = QualityAnalyzer()
        seq = Sequence(id="test", sequence="ATGCATGC")

        result = analyzer.analyze(seq)

        assert result.length == 8
        assert result.gcContent == 50.0
        assert result.meanQuality == 0.0
        assert result.q20Percentage == 0.0

    def test_q20_q30_percentages(self):
        analyzer = QualityAnalyzer()
        # quality=[10, 15, 20, 25, 30, 35, 40, 25, 20, 15]
        # >= Q20: 20, 25, 30, 35, 40, 25, 20 = 7 bases
        # >= Q30: 30, 35, 40 = 3 bases
        seq = Sequence(
            id="test",
            sequence="ATGCATGCAT",
            quality=[10, 15, 20, 25, 30, 35, 40, 25, 20, 15],
        )

        result = analyzer.analyze(seq)

        assert result.q20Percentage == 70.0  # 7/10
        assert result.q30Percentage == 30.0  # 3/10

    def test_gc_content_calculation(self):
        analyzer = QualityAnalyzer()

        # 100% GC
        seq1 = Sequence(id="test", sequence="GCGCGC")
        assert analyzer.analyze(seq1).gcContent == 100.0

        # 0% GC
        seq2 = Sequence(id="test", sequence="ATATAT")
        assert analyzer.analyze(seq2).gcContent == 0.0

        # 50% GC
        seq3 = Sequence(id="test", sequence="ATGC")
        assert analyzer.analyze(seq3).gcContent == 50.0

    def test_ambiguous_bases(self):
        analyzer = QualityAnalyzer()
        seq = Sequence(id="test", sequence="ATGCNATGCN")

        result = analyzer.analyze(seq)

        assert result.ambiguousCount == 2

    def test_empty_sequence_raises_error(self):
        analyzer = QualityAnalyzer()
        seq = Sequence(id="test", sequence="")

        with pytest.raises(ValueError, match="Empty sequence"):
            analyzer.analyze(seq)

    def test_analyze_window(self):
        analyzer = QualityAnalyzer()
        seq = Sequence(
            id="test",
            sequence="A" * 100,
            quality=[30] * 50 + [10] * 50,
        )

        result = analyzer.analyze_window(seq, window_size=50)

        assert len(result) == 2
        assert result[0]["meanQuality"] == 30.0
        assert result[1]["meanQuality"] == 10.0

    def test_find_low_quality_regions(self):
        analyzer = QualityAnalyzer()
        seq = Sequence(
            id="test",
            sequence="A" * 20,
            quality=[
                30,
                30,
                30,
                10,
                10,
                10,
                10,
                10,
                30,
                30,
                30,
                30,
                10,
                10,
                10,
                10,
                10,
                10,
                30,
                30,
            ],
        )

        regions = analyzer.find_low_quality_regions(seq, threshold=20, min_length=5)

        assert len(regions) == 2
        assert regions[0]["start"] == 3
        assert regions[0]["length"] == 5
        assert regions[1]["start"] == 12
        assert regions[1]["length"] == 6

    def test_snr_without_chromatogram(self):
        """Test that SNR is None without chromatogram data."""
        analyzer = QualityAnalyzer()
        seq = Sequence(id="test", sequence="ATGC", quality=[30, 30, 30, 30])

        result = analyzer.analyze(seq)

        assert result.snr is None

    def test_snr_with_chromatogram(self):
        """Test SNR calculation with chromatogram data."""
        analyzer = QualityAnalyzer()
        seq = Sequence(id="test", sequence="ATGC", quality=[30, 30, 30, 30])

        # Create chromatogram with clear peaks
        # Peak positions at 10, 20, 30, 40
        trace_length = 50
        trace_a = [10] * trace_length
        trace_c = [10] * trace_length
        trace_g = [10] * trace_length
        trace_t = [10] * trace_length

        # Add peaks at specific positions
        trace_a[10] = 1000  # A peak
        trace_t[20] = 1000  # T peak
        trace_g[30] = 1000  # G peak
        trace_c[40] = 1000  # C peak

        chromatogram = ChromatogramData(
            traceA=trace_a,
            traceC=trace_c,
            traceG=trace_g,
            traceT=trace_t,
            baseCalls=[ord("A"), ord("T"), ord("G"), ord("C")],
            peakLocations=[10, 20, 30, 40],
        )

        result = analyzer.analyze(seq, chromatogram=chromatogram)

        # SNR should be calculated
        assert result.snr is not None
        assert result.snr > 0

    def test_snr_calculation_good_quality(self):
        """Test SNR is high for good quality chromatogram."""
        analyzer = QualityAnalyzer()

        # High signal (1000) with low noise (10)
        trace_length = 50
        trace_a = [10] * trace_length
        trace_c = [10] * trace_length
        trace_g = [10] * trace_length
        trace_t = [10] * trace_length

        trace_a[10] = 1000
        trace_t[20] = 1000
        trace_g[30] = 1000
        trace_c[40] = 1000

        chromatogram = ChromatogramData(
            traceA=trace_a,
            traceC=trace_c,
            traceG=trace_g,
            traceT=trace_t,
            baseCalls=[ord("A"), ord("T"), ord("G"), ord("C")],
            peakLocations=[10, 20, 30, 40],
        )

        snr = analyzer.calculate_snr(chromatogram)

        # SNR should be high for clean signal
        assert snr > 50

    def test_snr_calculation_poor_quality(self):
        """Test SNR is lower for noisy chromatogram."""
        analyzer = QualityAnalyzer()

        # Variable background noise
        import random

        random.seed(42)
        trace_length = 100

        # Noisy baseline with some peaks
        trace_a = [random.randint(50, 150) for _ in range(trace_length)]
        trace_c = [random.randint(50, 150) for _ in range(trace_length)]
        trace_g = [random.randint(50, 150) for _ in range(trace_length)]
        trace_t = [random.randint(50, 150) for _ in range(trace_length)]

        # Add modest peaks
        trace_a[25] = 200
        trace_t[50] = 200
        trace_g[75] = 200

        chromatogram = ChromatogramData(
            traceA=trace_a,
            traceC=trace_c,
            traceG=trace_g,
            traceT=trace_t,
            baseCalls=[ord("A"), ord("T"), ord("G")],
            peakLocations=[25, 50, 75],
        )

        snr = analyzer.calculate_snr(chromatogram)

        # SNR should be lower for noisy data (compared to 50+ for clean data)
        assert snr < 20

    def test_snr_empty_peaks(self):
        """Test SNR returns 0 for empty peak locations."""
        analyzer = QualityAnalyzer()

        chromatogram = ChromatogramData(
            traceA=[100] * 50,
            traceC=[100] * 50,
            traceG=[100] * 50,
            traceT=[100] * 50,
            baseCalls=[],
            peakLocations=[],
        )

        snr = analyzer.calculate_snr(chromatogram)

        assert snr == 0.0

    def test_snr_in_to_dict(self):
        """Test SNR is included in QualityMetrics.to_dict() when present."""
        analyzer = QualityAnalyzer()
        seq = Sequence(id="test", sequence="ATGC", quality=[30, 30, 30, 30])

        # Without chromatogram - SNR should not be in dict
        result_no_chrom = analyzer.analyze(seq)
        dict_no_chrom = result_no_chrom.to_dict()
        assert "snr" not in dict_no_chrom

        # With chromatogram - SNR should be in dict
        chromatogram = ChromatogramData(
            traceA=[10] * 50,
            traceC=[10] * 50,
            traceG=[10] * 50,
            traceT=[10] * 50,
            baseCalls=[ord("A"), ord("T")],
            peakLocations=[10, 20],
        )
        chromatogram.traceA[10] = 500
        chromatogram.traceT[20] = 500

        result_with_chrom = analyzer.analyze(seq, chromatogram=chromatogram)
        dict_with_chrom = result_with_chrom.to_dict()
        assert "snr" in dict_with_chrom
        assert dict_with_chrom["snr"] is not None


class TestTrimmingAnalyzer:
    """Tests for TrimmingAnalyzer."""

    def test_trim_quality_threshold(self):
        analyzer = TrimmingAnalyzer()
        seq = Sequence(
            id="test",
            sequence="NNNATGCATGCNN",
            quality=[5, 5, 5, 30, 30, 30, 30, 30, 30, 30, 5, 5, 5],
        )

        result = analyzer.analyze(seq, algorithm=TrimmingAlgorithm.QUALITY_THRESHOLD, threshold=20)

        assert result.trimStart == 3
        assert result.trimEnd == 10
        assert result.trimmedSequence == "ATGCATG"
        assert result.trimmedLength == 7
        assert result.originalLength == 13

    def test_trim_sliding_window(self):
        analyzer = TrimmingAnalyzer()
        # Low quality at ends, high quality in middle
        seq = Sequence(
            id="test",
            sequence="A" * 20,
            quality=[5, 5, 5, 5, 5, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 5, 5, 5, 5, 5],
        )

        result = analyzer.analyze(
            seq,
            algorithm=TrimmingAlgorithm.SLIDING_WINDOW,
            window_size=5,
            threshold=30,
        )

        # Finds first/last window where average >= threshold
        # Window at position 4 includes one low Q base but average still >= 30
        assert result.trimStart >= 4
        assert result.trimStart <= 5
        assert result.trimEnd >= 15
        assert result.trimmedLength >= 10
        assert result.algorithm == "sliding_window"

    def test_trim_modified_mott(self):
        analyzer = TrimmingAnalyzer()
        seq = Sequence(
            id="test",
            sequence="A" * 20,
            quality=[5] * 5 + [40] * 10 + [5] * 5,
        )

        result = analyzer.analyze(seq, algorithm=TrimmingAlgorithm.MODIFIED_MOTT, cutoff=0.05)

        # Should find the high-quality region in the middle
        assert result.trimmedLength > 0
        assert result.trimStart >= 0
        assert result.trimEnd <= 20

    def test_all_low_quality_returns_empty(self):
        analyzer = TrimmingAnalyzer()
        seq = Sequence(
            id="test",
            sequence="ATGCATGC",
            quality=[5, 5, 5, 5, 5, 5, 5, 5],
        )

        result = analyzer.analyze(seq, algorithm=TrimmingAlgorithm.QUALITY_THRESHOLD, threshold=20)

        assert result.trimmedLength == 0
        assert result.trimmedSequence == ""

    def test_all_high_quality_returns_full(self):
        analyzer = TrimmingAnalyzer()
        seq = Sequence(
            id="test",
            sequence="ATGCATGC",
            quality=[40, 40, 40, 40, 40, 40, 40, 40],
        )

        result = analyzer.analyze(seq, algorithm=TrimmingAlgorithm.QUALITY_THRESHOLD, threshold=20)

        assert result.trimmedLength == 8
        assert result.trimmedSequence == "ATGCATGC"
        assert result.trimStart == 0
        assert result.trimEnd == 8

    def test_trimmed_quality_matches_sequence(self):
        analyzer = TrimmingAnalyzer()
        seq = Sequence(
            id="test",
            sequence="NNATGCNN",
            quality=[5, 5, 30, 30, 30, 30, 5, 5],
        )

        result = analyzer.analyze(seq, algorithm=TrimmingAlgorithm.QUALITY_THRESHOLD, threshold=20)

        assert len(result.trimmedSequence) == len(result.trimmedQuality)
        assert result.trimmedQuality == [30, 30, 30, 30]

    def test_missing_quality_raises_error(self):
        analyzer = TrimmingAnalyzer()
        seq = Sequence(id="test", sequence="ATGCATGC")

        with pytest.raises(ValueError, match="no quality scores"):
            analyzer.analyze(seq, algorithm=TrimmingAlgorithm.QUALITY_THRESHOLD)

    def test_default_algorithm_is_modified_mott(self):
        analyzer = TrimmingAnalyzer()
        seq = Sequence(
            id="test",
            sequence="ATGCATGC",
            quality=[40, 40, 40, 40, 40, 40, 40, 40],
        )

        result = analyzer.analyze(seq)

        assert result.algorithm == "modified_mott"

    def test_sliding_window_short_sequence(self):
        analyzer = TrimmingAnalyzer()
        seq = Sequence(
            id="test",
            sequence="ATGC",
            quality=[30, 30, 30, 30],
        )

        result = analyzer.analyze(
            seq,
            algorithm=TrimmingAlgorithm.SLIDING_WINDOW,
            window_size=10,
            threshold=20,
        )

        # Sequence shorter than window, should check overall average
        assert result.trimmedLength == 4
