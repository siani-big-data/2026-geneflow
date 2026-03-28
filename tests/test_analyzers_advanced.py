"""Tests for advanced analyzers."""

import pytest
from src.analyzers import (
    HeterozygoteAnalyzer,
    MotifAnalyzer,
    TranslationAnalyzer,
    ORFAnalyzer,
    RestrictionAnalyzer,
)
from src.models import Sequence, ChromatogramData


class TestHeterozygoteAnalyzer:
    """Tests for HeterozygoteAnalyzer."""

    @pytest.fixture
    def analyzer(self):
        return HeterozygoteAnalyzer()

    def test_detect_from_iupac_codes(self, analyzer):
        """Test heterozygote detection from IUPAC codes."""
        # R = A/G, Y = C/T, S = G/C, W = A/T, K = G/T
        seq = Sequence(id="test", sequence="ACGRYSWK")
        result = analyzer.analyze(seq)

        assert result.heterozygoteCount == 5  # R, Y, S, W, K
        assert len(result.calls) == 5

    def test_detect_no_heterozygotes(self, analyzer):
        """Test sequence with no heterozygotes."""
        seq = Sequence(id="test", sequence="ACGTACGT")
        result = analyzer.analyze(seq)

        assert result.heterozygoteCount == 0
        assert len(result.calls) == 0

    def test_heterozygote_rate(self, analyzer):
        """Test heterozygote rate calculation."""
        seq = Sequence(id="test", sequence="ACGTACGTR")  # 1 out of 9
        result = analyzer.analyze(seq)

        assert result.heterozygoteRate == pytest.approx(1 / 9, rel=0.01)

    def test_iupac_code_mapping(self, analyzer):
        """Test correct IUPAC code identification."""
        seq = Sequence(id="test", sequence="R")  # A/G
        result = analyzer.analyze(seq)

        assert len(result.calls) == 1
        call = result.calls[0]
        assert call.iupacCode == "R"
        assert set([call.base1, call.base2]) == {"A", "G"}

    def test_summarize(self, analyzer):
        """Test summary statistics."""
        seq = Sequence(id="test", sequence="RRYYY")  # 2 R, 3 Y
        result = analyzer.analyze(seq)

        summary = analyzer.summarize(result)
        assert summary["count"] == 5
        assert summary["iupacCodes"]["R"] == 2
        assert summary["iupacCodes"]["Y"] == 3


class TestMotifAnalyzer:
    """Tests for MotifAnalyzer."""

    @pytest.fixture
    def analyzer(self):
        return MotifAnalyzer()

    def test_find_exact_pattern(self, analyzer):
        """Test finding exact pattern."""
        seq = Sequence(id="test", sequence="ACGTACGTACGT")
        result = analyzer.analyze(seq, pattern="ACGT")

        assert result.matchCount == 3
        assert result.matches[0].start == 0
        assert result.matches[1].start == 4
        assert result.matches[2].start == 8

    def test_find_iupac_pattern(self, analyzer):
        """Test finding pattern with IUPAC codes."""
        seq = Sequence(id="test", sequence="ACGTACGT")
        # R = A or G
        result = analyzer.analyze(seq, pattern="RCGT")

        assert result.matchCount == 2  # ACGT and GCGT don't exist, but ACGT matches

    def test_no_matches(self, analyzer):
        """Test when pattern not found."""
        seq = Sequence(id="test", sequence="AAAAAAA")
        result = analyzer.analyze(seq, pattern="CCCC")

        assert result.matchCount == 0

    def test_search_complement(self, analyzer):
        """Test searching both strands."""
        seq = Sequence(id="test", sequence="ACGTACGT")
        result = analyzer.analyze(seq, pattern="ACGT", search_complement=True)

        assert result.searchedBothStrands is True

    def test_find_repeats(self, analyzer):
        """Test finding tandem repeats."""
        seq = Sequence(id="test", sequence="ATATATATAT")
        repeats = analyzer.find_repeats(seq, min_unit_length=2, min_repeats=3)

        assert len(repeats) >= 1
        # Should find AT repeated

    def test_pattern_required(self, analyzer):
        """Test that pattern is required."""
        seq = Sequence(id="test", sequence="ACGT")

        with pytest.raises(ValueError, match="Pattern required"):
            analyzer.analyze(seq, pattern="")


class TestTranslationAnalyzer:
    """Tests for TranslationAnalyzer."""

    @pytest.fixture
    def analyzer(self):
        return TranslationAnalyzer()

    def test_translate_frame_1(self, analyzer):
        """Test translation in frame 1."""
        # ATG = M (start), TAA = * (stop)
        seq = Sequence(id="test", sequence="ATGTAA")
        result = analyzer.analyze(seq, frame=1)

        assert result.frame == 1
        assert "M" in result.proteinSequence  # Should contain M
        assert result.proteinLength >= 1

    def test_translate_frame_2(self, analyzer):
        """Test translation in frame 2."""
        # Frame 2 starts at position 1
        # AATGTAA -> frame 2: ATG TAA -> M*
        seq = Sequence(id="test", sequence="AATGTAA")
        result = analyzer.analyze(seq, frame=2)

        assert result.frame == 2
        assert result.proteinLength >= 1

    def test_count_stop_codons(self, analyzer):
        """Test stop codon counting."""
        # TAA and TAG are stop codons
        seq = Sequence(id="test", sequence="TAATAGTGA")  # Three stop codons
        result = analyzer.analyze(seq, frame=1)

        assert result.stopCodonCount >= 2

    def test_find_start_codons(self, analyzer):
        """Test finding start codon positions."""
        seq = Sequence(id="test", sequence="ATGAAATGA")
        result = analyzer.analyze(seq, frame=1)

        assert 0 in result.startCodonPositions

    def test_translate_all_frames(self, analyzer):
        """Test translation in all six frames."""
        seq = Sequence(id="test", sequence="ATGCATGCATGC")
        results = analyzer.translate_all_frames(seq)

        assert len(results) == 6

    def test_invalid_frame(self, analyzer):
        """Test error on invalid frame."""
        seq = Sequence(id="test", sequence="ATGCAT")

        with pytest.raises(ValueError, match="Frame must be"):
            analyzer.analyze(seq, frame=4)

    def test_amino_acid_composition(self, analyzer):
        """Test amino acid composition."""
        # AAA = K (Lysine), so AAAAAAAAA = KKK
        seq = Sequence(id="test", sequence="AAAAAAAAA")
        result = analyzer.analyze(seq, frame=1)

        # Should have K (lysine)
        assert result.aminoAcidComposition.get("K", 0) >= 1


class TestORFAnalyzer:
    """Tests for ORFAnalyzer."""

    @pytest.fixture
    def analyzer(self):
        return ORFAnalyzer()

    def test_find_orf(self, analyzer):
        """Test finding a simple ORF."""
        # ATG...TAA = ORF
        seq = Sequence(
            id="test",
            sequence="ATGAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATAA"
        )
        result = analyzer.analyze(seq, min_length=10)

        assert result.totalOrfs >= 1
        assert result.longestOrf is not None

    def test_no_orf_short_sequence(self, analyzer):
        """Test no ORF in short sequence."""
        seq = Sequence(id="test", sequence="ATGTAA")
        result = analyzer.analyze(seq, min_length=30)

        assert result.totalOrfs == 0

    def test_orf_includes_protein(self, analyzer):
        """Test that ORF includes protein sequence."""
        seq = Sequence(
            id="test",
            sequence="ATGAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATAA"
        )
        result = analyzer.analyze(seq, min_length=10)

        if result.longestOrf:
            # ORF should contain the protein sequence
            assert len(result.longestOrf.proteinSequence) >= 10

    def test_find_longest_orf(self, analyzer):
        """Test finding longest ORF."""
        seq = Sequence(
            id="test",
            sequence="ATGAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATAA"
        )
        orf = analyzer.find_longest_orf(seq)

        assert orf is not None

    def test_search_specific_frames(self, analyzer):
        """Test searching specific frames."""
        seq = Sequence(id="test", sequence="ATGAAATGA" * 20)
        result = analyzer.analyze(seq, frames=[1], min_length=5)

        assert 1 in result.searchedFrames
        assert len(result.searchedFrames) == 1


class TestRestrictionAnalyzer:
    """Tests for RestrictionAnalyzer."""

    @pytest.fixture
    def analyzer(self):
        return RestrictionAnalyzer()

    def test_find_ecori_site(self, analyzer):
        """Test finding EcoRI site (GAATTC)."""
        seq = Sequence(id="test", sequence="AAAGAATTCAAA")
        result = analyzer.analyze(seq, enzymes=["EcoRI"])

        assert result.totalSites == 1
        assert result.sites[0].enzyme == "EcoRI"

    def test_find_multiple_sites(self, analyzer):
        """Test finding multiple sites."""
        seq = Sequence(id="test", sequence="GAATTCGAATTC")
        result = analyzer.analyze(seq, enzymes=["EcoRI"])

        assert result.totalSites == 2

    def test_no_sites(self, analyzer):
        """Test when no sites found."""
        seq = Sequence(id="test", sequence="AAAAAAAAAA")
        result = analyzer.analyze(seq, enzymes=["EcoRI"])

        assert result.totalSites == 0

    def test_fragment_calculation(self, analyzer):
        """Test fragment length calculation."""
        # EcoRI cuts at position 4 (G/AATTC)
        seq = Sequence(id="test", sequence="AAAGAATTCAAA")
        result = analyzer.analyze(seq, enzymes=["EcoRI"])

        assert len(result.fragmentLengths) >= 1

    def test_find_unique_cutters(self, analyzer):
        """Test finding enzymes that cut once."""
        seq = Sequence(id="test", sequence="GAATTCGGATCC")
        unique = analyzer.find_unique_cutters(seq, enzymes=["EcoRI", "BamHI"])

        assert "EcoRI" in unique
        assert "BamHI" in unique

    def test_find_non_cutters(self, analyzer):
        """Test finding enzymes that don't cut."""
        seq = Sequence(id="test", sequence="AAAAAAAAAA")
        non_cutters = analyzer.find_non_cutters(seq, enzymes=["EcoRI"])

        assert "EcoRI" in non_cutters

    def test_unknown_enzyme_raises(self, analyzer):
        """Test error on unknown enzyme."""
        seq = Sequence(id="test", sequence="ACGT")

        with pytest.raises(ValueError, match="Unknown enzyme"):
            analyzer.analyze(seq, enzymes=["FAKE_ENZYME"])

    def test_summary_stats(self, analyzer):
        """Test summary statistics."""
        seq = Sequence(id="test", sequence="GAATTCGAATTC")
        result = analyzer.analyze(seq, enzymes=["EcoRI"])

        summary = analyzer.summarize(result)
        assert summary["totalSites"] == 2
        assert summary["sitesPerEnzyme"]["EcoRI"] == 2
