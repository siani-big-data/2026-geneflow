"""SCF (Standard Chromatogram Format) trace file parser."""

import struct
from typing import Any

from src.constants import gc_content
from src.models import (
    ChromatogramData,
    ParsedTrace,
    QualityMetrics,
    Sequence,
    TraceFormat,
)
from src.parsers.parser import BaseParser


class SCFParser(BaseParser):
    """Parser for SCF trace files."""

    MAGIC = b".scf"

    HEADER_SIZE = 128

    @property
    def format(self) -> TraceFormat:
        return TraceFormat.SCF

    @property
    def extensions(self) -> list[str]:
        return ["scf"]

    def parse(self, data: bytes, trace_id: str) -> ParsedTrace:
        """Parse SCF file data."""
        if len(data) < self.HEADER_SIZE:
            raise ValueError("File too small to be valid SCF")

        header = self._parse_header(data)

        if header["magic"] != self.MAGIC:
            raise ValueError(f"Invalid SCF file: expected {self.MAGIC}, got {header['magic']}")

        sequence_str = self._read_bases(data, header)
        quality = self._read_quality(data, header)

        sequence = Sequence(
            id=trace_id,
            sequence=sequence_str,
            quality=quality,
        )

        chromatogram = self._read_chromatogram(data, header)
        quality_metrics = self._calculate_metrics(sequence_str, quality)
        metadata = self._read_comments(data, header)

        return ParsedTrace(
            traceId=trace_id,
            format=TraceFormat.SCF,
            sequence=sequence,
            chromatogram=chromatogram,
            qualityMetrics=quality_metrics,
            metadata=metadata,
        )

    def _parse_header(self, data: bytes) -> dict[str, Any]:
        """Parse SCF file header (128 bytes, big-endian)."""
        magic = data[0:4]
        num_samples = struct.unpack(">I", data[4:8])[0]
        samples_offset = struct.unpack(">I", data[8:12])[0]
        num_bases = struct.unpack(">I", data[12:16])[0]
        bases_offset = struct.unpack(">I", data[24:28])[0]
        comments_size = struct.unpack(">I", data[28:32])[0]
        comments_offset = struct.unpack(">I", data[32:36])[0]
        version = data[36:40].decode("ascii", errors="replace").strip("\x00")
        sample_size = struct.unpack(">I", data[40:44])[0]

        return {
            "magic": magic,
            "numSamples": num_samples,
            "samplesOffset": samples_offset,
            "numBases": num_bases,
            "basesOffset": bases_offset,
            "commentsSize": comments_size,
            "commentsOffset": comments_offset,
            "version": version,
            "sampleSize": sample_size if sample_size in [1, 2] else 1,
        }

    def _read_bases(self, data: bytes, header: dict) -> str:
        """Read base sequence from SCF file."""
        num_bases = header["numBases"]
        bases_offset = header["basesOffset"]

        bases_start = bases_offset + (4 * num_bases) + (4 * num_bases)

        bases = data[bases_start : bases_start + num_bases]
        return bases.decode("ascii", errors="replace")

    def _read_quality(self, data: bytes, header: dict) -> list[int]:
        """Read quality scores from SCF file (max of per-base probabilities)."""
        num_bases = header["numBases"]
        bases_offset = header["basesOffset"]

        prob_offset = bases_offset + (4 * num_bases)

        quality = []
        for i in range(num_bases):
            prob_a = data[prob_offset + i]
            prob_c = data[prob_offset + num_bases + i]
            prob_g = data[prob_offset + (2 * num_bases) + i]
            prob_t = data[prob_offset + (3 * num_bases) + i]

            max_prob = max(prob_a, prob_c, prob_g, prob_t)
            quality.append(max_prob)

        return quality

    def _read_chromatogram(self, data: bytes, header: dict) -> ChromatogramData | None:
        """Read chromatogram trace data from SCF file."""
        num_samples = header["numSamples"]
        samples_offset = header["samplesOffset"]
        sample_size = header["sampleSize"]
        num_bases = header["numBases"]
        bases_offset = header["basesOffset"]

        if num_samples == 0:
            return None

        traces = {"A": [], "C": [], "G": [], "T": []}
        channels = ["A", "C", "G", "T"]

        for channel_idx, channel in enumerate(channels):
            channel_offset = samples_offset + (channel_idx * num_samples * sample_size)

            for i in range(num_samples):
                if sample_size == 1:
                    value = data[channel_offset + i]
                else:
                    pos = channel_offset + (i * 2)
                    value = struct.unpack(">H", data[pos : pos + 2])[0]
                traces[channel].append(value)

        peak_locs = []
        for i in range(num_bases):
            pos = bases_offset + (i * 4)
            peak_loc = struct.unpack(">I", data[pos : pos + 4])[0]
            peak_locs.append(peak_loc)

        return ChromatogramData(
            traceA=traces["A"],
            traceC=traces["C"],
            traceG=traces["G"],
            traceT=traces["T"],
            baseCalls=list(range(num_bases)),
            peakLocations=peak_locs,
        )

    def _read_comments(self, data: bytes, header: dict) -> dict[str, str]:
        """Read comments section as metadata."""
        comments_size = header["commentsSize"]
        comments_offset = header["commentsOffset"]

        if comments_size == 0:
            return {}

        comments_data = data[comments_offset : comments_offset + comments_size]
        comments_str = comments_data.decode("ascii", errors="replace")

        metadata = {}
        for line in comments_str.split("\n"):
            if "=" in line:
                key, _, value = line.partition("=")
                key = key.strip()
                value = value.strip().rstrip("\x00")
                if key and value:
                    metadata[key] = value

        return metadata

    def _calculate_metrics(self, sequence: str, quality: list[int] | None) -> QualityMetrics:
        """Calculate quality metrics."""
        if not quality:
            return QualityMetrics(
                meanQuality=0.0,
                q20Percentage=0.0,
                q30Percentage=0.0,
                gcContent=gc_content(sequence),
                length=len(sequence),
                ambiguousCount=sum(1 for b in sequence if b not in "ACGT"),
            )

        mean_q = sum(quality) / len(quality) if quality else 0.0
        q20_count = sum(1 for q in quality if q >= 20)
        q30_count = sum(1 for q in quality if q >= 30)

        return QualityMetrics(
            meanQuality=round(mean_q, 2),
            q20Percentage=round((q20_count / len(quality)) * 100, 2) if quality else 0.0,
            q30Percentage=round((q30_count / len(quality)) * 100, 2) if quality else 0.0,
            gcContent=round(gc_content(sequence), 2),
            length=len(sequence),
            ambiguousCount=sum(1 for b in sequence if b not in "ACGT"),
        )
