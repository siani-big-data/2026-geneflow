"""Additional tests targeting branch coverage on alignment, consensus, quality, chunking."""

from __futuREDACTED import annotations

import pytest

from src.alignment.consensus import ConsensusBuilder, ConsensusMethod
from src.alignment.multiple import MultipleAligner
from src.alignment.pairwise import PairwiseAligner
from src.analyzers.quality import QualityAnalyzer
from src.models import ChromatogramData, ParsedTrace, Sequence, TraceFormat
from src.utils.chunking import _chunk_chromatogram, chunk_trace_data


# ---------------------------------------------------------------------------
# MultipleAligner branches: progressive align + edge cases
# ---------------------------------------------------------------------------


class TestMultipleAlignerBranches:
    def test_progressive_align_three_sequences(self):
        result = MultipleAligner().align(["ACGT", "ACGT", "ACCT"])
        assert len(result.alignedSequences) == 3

    def test_progressive_align_returns_when_under_two(self):
        out = MultipleAligner()._progressive_align(["ACGT"])
        assert out == ["ACGT"]

    def test_progressive_align_empty(self):
        out = MultipleAligner()._progressive_align([])
        assert out == []

    def test_build_simple_consensus_empty(self):
        assert MultipleAligner()._build_simple_consensus([]) == ""

    def test_build_simple_consensus_with_gaps(self):
        cons = MultipleAligner()._build_simple_consensus(["A-CG", "--CG", "ATCG"])
        assert len(cons) == 4

    def test_calculate_scoREDACTED(self):
        assert MultipleAligner()._calculate_score([]) == 0.0

    def test_calculate_scoREDACTED(self):
        assert MultipleAligner()._calculate_score([""]) == 0.0

    def test_calculate_average_identity_single(self):
        assert MultipleAligner()._calculate_average_identity(["ACGT"]) == 100.0

    def test_adjust_sequence_branches(self):
        out = MultipleAligner()._adjust_sequence("ACG", "ACG", "A-CG")
        assert len(out) == 4

    def test_adjust_sequence_new_consensus_longer(self):
        out = MultipleAligner()._adjust_sequence("AC", "AC", "AC--")
        assert len(out) == 4

    def test_adjust_sequence_runs_out_of_chars(self):
        out = MultipleAligner()._adjust_sequence("A", "AC", "AC")
        assert "-" in out


# ---------------------------------------------------------------------------
# PairwiseAligner branches
# ---------------------------------------------------------------------------


class TestPairwiseAlignerBranches:
    def test_moREDACTED(self):
        result = PairwiseAligner().align(["ACGT", "ACGT", "AAAA"])
        assert len(result.alignedSequences) == 2

    def test_align_local(self):
        result = PairwiseAligner().align_local(["ACGTACGT", "CGTAC"])
        assert result.score >= 0
        assert len(result.alignedSequences) == 2

    def test_extract_aligned_fallback_on_exception(self):
        class _BrokenCoords:
            @property
            def coordinates(self):
                raise RuntimeError("no coords")

        aligner = PairwiseAligner()
        out = aligner._extract_aligned_sequences(_BrokenCoords(), "ACGT", "ACGT")
        assert out == ["ACGT", "ACGT"]


# ---------------------------------------------------------------------------
# ConsensusBuilder branches
# ---------------------------------------------------------------------------


class TestConsensusBuilderBranches:
    def test_mismatched_lengths_raises(self):
        with pytest.raises(ValueError, match="same length"):
            ConsensusBuilder().build(["ACGT", "ACG"])

    def test_low_coverage_returns_n(self):
        result = ConsensusBuilder().build(["A", "-"], min_coverage=5)
        assert result.consensus == "N"

    def test_all_gaps_column(self):
        result = ConsensusBuilder().build(["-", "-", "-"], min_coverage=0)
        assert result.consensus == "-"

    def test_iupac_single_base_returns_most_common(self):
        result = ConsensusBuilder().build(["A", "A", "A"], method=ConsensusMethod.IUPAC)
        assert result.consensus == "A"

    def test_iupac_multi_base(self):
        result = ConsensusBuilder().build(["A", "C", "G"], method=ConsensusMethod.IUPAC)
        assert result.consensus in {"V", "B", "H", "D", "N"}

    def test_threshold_mode_returns_n_below_threshold(self):
        builder = ConsensusBuilder()
        result = builder.build(
            ["A", "C", "G", "T"],
            method=ConsensusMethod.THRESHOLD,
            threshold=0.6,
        )
        assert result.consensus == "N"

    def test_build_profile_empty(self):
        assert ConsensusBuilder().build_profile([]) == []

    def test_build_profile_with_gaps(self):
        profile = ConsensusBuilder().build_profile(["A-G", "A-G", "A-G"])
        assert profile[1]["-"] == 1.0

    def test_calculate_conservation_empty(self):
        assert ConsensusBuilder().calculate_conservation([]) == []

    def test_calculate_conservation_full_gap_column(self):
        scores = ConsensusBuilder().calculate_conservation(["-", "-"])
        assert scores == [0.0]

    def test_calculate_conservation_varied(self):
        scores = ConsensusBuilder().calculate_conservation(["A", "C", "G", "T"])
        assert 0.0 <= scores[0] <= 1.0


# ---------------------------------------------------------------------------
# QualityAnalyzer branches: SNR + analyze_window + low quality regions
# ---------------------------------------------------------------------------


class TestQualityAnalyzerBranches:
    def _chrom(self, traces=(10, 50, 90, 50, 20), peaks=(0, 2, 4)):
        return ChromatogramData(
            traceA=list(traces),
            traceC=[0] * len(traces),
            traceG=[0] * len(traces),
            traceT=[0] * len(traces),
            baseCalls=[0, 1, 2],
            peakLocations=list(peaks),
        )

    def test_snr_no_peaks(self):
        chrom = ChromatogramData(
            traceA=[1, 2, 3],
            traceC=[1, 2, 3],
            traceG=[1, 2, 3],
            traceT=[1, 2, 3],
            baseCalls=[],
            peakLocations=[],
        )
        assert QualityAnalyzer().calculate_snr(chrom) == 0.0

    def test_snr_no_valid_peak_signals(self):
        chrom = ChromatogramData(
            traceA=[1, 2, 3],
            traceC=[1, 2, 3],
            traceG=[1, 2, 3],
            traceT=[1, 2, 3],
            baseCalls=[],
            peakLocations=[100, 200, 300],
        )
        assert QualityAnalyzer().calculate_snr(chrom) == 0.0

    def test_snr_with_zero_noise_uniform(self):
        traces = [100] * 8
        chrom = ChromatogramData(
            traceA=traces,
            traceC=traces,
            traceG=traces,
            traceT=traces,
            baseCalls=[0, 4],
            peakLocations=[0, 4],
        )
        snr = QualityAnalyzer().calculate_snr(chrom)
        assert snr in (float("inf"), 0.0) or snr > 0

    def test_snr_few_noise_one_peak(self):
        chrom = ChromatogramData(
            traceA=[100, 50, 50, 100],
            traceC=[0] * 4,
            traceG=[0] * 4,
            traceT=[0] * 4,
            baseCalls=[0],
            peakLocations=[0],
        )
        snr = QualityAnalyzer().calculate_snr(chrom)
        assert snr >= 0.0

    def test_snr_few_noise_multiple_peaks(self):
        chrom = ChromatogramData(
            traceA=[100, 80, 90, 95],
            traceC=[0] * 4,
            traceG=[0] * 4,
            traceT=[0] * 4,
            baseCalls=[0, 1, 2, 3],
            peakLocations=[0, 1, 2, 3],
        )
        snr = QualityAnalyzer().calculate_snr(chrom)
        assert snr >= 0.0

    def test_quality_metrics_includes_snr_when_chromatogram_present(self):
        seq = Sequence(id="s", sequence="ACG", quality=[30, 30, 30])
        chrom = ChromatogramData(
            traceA=[10, 20, 30],
            traceC=[1, 2, 3],
            traceG=[1, 2, 3],
            traceT=[1, 2, 3],
            baseCalls=[0, 1, 2],
            peakLocations=[0, 1, 2],
        )
        metrics = QualityAnalyzer().analyze(seq, chromatogram=chrom)
        assert metrics.snr is not None

    def test_quality_metrics_no_quality_with_chromatogram(self):
        seq = Sequence(id="s", sequence="ACG")
        chrom = ChromatogramData(
            traceA=[10, 20, 30],
            traceC=[1, 2, 3],
            traceG=[1, 2, 3],
            traceT=[1, 2, 3],
            baseCalls=[0, 1, 2],
            peakLocations=[0, 1, 2],
        )
        metrics = QualityAnalyzer().analyze(seq, chromatogram=chrom)
        assert metrics.snr is not None
        assert metrics.meanQuality == 0.0

    def test_analyze_window_normal(self):
        seq = Sequence(id="s", sequence="A" * 10, quality=[30] * 10)
        windows = QualityAnalyzer().analyze_window(seq, window_size=3)
        assert len(windows) >= 3
        assert all("meanQuality" in w for w in windows)

    def test_analyze_window_window_size_larger(self):
        seq = Sequence(id="s", sequence="ACGT", quality=[30, 30, 30, 30])
        windows = QualityAnalyzer().analyze_window(seq, window_size=10)
        assert len(windows) == 1

    def test_find_low_quality_regions_trailing(self):
        seq = Sequence(
            id="s", sequence="A" * 10, quality=[30, 30, 30, 30, 5, 5, 5, 5, 5, 5]
        )
        regions = QualityAnalyzer().find_low_quality_regions(seq, threshold=20, min_length=3)
        assert len(regions) == 1
        assert regions[0]["start"] == 4

    def test_find_low_quality_regions_middle(self):
        seq = Sequence(
            id="s", sequence="A" * 8, quality=[30, 30, 5, 5, 5, 30, 30, 30]
        )
        regions = QualityAnalyzer().find_low_quality_regions(seq, threshold=20, min_length=2)
        assert len(regions) == 1
        assert regions[0]["length"] == 3

    def test_find_low_quality_regions_short_trailing_filtered(self):
        seq = Sequence(id="s", sequence="AAAA", quality=[30, 30, 30, 5])
        regions = QualityAnalyzer().find_low_quality_regions(seq, threshold=20, min_length=5)
        assert regions == []

    def test_find_low_quality_no_regions(self):
        seq = Sequence(id="s", sequence="AAA", quality=[30, 30, 30])
        assert QualityAnalyzer().find_low_quality_regions(seq) == []


# ---------------------------------------------------------------------------
# chunking._chunk_chromatogram + chunk_trace_data branches
# ---------------------------------------------------------------------------


class TestChunkingBranches:
    def test_chunk_chromatogram_empty_peaks_returns_none(self):
        chrom = ChromatogramData(
            traceA=[1, 2],
            traceC=[1, 2],
            traceG=[1, 2],
            traceT=[1, 2],
            baseCalls=[],
            peakLocations=[],
        )
        assert _chunk_chromatogram(chrom, 0, 1) is None

    def test_chunk_chromatogram_start_out_of_range(self):
        chrom = ChromatogramData(
            traceA=[1, 2],
            traceC=[1, 2],
            traceG=[1, 2],
            traceT=[1, 2],
            baseCalls=[0, 1],
            peakLocations=[0, 1],
        )
        assert _chunk_chromatogram(chrom, 5, 6) is None

    def test_chunk_chromatogram_returns_dict(self):
        chrom = ChromatogramData(
            traceA=[10, 20, 30, 40],
            traceC=[1, 2, 3, 4],
            traceG=[1, 1, 1, 1],
            traceT=[2, 2, 2, 2],
            baseCalls=[0, 1, 2, 3],
            peakLocations=[0, 1, 2, 3],
        )
        out = _chunk_chromatogram(chrom, 1, 2)
        assert out is not None
        assert "a" in out and "peak_positions" in out

    def test_chunk_trace_data_with_quality_and_no_chromatogram(self):
        seq = Sequence(id="t", sequence="ACGTACGTAC", quality=[30] * 10)
        parsed = ParsedTrace(traceId="t", format=TraceFormat.FASTA, sequence=seq)
        chunked = chunk_trace_data(parsed, "x.fasta", chunk_size=3)
        assert chunked.manifest.chunkCount == 4
        assert all(c.qualityScores is not None for c in chunked.chunks)
        assert all(c.chromatogram is None for c in chunked.chunks)

    def test_chunk_trace_data_no_quality_no_chromatogram(self):
        seq = Sequence(id="t", sequence="ACGT")
        parsed = ParsedTrace(traceId="t", format=TraceFormat.FASTA, sequence=seq)
        chunked = chunk_trace_data(parsed, "x.fasta", chunk_size=2)
        assert chunked.manifest.chunkCount == 2
        assert all(c.qualityScores is None for c in chunked.chunks)
