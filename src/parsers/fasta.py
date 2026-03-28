"""FASTA file parser using BioPython."""

from io import StringIO

from Bio import SeqIO

from src.constants import gc_content
from src.models import (
    ParsedTrace,
    QualityMetrics,
    Sequence,
    TraceFormat,
)
from src.parsers.parser import BaseParser


class FASTAParser(BaseParser):
    """Parser for FASTA files using BioPython."""

    @property
    def format(self) -> TraceFormat:
        return TraceFormat.FASTA

    @property
    def extensions(self) -> list[str]:
        return ["fasta", "fa", "fna", "fas"]

    def parse(self, data: bytes, trace_id: str) -> ParsedTrace:
        """Parse FASTA file data (first sequence only)."""
        text = data.decode("utf-8", errors="replace")
        handle = StringIO(text)

        try:
            record = next(SeqIO.parse(handle, "fasta"))
        except StopIteration:
            raise ValueError("No valid FASTA sequence found")
        except Exception as e:
            raise ValueError(f"Invalid FASTA file: {e}")

        sequence_str = str(record.seq).upper()

        # Create sequence object
        sequence = Sequence(
            id=trace_id,
            sequence=sequence_str,
            quality=None,  # FASTA has no quality scores
            name=record.id,
            description=record.description if record.description != record.id else None,
        )

        # Calculate quality metrics (no quality scores)
        quality_metrics = QualityMetrics(
            meanQuality=0.0,
            q20Percentage=0.0,
            q30Percentage=0.0,
            gcContent=round(gc_content(sequence_str), 2),
            length=len(sequence_str),
            ambiguousCount=sum(1 for b in sequence_str if b not in "ACGT"),
        )

        return ParsedTrace(
            traceId=trace_id,
            format=TraceFormat.FASTA,
            sequence=sequence,
            chromatogram=None,
            qualityMetrics=quality_metrics,
            metadata={"name": record.id} if record.id else {},
        )

    def parse_all(self, data: bytes) -> list[dict]:
        """Parse all sequences from a FASTA file."""
        text = data.decode("utf-8", errors="replace")
        handle = StringIO(text)
        records = []

        for record in SeqIO.parse(handle, "fasta"):
            records.append(
                {
                    "name": record.id,
                    "description": record.description,
                    "sequence": str(record.seq).upper(),
                }
            )

        return records
