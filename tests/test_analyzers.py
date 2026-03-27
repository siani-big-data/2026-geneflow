"""Tests for sequence analyzers."""

import pytest
from src.analyzers import QualityAnalyzer, TrimmingAnalyzer
from src.models import Sequence, TrimmingAlgorithm


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
            quality=[30, 30, 30, 10, 10, 10, 10, 10, 30, 30, 30, 30, 10, 10, 10, 10, 10, 10, 30, 30],
        )

        regions = analyzer.find_low_quality_regions(seq, threshold=20, min_length=5)

        assert len(regions) == 2
        assert regions[0]["start"] == 3
        assert regions[0]["length"] == 5
        assert regions[1]["start"] == 12
        assert regions[1]["length"] == 6


class TestTrimmingAnalyzer:
    """Tests for TrimmingAnalyzer."""

    def test_trim_quality_threshold(self):
        analyzer = TrimmingAnalyzer()
        seq = Sequence(
            id="test",
            sequence="NNNATGCATGCNN",
            quality=[5, 5, 5, 30, 30, 30, 30, 30, 30, 30, 5, 5, 5],
        )

        result = analyzer.analyze(
            seq, algorithm=TrimmingAlgorithm.QUALITY_THRESHOLD, threshold=20
        )

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

        result = analyzer.analyze(
            seq, algorithm=TrimmingAlgorithm.MODIFIED_MOTT, cutoff=0.05
        )

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

        result = analyzer.analyze(
            seq, algorithm=TrimmingAlgorithm.QUALITY_THRESHOLD, threshold=20
        )

        assert result.trimmedLength == 0
        assert result.trimmedSequence == ""

    def test_all_high_quality_returns_full(self):
        analyzer = TrimmingAnalyzer()
        seq = Sequence(
            id="test",
            sequence="ATGCATGC",
            quality=[40, 40, 40, 40, 40, 40, 40, 40],
        )

        result = analyzer.analyze(
            seq, algorithm=TrimmingAlgorithm.QUALITY_THRESHOLD, threshold=20
        )

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

        result = analyzer.analyze(
            seq, algorithm=TrimmingAlgorithm.QUALITY_THRESHOLD, threshold=20
        )

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
