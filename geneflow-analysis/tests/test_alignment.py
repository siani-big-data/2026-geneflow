"""Tests for alignment module."""

import pytest

from src.alignment import (
    ConsensusBuilder,
    MultipleAligner,
    PairwiseAligner,
    VariantDetector,
)
from src.alignment.consensus import ConsensusMethod
from src.alignment.variants import VariantType
from src.models import AlignmentType


class TestPairwiseAligner:
    """Tests for PairwiseAligner."""

    def test_align_identical_sequences(self):
        """Test alignment of identical sequences."""
        aligner = PairwiseAligner()
        result = aligner.align(["ACGT", "ACGT"])

        assert result.type == AlignmentType.PAIRWISE
        assert result.identity == 100.0
        assert result.gaps == 0
        assert len(result.alignedSequences) == 2

    def test_align_different_sequences(self):
        """Test alignment of different sequences."""
        aligner = PairwiseAligner()
        result = aligner.align(["ACGT", "ACTT"])

        assert result.type == AlignmentType.PAIRWISE
        assert len(result.alignedSequences) == 2
        assert result.identity < 100.0

    def test_align_with_gaps(self):
        """Test alignment that requires gaps."""
        aligner = PairwiseAligner()
        result = aligner.align(["ACGTACGT", "ACGACGT"])

        assert result.type == AlignmentType.PAIRWISE
        # Should have gaps due to length difference
        aligned1, aligned2 = result.alignedSequences
        assert len(aligned1) == len(aligned2)

    def test_align_validates_min_sequences(self):
        """Test that alignment requires minimum 2 sequences."""
        aligner = PairwiseAligner()

        with pytest.raises(ValueError, match="at least 2"):
            aligner.align(["ACGT"])

    def test_align_validates_empty_sequences(self):
        """Test that empty sequences are rejected."""
        aligner = PairwiseAligner()

        with pytest.raises(ValueError, match="Empty sequence"):
            aligner.align(["ACGT", ""])

    def test_align_local(self):
        """Test local alignment."""
        aligner = PairwiseAligner()
        result = aligner.align_local(["XXXACGTXXX", "ACGT"])

        assert len(result.alignedSequences) == 2
        # Local alignment should find the matching region

    def test_align_case_insensitive(self):
        """Test that alignment is case insensitive."""
        aligner = PairwiseAligner()
        result1 = aligner.align(["ACGT", "ACGT"])
        result2 = aligner.align(["acgt", "ACGT"])

        assert result1.identity == result2.identity

    def test_align_custom_scoring(self):
        """Test alignment with custom scoring."""
        aligner = PairwiseAligner()
        result = aligner.align(
            ["ACGT", "ACTT"],
            match_score=3,
            mismatch_score=-2,
        )

        assert result.type == AlignmentType.PAIRWISE


class TestMultipleAligner:
    """Tests for MultipleAligner."""

    def test_align_three_sequences(self):
        """Test multiple alignment with 3 sequences."""
        aligner = MultipleAligner()
        result = aligner.align(["ACGT", "ACGT", "ACGT"])

        assert result.type == AlignmentType.MULTIPLE
        assert len(result.alignedSequences) == 3
        assert result.identity == 100.0

    def test_align_different_sequences(self):
        """Test multiple alignment with different sequences."""
        aligner = MultipleAligner()
        result = aligner.align(["ACGT", "ACTT", "AGGT"])

        assert result.type == AlignmentType.MULTIPLE
        assert len(result.alignedSequences) == 3
        # All aligned sequences should have same length
        lengths = [len(s) for s in result.alignedSequences]
        assert len(set(lengths)) == 1

    def test_align_two_sequences_delegates(self):
        """Test that 2 sequences delegates to pairwise."""
        aligner = MultipleAligner()
        result = aligner.align(["ACGT", "ACGT"])

        # Should still work, delegates to pairwise
        assert len(result.alignedSequences) == 2

    def test_align_validates_sequences(self):
        """Test validation of input sequences."""
        aligner = MultipleAligner()

        with pytest.raises(ValueError, match="at least 2"):
            aligner.align(["ACGT"])

    def test_align_with_gaps_needed(self):
        """Test alignment requiring gap insertion."""
        aligner = MultipleAligner()
        result = aligner.align(
            [
                "ACGTACGT",
                "ACGACGT",
                "ACGTCGT",
            ]
        )

        assert len(result.alignedSequences) == 3
        # Check all have same length
        assert len(set(len(s) for s in result.alignedSequences)) == 1


class TestConsensusBuilder:
    """Tests for ConsensusBuilder."""

    def test_consensus_identical(self):
        """Test consensus of identical sequences."""
        builder = ConsensusBuilder()
        result = builder.build(["ACGT", "ACGT", "ACGT"])

        assert result.consensus == "ACGT"
        assert all(q == 1.0 for q in result.quality)
        assert all(c == 3 for c in result.coverage)

    def test_consensus_majority(self):
        """Test majority vote consensus."""
        builder = ConsensusBuilder()
        result = builder.build(
            ["ACGT", "ACGT", "TCGT"],
            method=ConsensusMethod.MAJORITY,
        )

        # First position: 2 A, 1 T -> A wins
        assert result.consensus[0] == "A"
        assert result.quality[0] == pytest.approx(0.667, rel=0.01)

    def test_consensus_threshold(self):
        """Test threshold-based consensus."""
        builder = ConsensusBuilder()
        result = builder.build(
            ["ACGT", "TCGT", "GCGT"],
            method=ConsensusMethod.THRESHOLD,
            threshold=0.5,
        )

        # First position: A=1, T=1, G=1 -> no majority >= 50%, should be N
        assert result.consensus[0] == "N"

    def test_consensus_iupac(self):
        """Test IUPAC ambiguity code consensus."""
        builder = ConsensusBuilder()
        result = builder.build(
            ["ACGT", "GCGT"],
            method=ConsensusMethod.IUPAC,
        )

        # First position: A and G -> R (purine)
        assert result.consensus[0] == "R"

    def test_consensus_empty(self):
        """Test consensus of empty list."""
        builder = ConsensusBuilder()
        result = builder.build([])

        assert result.consensus == ""
        assert result.quality == []

    def test_consensus_with_gaps(self):
        """Test consensus handles gaps."""
        builder = ConsensusBuilder()
        result = builder.build(["A-GT", "ACGT", "ACGT"])

        # Position 1: 2 C, 1 gap -> C should win
        assert result.consensus[1] == "C"

    def test_build_profile(self):
        """Test PSSM profile building."""
        builder = ConsensusBuilder()
        profile = builder.build_profile(["ACGT", "ACGT", "TCGT"])

        assert len(profile) == 4
        # Position 0: A=2/3, T=1/3
        assert profile[0]["A"] == pytest.approx(0.667, rel=0.01)
        assert profile[0]["T"] == pytest.approx(0.333, rel=0.01)

    def test_calculate_conservation(self):
        """Test conservation score calculation."""
        builder = ConsensusBuilder()

        # Fully conserved
        scores = builder.calculate_conservation(["AAAA", "AAAA", "AAAA"])
        assert all(s == 1.0 for s in scores)

        # Mixed
        scores = builder.calculate_conservation(["ACGT", "ACGT", "TGCA"])
        # Some positions will be less conserved
        assert 0 <= min(scores) <= 1.0


class TestVariantDetector:
    """Tests for VariantDetector."""

    def test_detect_no_variants(self):
        """Test detection with identical sequences."""
        detector = VariantDetector()
        report = detector.detect(["ACGT", "ACGT", "ACGT"])

        assert len(report.variants) == 0
        assert report.variantPositions == 0

    def test_detect_snp(self):
        """Test SNP detection."""
        detector = VariantDetector()
        # Reference is first sequence
        report = detector.detect(["ACGT", "TCGT", "ACGT"])

        assert report.snpCount >= 1
        # Find SNP at position 0
        snps = [v for v in report.variants if v.position == 0]
        assert len(snps) == 1
        assert snps[0].type == VariantType.SNP
        assert snps[0].reference == "A"
        assert snps[0].alternate == "T"

    def test_detect_deletion(self):
        """Test deletion detection."""
        detector = VariantDetector()
        report = detector.detect(["ACGT", "A-GT", "ACGT"])

        deletions = [v for v in report.variants if v.type == VariantType.DELETION]
        assert len(deletions) >= 1

    def test_detect_insertion(self):
        """Test insertion detection."""
        detector = VariantDetector()
        report = detector.detect(["A-GT", "ACGT", "A-GT"])

        insertions = [v for v in report.variants if v.type == VariantType.INSERTION]
        assert len(insertions) >= 1

    def test_detect_with_min_frequency(self):
        """Test filtering by minimum frequency."""
        detector = VariantDetector()

        # 1 out of 3 sequences has variant = 50% frequency
        report = detector.detect(
            ["ACGT", "TCGT", "ACGT", "ACGT"],
            min_frequency=0.6,
        )

        # Variant at 33% should be filtered
        assert report.snpCount == 0

    def test_detect_vs_consensus(self):
        """Test variant detection vs consensus."""
        detector = VariantDetector()
        report = detector.detect_vs_consensus(
            ["ACGT", "TCGT", "ACGT"],
            consensus="ACGT",
        )

        # One sequence differs at position 0
        assert report.snpCount >= 1

    def test_summarize_variants(self):
        """Test variant summary statistics."""
        detector = VariantDetector()
        report = detector.detect(["ACGT", "TCGT", "GCGT"])

        summary = detector.summarize_variants(report)

        assert "totalVariants" in summary
        assert "variantRate" in summary
        assert "transitions" in summary
        assert "transversions" in summary

    def test_ti_tv_ratio(self):
        """Test transition/transversion ratio calculation."""
        detector = VariantDetector()

        # A->G is transition, A->C is transversion
        report = detector.detect(
            [
                "AAAA",  # Reference
                "GAAA",  # A->G transition
                "CAAA",  # A->C transversion
            ]
        )

        summary = detector.summarize_variants(report)
        # Should have both types
        assert summary["transitions"] + summary["transversions"] > 0

    def test_detect_empty_sequences(self):
        """Test with empty input."""
        detector = VariantDetector()
        report = detector.detect([])

        assert len(report.variants) == 0

    def test_detect_single_sequence(self):
        """Test with single sequence (no variants possible)."""
        detector = VariantDetector()
        report = detector.detect(["ACGT"])

        assert len(report.variants) == 0


class TestAlignmentIntegration:
    """Integration tests for alignment workflow."""

    def test_full_workflow(self):
        """Test complete alignment -> consensus -> variants workflow."""
        # Align sequences
        aligner = MultipleAligner()
        alignment = aligner.align(
            [
                "ACGTACGT",
                "ACGTAGGT",
                "ACGTACGT",
            ]
        )

        # Build consensus
        builder = ConsensusBuilder()
        consensus = builder.build(alignment.alignedSequences)

        assert len(consensus.consensus) == len(alignment.alignedSequences[0])

        # Detect variants
        detector = VariantDetector()
        report = detector.detect(alignment.alignedSequences)

        # Should find variant at position where sequences differ
        assert report.totalPositions > 0

    def test_conservation_identifies_variable_regions(self):
        """Test that conservation scores identify variable regions."""
        builder = ConsensusBuilder()

        sequences = [
            "AAAACGTAAAA",
            "AAAATGTAAAA",
            "AAAAGGTAAAA",
        ]

        conservation = builder.calculate_conservation(sequences)

        # Position 4 (C/T/G) should have lower conservation
        assert conservation[4] < conservation[0]
        # Conserved positions should be 1.0
        assert conservation[0] == 1.0
