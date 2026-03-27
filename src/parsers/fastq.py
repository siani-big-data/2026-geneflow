
"""FASTQ file parser."""

from src.models import (
    ParsedTrace,
    QualityMetrics,
    Sequence,
    TraceFormat,
)
from src.parsers.parser import BaseParser
from src.constants import gc_content


class FASTQParser(BaseParser):
    """Parser for FASTQ files."""

    # Quality score encoding offset (Phred+33 is standard)
    PHRED_OFFSET = 33

    @property
    def format(self) -> TraceFormat:
        return TraceFormat.FASTQ

    @property
    def extensions(self) -> list[str]:
        return ["fastq", "fq"]

    def parse(self, data: bytes, trace_id: str) -> ParsedTrace:
        """Parse FASTQ file data (first record only)."""
        text = data.decode("utf-8", errors="replace")
        lines = text.strip().split("\n")

        if len(lines) < 4:
            raise ValueError("Invalid FASTQ: need at least 4 lines")

        # Parse first record
        record = self._parse_record(lines, 0)

        # Create sequence object
        sequence = Sequence(
            id=trace_id,
            sequence=record["sequence"],
            quality=record["quality"],
            name=record["name"],
            description=record["description"],
        )

        # Calculate quality metrics
        quality_metrics = self._calculate_metrics(
            record["sequence"], record["quality"]
        )

        return ParsedTrace(
            traceId=trace_id,
            format=TraceFormat.FASTQ,
            sequence=sequence,
            chromatogram=None,  # FASTQ has no chromatogram
            qualityMetrics=quality_metrics,
            metadata={"name": record["name"]} if record["name"] else {},
        )

    def parse_all(self, data: bytes) -> list[dict]:
        """Parse all records from a FASTQ file."""
        text = data.decode("utf-8", errors="replace")
        lines = text.strip().split("\n")

        records = []
        i = 0

        while i < len(lines) - 3:
            try:
                record = self._parse_record(lines, i)
                records.append(record)
                i += 4
            except ValueError:
                # Skip malformed records
                i += 1

        return records

    def _parse_record(self, lines: list[str], start: int) -> dict:
        """Parse a single FASTQ record starting at the given line index."""
        if start + 3 >= len(lines):
            raise ValueError(f"Not enough lines for FASTQ record at line {start}")

        header = lines[start].strip()
        sequence_line = lines[start + 1].strip()
        plus_line = lines[start + 2].strip()
        quality_line = lines[start + 3].strip()

        # Validate header
        if not header.startswith("@"):
            raise ValueError(f"Invalid FASTQ header at line {start}: {header[:50]}")

        # Validate plus line
        if not plus_line.startswith("+"):
            raise ValueError(f"Invalid FASTQ plus line at line {start + 2}")

        # Validate lengths match
        if len(sequence_line) != len(quality_line):
            raise ValueError(
                f"Sequence and quality lengths don't match: "
                f"{len(sequence_line)} vs {len(quality_line)}"
            )

        # Parse header
        header_content = header[1:]  # Remove @
        parts = header_content.split(None, 1)
        name = parts[0] if parts else ""
        description = parts[1] if len(parts) > 1 else ""

        # Parse quality scores (Phred+33)
        quality = [ord(c) - self.PHRED_OFFSET for c in quality_line]

        return {
            "name": name,
            "description": description,
            "sequence": sequence_line.upper(),
            "quality": quality,
        }

    def _calculate_metrics(
        self, sequence: str, quality: list[int]
    ) -> QualityMetrics:
        """Calculate quality metrics."""
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
