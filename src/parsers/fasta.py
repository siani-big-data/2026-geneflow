"""FASTA file parser."""

from src.models import (
    ParsedTrace,
    QualityMetrics,
    Sequence,
    TraceFormat,
)
from src.parsers.parser import BaseParser
from src.constants import gc_content


class FASTAParser(BaseParser):
    """Parser for FASTA files."""

    @property
    def format(self) -> TraceFormat:
        return TraceFormat.FASTA

    @property
    def extensions(self) -> list[str]:
        return ["fasta", "fa", "fna", "fas"]

    def parse(self, data: bytes, trace_id: str) -> ParsedTrace:
        """Parse FASTA file data (first sequence only)."""
        text = data.decode("utf-8", errors="replace")

        # Parse first record
        record = self._parse_first_record(text)

        if not record:
            raise ValueError("No valid FASTA sequence found")

        # Create sequence object
        sequence = Sequence(
            id=trace_id,
            sequence=record["sequence"],
            quality=None,  # FASTA has no quality scores
            name=record["name"],
            description=record["description"],
        )

        # Calculate quality metrics (no quality scores)
        quality_metrics = QualityMetrics(
            meanQuality=0.0,
            q20Percentage=0.0,
            q30Percentage=0.0,
            gcContent=round(gc_content(record["sequence"]), 2),
            length=len(record["sequence"]),
            ambiguousCount=sum(1 for b in record["sequence"] if b not in "ACGT"),
        )

        return ParsedTrace(
            traceId=trace_id,
            format=TraceFormat.FASTA,
            sequence=sequence,
            chromatogram=None,  # FASTA has no chromatogram
            qualityMetrics=quality_metrics,
            metadata={"name": record["name"]} if record["name"] else {},
        )

    def parse_all(self, data: bytes) -> list[dict]:
        """Parse all sequences from a FASTA file."""
        text = data.decode("utf-8", errors="replace")
        return self._parse_all_records(text)

    def _parse_first_record(self, text: str) -> dict | None:
        """Parse the first FASTA record."""
        lines = text.strip().split("\n")

        header = None
        sequence_lines = []

        for line in lines:
            line = line.strip()

            if not line:
                continue

            if line.startswith(">"):
                if header is not None:
                    # We've hit a second sequence, return the first
                    break
                header = line[1:]  # Remove >
            elif header is not None:
                # Only collect sequence if we have a header
                sequence_lines.append(line)

        if header is None or not sequence_lines:
            return None

        # Parse header
        parts = header.split(None, 1)
        name = parts[0] if parts else ""
        description = parts[1] if len(parts) > 1 else ""

        # Join sequence lines and normalize
        sequence = "".join(sequence_lines).upper()
        # Remove any non-letter characters (spaces, numbers, etc.)
        sequence = "".join(c for c in sequence if c.isalpha())

        return {
            "name": name,
            "description": description,
            "sequence": sequence,
        }

    def _parse_all_records(self, text: str) -> list[dict]:
        """Parse all FASTA records from text."""
        lines = text.strip().split("\n")
        records = []

        current_header = None
        current_sequence = []

        for line in lines:
            line = line.strip()

            if not line:
                continue

            if line.startswith(">"):
                # Save previous record
                if current_header is not None and current_sequence:
                    records.append(self._make_record(current_header, current_sequence))

                # Start new record
                current_header = line[1:]
                current_sequence = []
            elif current_header is not None:
                current_sequence.append(line)

        # Don't forget the last record
        if current_header is not None and current_sequence:
            records.append(self._make_record(current_header, current_sequence))

        return records

    def _make_record(self, header: str, sequence_lines: list[str]) -> dict:
        """Create a record dict from header and sequence lines."""
        parts = header.split(None, 1)
        name = parts[0] if parts else ""
        description = parts[1] if len(parts) > 1 else ""

        sequence = "".join(sequence_lines).upper()
        sequence = "".join(c for c in sequence if c.isalpha())

        return {
            "name": name,
            "description": description,
            "sequence": sequence,
        }
