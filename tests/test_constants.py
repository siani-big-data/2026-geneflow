"""Tests for biological constants and utilities."""

from src.constants import (
    AMINO_ACIDS,
    CODON_TABLE,
    COMPLEMENT,
    IUPAC_CODES,
    RESTRICTION_ENZYMES,
    START_CODONS,
    STOP_CODONS,
    complement,
    gc_content,
    get_iupac_code,
    reverse_complement,
    translate,
)


class TestIUPACCodes:
    """Tests for IUPAC codes."""

    def test_standard_bases(self):
        assert IUPAC_CODES["A"] == {"A"}
        assert IUPAC_CODES["C"] == {"C"}
        assert IUPAC_CODES["G"] == {"G"}
        assert IUPAC_CODES["T"] == {"T"}

    def test_ambiguity_codes(self):
        assert IUPAC_CODES["R"] == {"A", "G"}  # purine
        assert IUPAC_CODES["Y"] == {"C", "T"}  # pyrimidine
        assert IUPAC_CODES["N"] == {"A", "C", "G", "T"}  # any

    def test_get_iupac_code(self):
        assert get_iupac_code({"A", "G"}) == "R"
        assert get_iupac_code({"C", "T"}) == "Y"
        assert get_iupac_code({"A", "C", "G", "T"}) == "N"


class TestComplement:
    """Tests for complement operations."""

    def test_complement_map(self):
        assert COMPLEMENT["A"] == "T"
        assert COMPLEMENT["T"] == "A"
        assert COMPLEMENT["C"] == "G"
        assert COMPLEMENT["G"] == "C"

    def test_complement_function(self):
        assert complement("ATCG") == "TAGC"
        assert complement("AAAA") == "TTTT"
        assert complement("GCGC") == "CGCG"

    def test_reverse_complement(self):
        assert reverse_complement("ATCG") == "CGAT"
        assert reverse_complement("GAATTC") == "GAATTC"  # EcoRI palindrome
        assert reverse_complement("AAAAAA") == "TTTTTT"

    def test_complement_with_iupac(self):
        assert complement("R") == "Y"  # A|G -> T|C
        assert complement("Y") == "R"
        assert complement("N") == "N"


class TestCodonTable:
    """Tests for codon table."""

    def test_start_codon(self):
        assert CODON_TABLE["ATG"] == "M"
        assert "ATG" in START_CODONS

    def test_stop_codons(self):
        assert CODON_TABLE["TAA"] == "*"
        assert CODON_TABLE["TAG"] == "*"
        assert CODON_TABLE["TGA"] == "*"
        assert STOP_CODONS == {"TAA", "TAG", "TGA"}

    def test_all_codons_present(self):
        # 64 possible codons (4^3)
        assert len(CODON_TABLE) == 64

    def test_amino_acid_codons(self):
        # Phenylalanine
        assert CODON_TABLE["TTT"] == "F"
        assert CODON_TABLE["TTC"] == "F"
        # Leucine (6 codons)
        leucine_codons = [c for c, aa in CODON_TABLE.items() if aa == "L"]
        assert len(leucine_codons) == 6


class TestTranslation:
    """Tests for translation function."""

    def test_simple_translation(self):
        # ATG (M) + GCT (A) + TAA (*)
        assert translate("ATGGCTTAA") == "MA*"

    def test_translation_frames(self):
        seq = "AATGCTTAA"
        assert translate(seq, frame=0) == "NA*"  # AAT GCT TAA
        assert translate(seq, frame=1) == "ML"  # ATG CTT

    def test_translation_incomplete_codon(self):
        # Last incomplete codon should be ignored
        assert translate("ATGGCT") == "MA"
        assert translate("ATGGCTA") == "MA"  # trailing A ignored

    def test_unknown_codon(self):
        # If codon contains N, should return X
        assert translate("ATGNNN") == "MX"


class TestAminoAcids:
    """Tests for amino acid information."""

    def test_amino_acid_count(self):
        # 20 standard + stop
        assert len(AMINO_ACIDS) == 21

    def test_amino_acid_info(self):
        three_letter, full_name, prop = AMINO_ACIDS["M"]
        assert three_letter == "Met"
        assert full_name == "Methionine"
        assert prop == "nonpolar"

    def test_stop_codon_info(self):
        assert AMINO_ACIDS["*"][2] == "stop"


class TestGCContent:
    """Tests for GC content calculation."""

    def test_gc_content_50_percent(self):
        assert gc_content("ATGC") == 50.0

    def test_gc_content_100_percent(self):
        assert gc_content("GCGCGC") == 100.0

    def test_gc_content_0_percent(self):
        assert gc_content("ATATAT") == 0.0

    def test_gc_content_empty(self):
        assert gc_content("") == 0.0

    def test_gc_content_case_insensitive(self):
        assert gc_content("atgc") == 50.0
        assert gc_content("AtGc") == 50.0


class TestRestrictionEnzymes:
    """Tests for restriction enzyme data."""

    def test_common_enzymes_present(self):
        assert "EcoRI" in RESTRICTION_ENZYMES
        assert "BamHI" in RESTRICTION_ENZYMES
        assert "HindIII" in RESTRICTION_ENZYMES

    def test_enzyme_data_structure(self):
        seq, cut_pos, overhang = RESTRICTION_ENZYMES["EcoRI"]
        assert seq == "GAATTC"
        assert cut_pos == 1  # G|AATTC
        assert overhang == "5'"

    def test_blunt_cutters(self):
        seq, cut_pos, overhang = RESTRICTION_ENZYMES["SmaI"]
        assert overhang == "blunt"
        assert cut_pos == 3  # CCC|GGG

    def test_8_cutter(self):
        seq, _, _ = RESTRICTION_ENZYMES["NotI"]
        assert len(seq) == 8  # GCGGCCGC
