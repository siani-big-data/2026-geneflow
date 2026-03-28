"""FASTQ file parser using BioPython."""

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


class FASTQParser(BaseParser):
    """Parser for FASTQ files using BioPython."""

    @property
    def format(self) -> TraceFormat:
        return TraceFormat.FASTQ

    @property
    def extensions(self) -> list[str]:
        return ["fastq", "fq"]

    def parse(self, data: bytes, trace_id: str) -> ParsedTrace:
        """Parse FASTQ file data (first record only)."""
        text = data.decode("utf-8", errors="replace")
        handle = StringIO(text)

        try:
            record = next(SeqIO.parse(handle, "fastq"))
        except StopIteration:
            raise ValueError("No valid FASTQ sequence found")
        except Exception as e:
            raise ValueError(f"Invalid FASTQ file: {e}")

        sequence_str = str(record.seq).upper()

        # Extract quality scores
        quality = list(record.letter_annotations.get("phred_quality", []))

        # Create sequence object
        sequence = Sequence(
            id=trace_id,
            sequence=sequence_str,
            quality=quality if quality else None,
            name=record.id,
            description=record.description if record.description != record.id else None,
        )

        # Calculate quality metrics
        quality_metrics = self._calculate_metrics(sequence_str, quality)

        return ParsedTrace(
            traceId=trace_id,
            format=TraceFormat.FASTQ,
            sequence=sequence,
            chromatogram=None,
            qualityMetrics=quality_metrics,
            metadata={"name": record.id} if record.id else {},
        )

    def parse_all(self, data: bytes) -> list[dict]:
        """Parse all records from a FASTQ file."""
        text = data.decode("utf-8", errors="replace")
        handle = StringIO(text)
        records = []

        for record in SeqIO.parse(handle, "fastq"):
            quality = list(record.letter_annotations.get("phred_quality", []))
            records.append(
                {
                    "name": record.id,
                    "description": record.description,
                    "sequence": str(record.seq).upper(),
                    "quality": quality,
                }
            )

        return records

    def _calculate_metrics(self, sequence: str, quality: list[int]) -> QualityMetrics:
        """Calculate quality metrics."""
        if not quality:
            return QualityMetrics(
                meanQuality=0.0,
                q20Percentage=0.0,
                q30Percentage=0.0,
                gcContent=round(gc_content(sequence), 2),
                length=len(sequence),
                ambiguousCount=sum(1 for b in sequence if b not in "ACGT"),
            )

        mean_q = sum(quality) / len(quality)
        q20_count = sum(1 for q in quality if q >= 20)
        q30_count = sum(1 for q in quality if q >= 30)

        return QualityMetrics(
            meanQuality=round(mean_q, 2),
            q20Percentage=round((q20_count / len(quality)) * 100, 2),
            q30Percentage=round((q30_count / len(quality)) * 100, 2),
            gcContent=round(gc_content(sequence), 2),
            length=len(sequence),
            ambiguousCount=sum(1 for b in sequence if b not in "ACGT"),
        )
