"""Additional tests for chunking utilities to maximize coverage."""

import json

from src.mounters.storage.chunking import (
    ChunkMetadata,
    TraceChunk,
    TraceChunker,
    TraceManifest,
    chunk_sequence,
)


class TestChunkSequence:
    def test_chunk_sequence_empty(self):
        assert chunk_sequence("", 10) == []

    def test_chunk_sequence_smaller_than_chunk(self):
        result = chunk_sequence("ATC", 10)
        assert result == [(0, 3, "ATC")]

    def test_chunk_sequence_exact_multiple(self):
        result = chunk_sequence("ATCGATCG", 4)
        assert result == [(0, 4, "ATCG"), (4, 8, "ATCG")]

    def test_chunk_sequence_remainder(self):
        result = chunk_sequence("ATCGATCGAT", 4)
        assert result == [(0, 4, "ATCG"), (4, 8, "ATCG"), (8, 10, "AT")]


class TestTraceChunkExtra:
    def test_to_dict_with_quality_only(self):
        chunk = TraceChunk(
            index=0,
            start_position=0,
            end_position=3,
            bases="ATC",
            quality_scores=[10, 20, 30],
        )
        data = chunk.to_dict()
        assert data["quality_scores"] == [10, 20, 30]
        assert "chromatogram" not in data

    def test_to_dict_with_chromatogram_only(self):
        chunk = TraceChunk(
            index=0,
            start_position=0,
            end_position=3,
            bases="ATC",
            chromatogram={"A": [1, 2, 3]},
        )
        data = chunk.to_dict()
        assert data["chromatogram"] == {"A": [1, 2, 3]}
        assert "quality_scores" not in data

    def test_to_dict_full(self):
        chunk = TraceChunk(
            index=2,
            start_position=10,
            end_position=15,
            bases="GCTAA",
            quality_scores=[10, 20, 30, 40, 50],
            chromatogram={"A": [1]},
        )
        data = chunk.to_dict()
        assert data["index"] == 2
        assert data["bases"] == "GCTAA"
        assert "quality_scores" in data
        assert "chromatogram" in data

    def test_to_json_minimal(self):
        chunk = TraceChunk(index=0, start_position=0, end_position=1, bases="A")
        parsed = json.loads(chunk.to_json())
        assert parsed["bases"] == "A"
        assert "quality_scores" not in parsed


class TestTraceManifestExtra:
    def test_from_dict_no_chunks(self):
        data = {
            "trace_id": "t",
            "original_filename": "f",
            "format": "AB1",
            "total_bases": 0,
            "chunk_size": 10,
            "chunk_count": 0,
        }
        m = TraceManifest.from_dict(data)
        assert m.trace_id == "t"
        assert m.chunks == []
        # defaults
        assert m.has_chromatogram is False
        assert m.has_quality_scores is False
        assert m.created_at == ""

    def test_from_dict_with_optional_fields(self):
        data = {
            "trace_id": "t",
            "original_filename": "f",
            "format": "F",
            "total_bases": 1,
            "chunk_size": 1,
            "chunk_count": 1,
            "has_chromatogram": True,
            "has_quality_scores": True,
            "created_at": "2026-01-01T00:00:00Z",
            "chunks": [
                {
                    "index": 0,
                    "start_position": 0,
                    "end_position": 1,
                    "base_count": 1,
                    "filename": "chunk_0000.json",
                }
            ],
        }
        m = TraceManifest.from_dict(data)
        assert m.has_chromatogram is True
        assert m.has_quality_scores is True
        assert m.created_at.endswith("Z")
        assert len(m.chunks) == 1


class TestTraceChunkerEdge:
    def test_chunk_with_chromatogram_slices_correctly(self):
        chunker = TraceChunker(chunk_size=5)
        parsed = {
            "filename": "x.ab1",
            "format": "AB1",
            "sequence": "ATCGATCG",  # 8 bases
            "chromatogram": {
                "A": list(range(80)),  # 10x = 80
                "C": list(range(80)),
                "G": list(range(80)),
                "T": list(range(80)),
            },
        }
        manifest, chunks = chunker.chunk_trace("t", parsed)
        chunk_list = list(chunks)
        # 8 bases / 5 -> 2 chunks (sizes 5 and 3)
        assert len(chunk_list) == 2
        # first chunk: positions 0-5 -> chromatogram 0-50
        assert len(chunk_list[0].chromatogram["A"]) == 50
        # second chunk: positions 5-8 -> chromatogram 50-80 = 30 items
        assert len(chunk_list[1].chromatogram["A"]) == 30

    def test_chunk_with_partial_chromatogram_channels(self):
        """Only A channel present, others should be skipped."""
        chunker = TraceChunker(chunk_size=5)
        parsed = {
            "filename": "x.ab1",
            "format": "AB1",
            "sequence": "ATCG",
            "chromatogram": {"A": list(range(40))},
        }
        manifest, chunks = chunker.chunk_trace("t", parsed)
        chunk_list = list(chunks)
        assert "A" in chunk_list[0].chromatogram
        assert "C" not in chunk_list[0].chromatogram

    def test_reassemble_out_of_order(self):
        chunker = TraceChunker(chunk_size=2)
        c1 = TraceChunk(index=1, start_position=2, end_position=4, bases="CD")
        c0 = TraceChunk(index=0, start_position=0, end_position=2, bases="AB")
        c2 = TraceChunk(index=2, start_position=4, end_position=6, bases="EF")
        result = chunker.reassemble_sequence([c1, c0, c2])
        assert result == "ABCDEF"

    def test_chunker_with_only_sequence_no_quality(self):
        chunker = TraceChunker(chunk_size=3)
        parsed = {"filename": "f", "format": "F", "sequence": "ATCGAT"}
        manifest, chunks = chunker.chunk_trace("t", parsed)
        assert manifest.has_quality_scores is False
        chunk_list = list(chunks)
        # quality_scores in chunks is empty list
        assert chunk_list[0].quality_scores == []


class TestChunkMetadataDataclass:
    def test_chunk_metadata_creation(self):
        cm = ChunkMetadata(
            index=0,
            start_position=0,
            end_position=100,
            base_count=100,
            filename="chunk_0000.json",
        )
        assert cm.index == 0
        assert cm.filename == "chunk_0000.json"
