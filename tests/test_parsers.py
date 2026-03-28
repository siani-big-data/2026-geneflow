"""Tests for trace file parsers."""

import pytest
from src.parsers import (
    BaseParser,
    AB1Parser,
    SCFParser,
    FASTQParser,
    FASTAParser,
)
from src.models import TraceFormat


class TestBaseParser:
    """Tests for BaseParser interface."""

    def test_can_parse_extension(self):
        parser = FASTAParser()
        assert parser.can_parse("sequence.fasta")
        assert parser.can_parse("sequence.fa")
        assert not parser.can_parse("sequence.fastq")


class TestFASTAParser:
    """Tests for FASTA parser."""

    def test_parse_simple_fasta(self):
        data = b">seq1 Test sequence\nATGCGATCGATCGATCG\n"
        parser = FASTAParser()

        result = parser.parse(data, "trace-1")

        assert result.traceId == "trace-1"
        assert result.format == TraceFormat.FASTA
        assert result.sequence.sequence == "ATGCGATCGATCGATCG"
        assert result.sequence.name == "seq1"
        # BioPython includes full header in description
        assert "Test sequence" in (result.sequence.description or "")
        assert result.chromatogram is None
        assert result.sequence.quality is None

    def test_parse_multiline_sequence(self):
        data = b">seq1\nATGCGATC\nGATCGATC\nGATCGATC\n"
        parser = FASTAParser()

        result = parser.parse(data, "trace-1")

        assert result.sequence.sequence == "ATGCGATCGATCGATCGATCGATC"
        assert len(result.sequence) == 24

    def test_parse_lowercase_sequence(self):
        data = b">seq1\natgcgatc\n"
        parser = FASTAParser()

        result = parser.parse(data, "trace-1")

        # Should be uppercase
        assert result.sequence.sequence == "ATGCGATC"

    def test_parse_multiple_sequences(self):
        data = b">seq1\nATGC\n>seq2\nGCTA\n>seq3\nTTTT\n"
        parser = FASTAParser()

        # parse() returns only first sequence
        result = parser.parse(data, "trace-1")
        assert result.sequence.sequence == "ATGC"

        # parse_all() returns all sequences
        all_records = parser.parse_all(data)
        assert len(all_records) == 3
        assert all_records[0]["sequence"] == "ATGC"
        assert all_records[1]["sequence"] == "GCTA"
        assert all_records[2]["sequence"] == "TTTT"

    def test_quality_metrics(self):
        data = b">seq1\nATGCATGC\n"  # 50% GC
        parser = FASTAParser()

        result = parser.parse(data, "trace-1")

        assert result.qualityMetrics is not None
        assert result.qualityMetrics.length == 8
        assert result.qualityMetrics.gcContent == 50.0
        assert result.qualityMetrics.meanQuality == 0.0  # FASTA has no quality

    def test_empty_fasta_raises_error(self):
        parser = FASTAParser()

        with pytest.raises(ValueError):
            parser.parse(b"", "trace-1")

    def test_fasta_extensions(self):
        parser = FASTAParser()
        assert parser.extensions == ["fasta", "fa", "fna", "fas"]
        assert parser.format == TraceFormat.FASTA


class TestFASTQParser:
    """Tests for FASTQ parser."""

    def test_parse_simple_fastq(self):
        data = b"@seq1 description\nATGCGATC\n+\nIIIIIIII\n"
        parser = FASTQParser()

        result = parser.parse(data, "trace-1")

        assert result.traceId == "trace-1"
        assert result.format == TraceFormat.FASTQ
        assert result.sequence.sequence == "ATGCGATC"
        assert result.sequence.name == "seq1"
        # BioPython includes full header in description
        assert "description" in (result.sequence.description or "")
        assert result.chromatogram is None

    def test_quality_scores(self):
        # 'I' = ASCII 73, Phred = 73 - 33 = 40
        data = b"@seq1\nATGC\n+\nIIII\n"
        parser = FASTQParser()

        result = parser.parse(data, "trace-1")

        assert result.sequence.quality is not None
        assert result.sequence.quality == [40, 40, 40, 40]

    def test_quality_metrics_calculation(self):
        # Quality scores: 40, 40, 15, 25 (I=40, I=40, 0=15, :=25)
        data = b"@seq1\nATGC\n+\nII0:\n"
        parser = FASTQParser()

        result = parser.parse(data, "trace-1")

        metrics = result.qualityMetrics
        assert metrics.length == 4
        # Q20: 40, 40, 25 = 3/4 = 75%
        assert metrics.q20Percentage == 75.0
        # Q30: 40, 40 = 2/4 = 50%
        assert metrics.q30Percentage == 50.0

    def test_parse_multiple_records(self):
        data = b"@seq1\nATGC\n+\nIIII\n@seq2\nGCTA\n+\nJJJJ\n"
        parser = FASTQParser()

        # parse() returns first record
        result = parser.parse(data, "trace-1")
        assert result.sequence.sequence == "ATGC"

        # parse_all() returns all records
        all_records = parser.parse_all(data)
        assert len(all_records) == 2
        assert all_records[0]["sequence"] == "ATGC"
        assert all_records[1]["sequence"] == "GCTA"

    def test_invalid_fastq_too_few_lines(self):
        parser = FASTQParser()

        with pytest.raises(ValueError):
            parser.parse(b"@seq1\nATGC\n", "trace-1")

    def test_invalid_fastq_mismatched_lengths(self):
        parser = FASTQParser()

        with pytest.raises(ValueError):
            parser.parse(b"@seq1\nATGCGATC\n+\nIII\n", "trace-1")

    def test_fastq_extensions(self):
        parser = FASTQParser()
        assert parser.extensions == ["fastq", "fq"]
        assert parser.format == TraceFormat.FASTQ


class TestAB1Parser:
    """Tests for AB1 parser."""

    def test_ab1_extensions(self):
        parser = AB1Parser()
        assert parser.extensions == ["ab1", "abi"]
        assert parser.format == TraceFormat.AB1

    def test_invalid_magic_raises_error(self):
        parser = AB1Parser()

        # Create fake file with wrong magic
        fake_data = b"XXXX" + b"\x00" * 200

        with pytest.raises(ValueError, match="Invalid AB1 file"):
            parser.parse(fake_data, "trace-1")

    def test_file_too_small_raises_error(self):
        parser = AB1Parser()

        with pytest.raises(ValueError, match="Invalid AB1"):
            parser.parse(b"ABIF", "trace-1")


class TestSCFParser:
    """Tests for SCF parser."""

    def test_scf_extensions(self):
        parser = SCFParser()
        assert parser.extensions == ["scf"]
        assert parser.format == TraceFormat.SCF

    def test_invalid_magic_raises_error(self):
        parser = SCFParser()

        # Create fake file with wrong magic
        fake_data = b"XXXX" + b"\x00" * 200

        with pytest.raises(ValueError, match="Invalid SCF file"):
            parser.parse(fake_data, "trace-1")

    def test_file_too_small_raises_error(self):
        parser = SCFParser()

        with pytest.raises(ValueError, match="too small"):
            parser.parse(b".scf", "trace-1")


class TestParserFactory:
    """Tests for parser selection."""

    def test_select_parser_by_extension(self):
        parsers = [AB1Parser(), SCFParser(), FASTQParser(), FASTAParser()]

        def get_parser(filename: str) -> BaseParser | None:
            for parser in parsers:
                if parser.can_parse(filename):
                    return parser
            return None

        assert isinstance(get_parser("sample.ab1"), AB1Parser)
        assert isinstance(get_parser("sample.abi"), AB1Parser)
        assert isinstance(get_parser("sample.scf"), SCFParser)
        assert isinstance(get_parser("sample.fastq"), FASTQParser)
        assert isinstance(get_parser("sample.fq"), FASTQParser)
        assert isinstance(get_parser("sample.fasta"), FASTAParser)
        assert isinstance(get_parser("sample.fa"), FASTAParser)
        assert get_parser("sample.unknown") is None
