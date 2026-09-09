"""Tests for SCF parser using synthetic SCF binaries."""

import struct

import pytest

from src.parsers import SCFParser


def _build_scf(
    bases: bytes = b"ACGT",
    qualities: tuple = (10, 20, 30, 40),
    chromatogram_samples: int = 8,
    sample_size: int = 1,
    comments: bytes = b"NAME=sample\nINSTRUMENT=test\n",
) -> bytes:
    """Construct a minimal valid SCF v3 byte stream."""
    num_bases = len(bases)
    num_samples = chromatogram_samples

    header_size = 128
    samples_offset = header_size
    samples_block_size = num_samples * sample_size * 4
    bases_offset = samples_offset + samples_block_size
    bases_block_size = (num_bases * 4) + (num_bases * 4) + num_bases
    comments_offset = bases_offset + bases_block_size
    comments_size = len(comments)

    header = bytearray(header_size)
    header[0:4] = b".scf"
    struct.pack_into(">I", header, 4, num_samples)
    struct.pack_into(">I", header, 8, samples_offset)
    struct.pack_into(">I", header, 12, num_bases)
    struct.pack_into(">I", header, 24, bases_offset)
    struct.pack_into(">I", header, 28, comments_size)
    struct.pack_into(">I", header, 32, comments_offset)
    header[36:40] = b"3.00"
    struct.pack_into(">I", header, 40, sample_size)

    samples = bytearray()
    for _ in range(4):
        for i in range(num_samples):
            value = (i * 30) % 250 + 5
            if sample_size == 1:
                samples.append(value)
            else:
                samples += struct.pack(">H", value)

    peak_locations = b"".join(
        struct.pack(">I", i * (num_samples // num_bases or 1)) for i in range(num_bases)
    )

    probs = bytearray()
    for ch_idx in range(4):
        for i in range(num_bases):
            q = qualities[i] if i < len(qualities) else 30
            probs.append(q if (ch_idx + i) % 4 == 0 else max(0, q - 10))

    bases_block = peak_locations + bytes(probs) + bases

    return bytes(header) + bytes(samples) + bases_block + comments


class TestSCFParserSynthetic:
    def test_parse_minimal_scf(self):
        data = _build_scf()
        parser = SCFParser()

        parsed = parser.parse(data, "trace-1")

        assert parsed.traceId == "trace-1"
        assert parsed.sequence.sequence == "ACGT"
        assert parsed.sequence.quality is not None
        assert len(parsed.sequence.quality) == 4
        assert parsed.chromatogram is not None
        assert len(parsed.chromatogram.traceA) == 8
        assert len(parsed.chromatogram.peakLocations) == 4

    def test_parse_includes_metadata_from_comments(self):
        data = _build_scf(comments=b"NAME=mySample\nMACHINE=ABI3500\n")
        parser = SCFParser()

        parsed = parser.parse(data, "t")

        assert parsed.metadata.get("NAME") == "mySample"
        assert parsed.metadata.get("MACHINE") == "ABI3500"

    def test_parse_no_comments(self):
        data = _build_scf(comments=b"")
        parser = SCFParser()

        parsed = parser.parse(data, "t")

        assert parsed.metadata == {}

    def test_parse_sample_size_two(self):
        data = _build_scf(chromatogram_samples=6, sample_size=2)
        parser = SCFParser()

        parsed = parser.parse(data, "t")

        assert parsed.chromatogram is not None
        assert len(parsed.chromatogram.traceA) == 6

    def test_parse_zero_samples_returns_no_chromatogram(self):
        data = _build_scf(chromatogram_samples=0)
        parser = SCFParser()

        parsed = parser.parse(data, "t")

        assert parsed.chromatogram is None

    def test_quality_metrics_calculated(self):
        data = _build_scf(qualities=(30, 30, 30, 30))
        parser = SCFParser()

        parsed = parser.parse(data, "t")

        assert parsed.qualityMetrics is not None
        assert parsed.qualityMetrics.length == 4
        assert parsed.qualityMetrics.meanQuality > 0

    def test_invalid_magic_raises(self):
        bad = b"XXXX" + b"\x00" * 200
        with pytest.raises(ValueError, match="Invalid SCF"):
            SCFParser().parse(bad, "t")

    def test_too_small_raises(self):
        with pytest.raises(ValueError, match="too small"):
            SCFParser().parse(b".scf", "t")

    def test_quality_metrics_no_quality(self):
        from src.parsers.scf import SCFParser as _P

        metrics = _P()._calculate_metrics("ACGT", quality=None)
        assert metrics.meanQuality == 0.0
        assert metrics.length == 4
