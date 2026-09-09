"""AB1 (Applied Biosystems) trace file parser using BioPython."""

from io import BytesIO

from Bio import SeqIO

from src.constants import gc_content
from src.models import (
    ChromatogramData,
    ParsedTrace,
    QualityMetrics,
    Sequence,
    TraceFormat,
)
from src.parsers.parser import BaseParser


class AB1Parser(BaseParser):
    """Parser for AB1/ABI trace files using BioPython."""

    @property
    def format(self) -> TraceFormat:
        return TraceFormat.AB1

    @property
    def extensions(self) -> list[str]:
        return ["ab1", "abi"]

    def parse(self, data: bytes, trace_id: str) -> ParsedTrace:
        """Parse AB1 file data using BioPython."""
        handle = BytesIO(data)

        try:
            record = SeqIO.read(handle, "abi")
        except Exception as e:
            raise ValueError(f"Invalid AB1 file: {e}")

        sequence_str = str(record.seq)

        quality = None
        if "phred_quality" in record.letter_annotations:
            quality = list(record.letter_annotations["phred_quality"])

        name = (
            self._to_string(record.name)
            if record.name
            else (self._to_string(record.id) if record.id else None)
        )

        description = None
        if record.description and record.description != "<unknown description>":
            description = self._to_string(record.description)

        sequence = Sequence(
            id=trace_id,
            sequence=sequence_str,
            quality=quality,
            name=name,
            description=description,
        )

        chromatogram = self._extract_chromatogram(record)
        quality_metrics = self._calculate_metrics(sequence_str, quality)
        metadata = self._extract_metadata(record)

        return ParsedTrace(
            traceId=trace_id,
            format=TraceFormat.AB1,
            sequence=sequence,
            chromatogram=chromatogram,
            qualityMetrics=quality_metrics,
            metadata=metadata,
        )

    def _extract_chromatogram(self, record) -> ChromatogramData | None:
        """Extract chromatogram from BioPython record (DATA9=G, DATA10=A, DATA11=T, DATA12=C)."""
        try:
            abif_raw = record.annotations.get("abif_raw", {})

            if not abif_raw:
                return None

            trace_g = list(abif_raw.get("DATA9", []))
            trace_a = list(abif_raw.get("DATA10", []))
            trace_t = list(abif_raw.get("DATA11", []))
            trace_c = list(abif_raw.get("DATA12", []))

            if not any([trace_g, trace_a, trace_t, trace_c]):
                return None

            peak_locs = list(abif_raw.get("PLOC2", abif_raw.get("PLOC1", [])))

            return ChromatogramData(
                traceG=trace_g,
                traceA=trace_a,
                traceT=trace_t,
                traceC=trace_c,
                baseCalls=list(range(len(peak_locs))),
                peakLocations=peak_locs,
            )
        except Exception:
            return None

    def _calculate_metrics(self, sequence: str, quality: list[int] | None) -> QualityMetrics:
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

    def _extract_metadata(self, record) -> dict:
        """Extract metadata from BioPython record."""
        metadata = {}
        annotations = record.annotations

        if "sample_well" in annotations:
            metadata["sampleWell"] = self._to_string(annotations["sample_well"])

        if "run_start" in annotations:
            metadata["runStart"] = str(annotations["run_start"])

        if "run_finish" in annotations:
            metadata["runFinish"] = str(annotations["run_finish"])

        if "machine_model" in annotations:
            metadata["machineModel"] = self._to_string(annotations["machine_model"])

        if record.name:
            metadata["sampleName"] = self._to_string(record.name)

        return metadata

    def _to_string(self, value) -> str:
        """Convert value to string, handling bytes."""
        if isinstance(value, bytes):
            return value.decode("utf-8", errors="replace")
        return str(value)
