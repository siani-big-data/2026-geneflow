"""Tests for trace chunking."""

import json

from src.mounters.storage.chunking import (
    DEFAULT_CHUNK_SIZE,
    ChunkMetadata,
    TraceChunk,
    TraceChunker,
    TraceManifest,
)


class TestTraceChunker:
    """Tests for TraceChunker class."""

    def test_default_chunk_size(self):
        """Test default chunk size is 1000."""
        chunker = TraceChunker()
        assert chunker.chunk_size == DEFAULT_CHUNK_SIZE
        assert chunker.chunk_size == 1000

    def test_custom_chunk_size(self):
        """Test custom chunk size."""
        chunker = TraceChunker(chunk_size=500)
        assert chunker.chunk_size == 500

    def test_chunk_small_trace(self):
        """Test chunking a trace smaller than chunk size."""
        chunker = TraceChunker(chunk_size=1000)

        parsed_data = {
            "filename": "sample.ab1",
            "format": "AB1",
            "sequence": "ATCGATCG",  # 8 bases
            "quality_scores": [30, 35, 40, 38, 42, 30, 35, 40],
        }

        manifest, chunks = chunker.chunk_trace("trace-123", parsed_data)

        assert manifest.trace_id == "trace-123"
        assert manifest.total_bases == 8
        assert manifest.chunk_count == 1
        assert manifest.format == "AB1"
        assert manifest.has_quality_scores is True
        assert manifest.has_chromatogram is False

        chunk_list = list(chunks)
        assert len(chunk_list) == 1
        assert chunk_list[0].bases == "ATCGATCG"
        assert chunk_list[0].quality_scores == [30, 35, 40, 38, 42, 30, 35, 40]

    def test_chunk_large_trace(self):
        """Test chunking a trace larger than chunk size."""
        chunker = TraceChunker(chunk_size=100)

        sequence = "ATCG" * 75  # 300 bases
        quality_scores = [30] * 300

        parsed_data = {
            "filename": "large.fasta",
            "format": "FASTA",
            "sequence": sequence,
            "quality_scores": quality_scores,
        }

        manifest, chunks = chunker.chunk_trace("trace-456", parsed_data)

        assert manifest.total_bases == 300
        assert manifest.chunk_count == 3  # 300 / 100 = 3 chunks

        chunk_list = list(chunks)
        assert len(chunk_list) == 3

        # First chunk
        assert chunk_list[0].index == 0
        assert chunk_list[0].start_position == 0
        assert chunk_list[0].end_position == 100
        assert len(chunk_list[0].bases) == 100

        # Second chunk
        assert chunk_list[1].index == 1
        assert chunk_list[1].start_position == 100
        assert chunk_list[1].end_position == 200

        # Third chunk
        assert chunk_list[2].index == 2
        assert chunk_list[2].start_position == 200
        assert chunk_list[2].end_position == 300

    def test_chunk_with_chromatogram(self):
        """Test chunking with chromatogram data."""
        chunker = TraceChunker(chunk_size=10)

        parsed_data = {
            "filename": "sample.ab1",
            "format": "AB1",
            "sequence": "ATCGATCGATCG",  # 12 bases
            "chromatogram": {
                "A": list(range(120)),  # 10x more data points
                "C": list(range(120)),
                "G": list(range(120)),
                "T": list(range(120)),
            },
        }

        manifest, chunks = chunker.chunk_trace("trace-789", parsed_data)

        assert manifest.has_chromatogram is True
        assert manifest.chunk_count == 2  # 12 bases / 10 = 2 chunks

        chunk_list = list(chunks)
        assert len(chunk_list) == 2

        # Check chromatogram data is included
        assert "A" in chunk_list[0].chromatogram
        assert "C" in chunk_list[0].chromatogram
        assert "G" in chunk_list[0].chromatogram
        assert "T" in chunk_list[0].chromatogram

    def test_manifest_metadata(self):
        """Test manifest contains correct metadata."""
        chunker = TraceChunker(chunk_size=50)

        parsed_data = {
            "filename": "test_trace.ab1",
            "format": "AB1",
            "sequence": "A" * 120,
        }

        manifest, _ = chunker.chunk_trace("test-id", parsed_data)

        assert manifest.original_filename == "test_trace.ab1"
        assert manifest.format == "AB1"
        assert manifest.chunk_size == 50
        assert len(manifest.chunks) == 3  # 120 / 50 = 2.4 -> 3 chunks

        # Check chunk metadata
        assert manifest.chunks[0].filename == "chunk_0000.json"
        assert manifest.chunks[1].filename == "chunk_0001.json"
        assert manifest.chunks[2].filename == "chunk_0002.json"

    def test_reassemble_sequence(self):
        """Test reassembling sequence from chunks."""
        chunker = TraceChunker(chunk_size=5)
        original = "ATCGATCGATCGATCG"  # 16 bases

        parsed_data = {
            "filename": "test.fasta",
            "format": "FASTA",
            "sequence": original,
        }

        _, chunks = chunker.chunk_trace("test-id", parsed_data)
        chunk_list = list(chunks)

        reassembled = chunker.reassemble_sequence(chunk_list)
        assert reassembled == original

    def test_empty_sequence(self):
        """Test handling empty sequence."""
        chunker = TraceChunker(chunk_size=100)

        parsed_data = {
            "filename": "empty.fasta",
            "format": "FASTA",
            "sequence": "",
        }

        manifest, chunks = chunker.chunk_trace("empty-id", parsed_data)

        assert manifest.total_bases == 0
        assert manifest.chunk_count == 0
        assert list(chunks) == []


class TestTraceChunk:
    """Tests for TraceChunk dataclass."""

    def test_to_dict(self):
        """Test conversion to dict."""
        chunk = TraceChunk(
            index=0,
            start_position=0,
            end_position=10,
            bases="ATCGATCGAT",
            quality_scores=[30, 35, 40, 38, 42, 30, 35, 40, 38, 42],
        )

        data = chunk.to_dict()

        assert data["index"] == 0
        assert data["bases"] == "ATCGATCGAT"
        assert data["quality_scores"] == [30, 35, 40, 38, 42, 30, 35, 40, 38, 42]

    def test_to_json(self):
        """Test JSON serialization."""
        chunk = TraceChunk(
            index=1,
            start_position=10,
            end_position=20,
            bases="GCTAGCTAGT",
        )

        json_str = chunk.to_json()
        parsed = json.loads(json_str)

        assert parsed["index"] == 1
        assert parsed["bases"] == "GCTAGCTAGT"
        assert "quality_scores" not in parsed  # Not included if empty


class TestTraceManifest:
    """Tests for TraceManifest dataclass."""

    def test_to_dict(self):
        """Test conversion to dict."""
        manifest = TraceManifest(
            trace_id="test-123",
            original_filename="sample.ab1",
            format="AB1",
            total_bases=100,
            chunk_size=50,
            chunk_count=2,
            has_chromatogram=True,
            has_quality_scores=True,
            created_at="2026-03-26T10:00:00Z",
            chunks=[
                ChunkMetadata(0, 0, 50, 50, "chunk_0000.json"),
                ChunkMetadata(1, 50, 100, 50, "chunk_0001.json"),
            ],
        )

        data = manifest.to_dict()

        assert data["trace_id"] == "test-123"
        assert data["chunk_count"] == 2
        assert len(data["chunks"]) == 2
        assert data["chunks"][0]["filename"] == "chunk_0000.json"

    def test_from_dict(self):
        """Test creation from dict."""
        data = {
            "trace_id": "test-456",
            "original_filename": "sample.fasta",
            "format": "FASTA",
            "total_bases": 200,
            "chunk_size": 100,
            "chunk_count": 2,
            "has_chromatogram": False,
            "has_quality_scores": True,
            "created_at": "2026-03-26T12:00:00Z",
            "chunks": [
                {
                    "index": 0,
                    "start_position": 0,
                    "end_position": 100,
                    "base_count": 100,
                    "filename": "chunk_0000.json",
                },
                {
                    "index": 1,
                    "start_position": 100,
                    "end_position": 200,
                    "base_count": 100,
                    "filename": "chunk_0001.json",
                },
            ],
        }

        manifest = TraceManifest.from_dict(data)

        assert manifest.trace_id == "test-456"
        assert manifest.format == "FASTA"
        assert len(manifest.chunks) == 2
        assert manifest.chunks[1].start_position == 100

    def test_roundtrip(self):
        """Test dict -> manifest -> dict roundtrip."""
        original = TraceManifest(
            trace_id="roundtrip-test",
            original_filename="test.ab1",
            format="AB1",
            total_bases=50,
            chunk_size=50,
            chunk_count=1,
            chunks=[ChunkMetadata(0, 0, 50, 50, "chunk_0000.json")],
        )

        data = original.to_dict()
        restored = TraceManifest.from_dict(data)

        assert restored.trace_id == original.trace_id
        assert restored.total_bases == original.total_bases
        assert len(restored.chunks) == len(original.chunks)
