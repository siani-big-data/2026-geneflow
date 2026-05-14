"""
Chunking utilities for splitting trace data into manageable chunks.

Splits large trace sequences into chunks for efficient storage and
paginated retrieval from the datalake.
"""

from dataclasses import dataclass, field
from datetime import datetime, timezone
from typing import Optional

from src.models import ChromatogramData, ParsedTrace

DEFAULT_CHUNK_SIZE = 10_000


@dataclass
class ChunkMetadata:
    """Metadata for a single chunk."""

    index: int
    startPosition: int
    endPosition: int
    baseCount: int
    filename: str

    def to_dict(self) -> dict:
        """Serialize to dictionary."""
        return {
            "index": self.index,
            "start_position": self.startPosition,
            "end_position": self.endPosition,
            "base_count": self.baseCount,
            "filename": self.filename,
        }


@dataclass
class TraceChunk:
    """A chunk of trace sequence data."""

    index: int
    startPosition: int
    endPosition: int
    bases: str
    qualityScores: Optional[list[int]] = None
    chromatogram: Optional[dict] = None

    def to_dict(self) -> dict:
        """Serialize to dictionary."""
        result = {
            "index": self.index,
            "start_position": self.startPosition,
            "end_position": self.endPosition,
            "bases": self.bases,
        }
        if self.qualityScores is not None:
            result["quality_scores"] = self.qualityScores
        if self.chromatogram is not None:
            result["chromatogram"] = self.chromatogram
        return result


@dataclass
class TraceManifest:
    """Manifest containing metadata about stored trace chunks."""

    traceId: str
    originalFilename: str
    format: str
    totalBases: int
    chunkSize: int
    chunkCount: int
    hasChromatogram: bool
    hasQualityScores: bool
    createdAt: str
    qualityMetrics: Optional[dict] = None
    metadata: dict = field(default_factory=dict)
    chunks: list[ChunkMetadata] = field(default_factory=list)

    def to_dict(self) -> dict:
        """Serialize to dictionary."""
        return {
            "trace_id": self.traceId,
            "original_filename": self.originalFilename,
            "format": self.format,
            "total_bases": self.totalBases,
            "chunk_size": self.chunkSize,
            "chunk_count": self.chunkCount,
            "has_chromatogram": self.hasChromatogram,
            "has_quality_scores": self.hasQualityScores,
            "created_at": self.createdAt,
            "quality_metrics": self.qualityMetrics,
            "metadata": self.metadata,
            "chunks": [c.to_dict() for c in self.chunks],
        }


@dataclass
class ChunkedTraceData:
    """Complete chunked trace data ready for storage."""

    manifest: TraceManifest
    chunks: list[TraceChunk]

    def to_dict(self) -> dict:
        """Serialize to dictionary."""
        return {
            "manifest": self.manifest.to_dict(),
            "chunks": [c.to_dict() for c in self.chunks],
        }


def _chunk_chromatogram(
    chromatogram: ChromatogramData,
    start_base: int,
    end_base: int,
) -> Optional[dict]:
    """
    Extract chromatogram data for a specific base range.

    Uses peakLocations to map bases to chromatogram data points.
    """
    if not chromatogram.peakLocations:
        return None

    if start_base >= len(chromatogram.peakLocations):
        return None

    start_idx = (
        chromatogram.peakLocations[start_base]
        if start_base < len(chromatogram.peakLocations)
        else 0
    )
    end_idx = (
        chromatogram.peakLocations[min(end_base, len(chromatogram.peakLocations) - 1)] + 1
        if end_base < len(chromatogram.peakLocations)
        else len(chromatogram.traceA)
    )

    return {
        "a": chromatogram.traceA[start_idx:end_idx] if chromatogram.traceA else None,
        "c": chromatogram.traceC[start_idx:end_idx] if chromatogram.traceC else None,
        "g": chromatogram.traceG[start_idx:end_idx] if chromatogram.traceG else None,
        "t": chromatogram.traceT[start_idx:end_idx] if chromatogram.traceT else None,
        "peak_positions": [
            p - start_idx for p in chromatogram.peakLocations[start_base : end_base + 1]
        ],
    }


def chunk_trace_data(
    parsed: ParsedTrace,
    filename: str,
    chunk_size: int = DEFAULT_CHUNK_SIZE,
) -> ChunkedTraceData:
    """
    Split parsed trace data into chunks for storage.

    Args:
        parsed: The parsed trace data
        filename: Original filename
        chunk_size: Size of each chunk in bases (default: 10,000)

    Returns:
        ChunkedTraceData containing manifest and chunks
    """
    sequence = parsed.sequence.sequence
    quality = parsed.sequence.quality
    total_bases = len(sequence)

    chunk_count = (total_bases + chunk_size - 1) // chunk_size

    chunks: list[TraceChunk] = []
    chunk_metadata: list[ChunkMetadata] = []

    for i in range(chunk_count):
        start_pos = i * chunk_size
        end_pos = min(start_pos + chunk_size - 1, total_bases - 1)
        base_count = end_pos - start_pos + 1

        chunk_bases = sequence[start_pos : end_pos + 1]

        chunk_quality = None
        if quality:
            chunk_quality = quality[start_pos : end_pos + 1]

        chunk_chromatogram = None
        if parsed.chromatogram:
            chunk_chromatogram = _chunk_chromatogram(
                parsed.chromatogram,
                start_pos,
                end_pos,
            )

        chunk = TraceChunk(
            index=i,
            startPosition=start_pos,
            endPosition=end_pos,
            bases=chunk_bases,
            qualityScores=chunk_quality,
            chromatogram=chunk_chromatogram,
        )
        chunks.append(chunk)

        chunk_meta = ChunkMetadata(
            index=i,
            startPosition=start_pos,
            endPosition=end_pos,
            baseCount=base_count,
            filename=f"chunk_{i:04d}.json",
        )
        chunk_metadata.append(chunk_meta)

    manifest = TraceManifest(
        traceId=parsed.traceId,
        originalFilename=filename,
        format=parsed.format.value,
        totalBases=total_bases,
        chunkSize=chunk_size,
        chunkCount=chunk_count,
        hasChromatogram=parsed.chromatogram is not None,
        hasQualityScores=quality is not None,
        createdAt=datetime.now(timezone.utc).isoformat(),
        qualityMetrics=parsed.qualityMetrics.to_dict() if parsed.qualityMetrics else None,
        metadata=parsed.metadata,
        chunks=chunk_metadata,
    )

    return ChunkedTraceData(manifest=manifest, chunks=chunks)
