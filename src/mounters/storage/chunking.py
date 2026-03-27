"""Chunking utilities for trace data storage."""

import json
from dataclasses import asdict, dataclass, field
from typing import Iterator

DEFAULT_CHUNK_SIZE = 1000


@dataclass
class ChunkMetadata:
    """Metadata for a single chunk."""

    index: int
    start_position: int
    end_position: int
    base_count: int
    filename: str


@dataclass
class TraceChunk:
    """A chunk of trace data."""

    index: int
    start_position: int
    end_position: int
    bases: str
    quality_scores: list[int] = field(default_factory=list)
    chromatogram: dict[str, list[int]] = field(default_factory=dict)

    def to_dict(self) -> dict:
        """Convert to dictionary, excluding empty optional fields."""
        data = {
            "index": self.index,
            "start_position": self.start_position,
            "end_position": self.end_position,
            "bases": self.bases,
        }
        if self.quality_scores:
            data["quality_scores"] = self.quality_scores
        if self.chromatogram:
            data["chromatogram"] = self.chromatogram
        return data

    def to_json(self) -> str:
        """Convert to JSON string."""
        return json.dumps(self.to_dict())


@dataclass
class TraceManifest:
    """Manifest for a stored trace."""

    trace_id: str
    original_filename: str
    format: str
    total_bases: int
    chunk_size: int
    chunk_count: int
    chunks: list[ChunkMetadata]
    has_chromatogram: bool = False
    has_quality_scores: bool = False
    created_at: str = ""

    def to_dict(self) -> dict:
        """Convert to dictionary."""
        return {
            "trace_id": self.trace_id,
            "original_filename": self.original_filename,
            "format": self.format,
            "total_bases": self.total_bases,
            "chunk_size": self.chunk_size,
            "chunk_count": self.chunk_count,
            "has_chromatogram": self.has_chromatogram,
            "has_quality_scores": self.has_quality_scores,
            "created_at": self.created_at,
            "chunks": [asdict(c) for c in self.chunks],
        }

    @classmethod
    def from_dict(cls, data: dict) -> "TraceManifest":
        """Create from dictionary."""
        chunks = [
            ChunkMetadata(
                index=c["index"],
                start_position=c["start_position"],
                end_position=c["end_position"],
                base_count=c["base_count"],
                filename=c["filename"],
            )
            for c in data.get("chunks", [])
        ]
        return cls(
            trace_id=data["trace_id"],
            original_filename=data["original_filename"],
            format=data["format"],
            total_bases=data["total_bases"],
            chunk_size=data["chunk_size"],
            chunk_count=data["chunk_count"],
            has_chromatogram=data.get("has_chromatogram", False),
            has_quality_scores=data.get("has_quality_scores", False),
            created_at=data.get("created_at", ""),
            chunks=chunks,
        )


class TraceChunker:
    """Chunker for splitting trace data into manageable pieces."""

    def __init__(self, chunk_size: int = DEFAULT_CHUNK_SIZE):
        self._chunk_size = chunk_size

    @property
    def chunk_size(self) -> int:
        """Get the chunk size."""
        return self._chunk_size

    def chunk_trace(
        self,
        trace_id: str,
        parsed_data: dict,
    ) -> tuple[TraceManifest, Iterator[TraceChunk]]:
        """
        Split trace data into chunks.

        Returns manifest and iterator of chunks.
        """
        sequence = parsed_data.get("sequence", "")
        quality_scores = parsed_data.get("quality_scores", [])
        chromatogram = parsed_data.get("chromatogram", {})

        total_bases = len(sequence)
        chunk_count = (
            (total_bases + self._chunk_size - 1) // self._chunk_size if total_bases > 0 else 0
        )

        chunk_metas = []
        for i in range(chunk_count):
            start = i * self._chunk_size
            end = min(start + self._chunk_size, total_bases)
            chunk_metas.append(
                ChunkMetadata(
                    index=i,
                    start_position=start,
                    end_position=end,
                    base_count=end - start,
                    filename=f"chunk_{i:04d}.json",
                )
            )

        manifest = TraceManifest(
            trace_id=trace_id,
            original_filename=parsed_data.get("filename", "unknown"),
            format=parsed_data.get("format", "unknown"),
            total_bases=total_bases,
            chunk_size=self._chunk_size,
            chunk_count=chunk_count,
            has_chromatogram=bool(chromatogram),
            has_quality_scores=bool(quality_scores),
            chunks=chunk_metas,
        )

        def generate_chunks() -> Iterator[TraceChunk]:
            for meta in chunk_metas:
                start = meta.start_position
                end = meta.end_position

                chunk_chromatogram = {}
                if chromatogram:
                    # Chromatogram typically has 10x more data points than bases
                    chrom_start = start * 10
                    chrom_end = end * 10
                    for channel in ["A", "C", "G", "T"]:
                        if channel in chromatogram:
                            chunk_chromatogram[channel] = chromatogram[channel][
                                chrom_start:chrom_end
                            ]

                yield TraceChunk(
                    index=meta.index,
                    start_position=start,
                    end_position=end,
                    bases=sequence[start:end],
                    quality_scores=quality_scores[start:end] if quality_scores else [],
                    chromatogram=chunk_chromatogram,
                )

        return manifest, generate_chunks()

    def reassemble_sequence(self, chunks: list[TraceChunk]) -> str:
        """Reassemble sequence from chunks."""
        sorted_chunks = sorted(chunks, key=lambda c: c.index)
        return "".join(c.bases for c in sorted_chunks)


def chunk_sequence(sequence: str, chunk_size: int) -> list[tuple[int, int, str]]:
    """
    Split a sequence into chunks.

    Returns list of (start_position, end_position, chunk_data).
    """
    chunks = []
    for i in range(0, len(sequence), chunk_size):
        end = min(i + chunk_size, len(sequence))
        chunks.append((i, end, sequence[i:end]))
    return chunks
