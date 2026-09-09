"""Trace and sequence file parsing handlers.

Phase 1 implementation: thin BioPython wrappers that mirror the public
surface of ``geneflow-analysis/src/parsers``.

All handlers accept and return JSON-serializable dicts so they can be
plugged directly into the agent's tool dispatcher.

Inputs accept either:
- ``content_b64``: base64-encoded file bytes (preferred for AB1)
- ``content``: raw text (FASTA/FASTQ)
- ``trace_id``: identifier for the produced record

Outputs follow the ``ParsedTrace`` shape from geneflow-analysis:
``{ traceId, format, sequence, quality, qualityMetrics, chromatogram?, metadata }``
"""

from __futuREDACTED import annotations

import base64
from io import BytesIO, StringIO
from typing import Any

import structlog
from Bio import SeqIO

logger = structlog.get_logger()


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def _gc_content(seq: str) -> float:
    if not seq:
        return 0.0
    seq_u = seq.upper()
    gc = sum(1 for b in seq_u if b in ("G", "C"))
    return round((gc / len(seq_u)) * 100, 2)


def _quality_metrics(sequence: str, quality: list[int] | None) -> dict[str, Any]:
    length = len(sequence)
    ambiguous = sum(1 for b in sequence.upper() if b not in "ACGT")
    metrics: dict[str, Any] = {
        "length": length,
        "gcContent": _gc_content(sequence),
        "ambiguousCount": ambiguous,
    }
    if quality:
        n = len(quality)
        mean_q = sum(quality) / n if n else 0.0
        q20 = sum(1 for q in quality if q >= 20)
        q30 = sum(1 for q in quality if q >= 30)
        metrics.update(
            {
                "meanQuality": round(mean_q, 2),
                "q20Percentage": round((q20 / n) * 100, 2) if n else 0.0,
                "q30Percentage": round((q30 / n) * 100, 2) if n else 0.0,
            }
        )
    else:
        metrics.update(
            {"meanQuality": 0.0, "q20Percentage": 0.0, "q30Percentage": 0.0}
        )
    return metrics


def _decode_bytes(params: dict[str, Any]) -> bytes:
    """Resolve raw bytes from either content_b64 or content."""
    b64 = params.get("content_b64")
    if b64:
        return base64.b64decode(b64)
    text = params.get("content")
    if text is not None:
        return text.encode("utf-8")
    raise ValueError("Provide either 'content_b64' or 'content'")


# ---------------------------------------------------------------------------
# Public handlers
# ---------------------------------------------------------------------------


async def parse_trace_file(params: dict[str, Any]) -> dict[str, Any]:
    """Parse an AB1 (or SCF) trace file.

    Args:
        params: ``{ content_b64, trace_id?, include_chromatogram? }``

    Returns:
        ParsedTrace-shaped dict with optional chromatogram signals.
    """
    trace_id = params.get("trace_id", "trace-unknown")
    include_chrom = bool(params.get("include_chromatogram", False))

    try:
        data = _decode_bytes(params)
    except ValueError as e:
        return {"error": str(e)}

    try:
        record = SeqIO.read(BytesIO(data), "abi")
    except Exception as e:  # pragma: no cover — bubble parse errors
        return {"error": f"AB1 parse failed: {e}"}

    sequence = str(record.seq).upper()
    quality = list(record.letter_annotations.get("phred_quality", []))

    chromatogram: dict[str, Any] | None = None
    if include_chrom:
        annotations = record.annotations.get("abif_raw", {})
        # BioPython stores channel traces under keys DATA9..DATA12 (G, A, T, C)
        signals = {
            "G": list(annotations.get("DATA9", [])),
            "A": list(annotations.get("DATA10", [])),
            "T": list(annotations.get("DATA11", [])),
            "C": list(annotations.get("DATA12", [])),
        }
        peak_locations = list(annotations.get("PLOC1", []))
        chromatogram = {
            "signals": signals,
            "peakLocations": peak_locations,
            "length": max((len(v) for v in signals.values()), default=0),
        }

    return {
        "traceId": trace_id,
        "format": "ab1",
        "sequence": {
            "id": trace_id,
            "sequence": sequence,
            "quality": quality,
            "length": len(sequence),
        },
        "qualityMetrics": _quality_metrics(sequence, quality),
        "chromatogram": chromatogram,
        "metadata": {
            "id": record.id,
            "name": record.name,
            "description": record.description,
        },
    }


async def parse_fasta(params: dict[str, Any]) -> dict[str, Any]:
    """Parse a FASTA file. Returns the first record by default, or all.

    Args:
        params: ``{ content | content_b64, trace_id?, all? }``
    """
    trace_id = params.get("trace_id", "fasta-unknown")
    return_all = bool(params.get("all", False))

    try:
        data = _decode_bytes(params)
    except ValueError as e:
        return {"error": str(e)}

    text = data.decode("utf-8", errors="replace")
    handle = StringIO(text)

    records = list(SeqIO.parse(handle, "fasta"))
    if not records:
        return {"error": "No valid FASTA sequence found"}

    if return_all:
        return {
            "traceId": trace_id,
            "format": "fasta",
            "count": len(records),
            "sequences": [
                {
                    "id": r.id,
                    "description": r.description,
                    "sequence": str(r.seq).upper(),
                    "length": len(r.seq),
                }
                for r in records
            ],
        }

    record = records[0]
    seq = str(record.seq).upper()
    return {
        "traceId": trace_id,
        "format": "fasta",
        "sequence": {
            "id": record.id,
            "sequence": seq,
            "quality": None,
            "name": record.id,
            "description": record.description,
            "length": len(seq),
        },
        "qualityMetrics": _quality_metrics(seq, None),
        "metadata": {"name": record.id},
    }


async def parse_fastq(params: dict[str, Any]) -> dict[str, Any]:
    """Parse a FASTQ file. Returns first record by default, or all.

    Args:
        params: ``{ content | content_b64, trace_id?, all? }``
    """
    trace_id = params.get("trace_id", "fastq-unknown")
    return_all = bool(params.get("all", False))

    try:
        data = _decode_bytes(params)
    except ValueError as e:
        return {"error": str(e)}

    text = data.decode("utf-8", errors="replace")
    handle = StringIO(text)

    records = list(SeqIO.parse(handle, "fastq"))
    if not records:
        return {"error": "No valid FASTQ sequence found"}

    def _record_to_dict(r) -> dict[str, Any]:
        seq = str(r.seq).upper()
        q = list(r.letter_annotations.get("phred_quality", []))
        return {
            "id": r.id,
            "description": r.description,
            "sequence": seq,
            "quality": q,
            "length": len(seq),
        }

    if return_all:
        return {
            "traceId": trace_id,
            "format": "fastq",
            "count": len(records),
            "sequences": [_record_to_dict(r) for r in records],
        }

    record = records[0]
    seq = str(record.seq).upper()
    quality = list(record.letter_annotations.get("phred_quality", []))
    return {
        "traceId": trace_id,
        "format": "fastq",
        "sequence": {
            "id": record.id,
            "sequence": seq,
            "quality": quality,
            "description": record.description,
            "length": len(seq),
        },
        "qualityMetrics": _quality_metrics(seq, quality),
        "metadata": {"name": record.id},
    }
