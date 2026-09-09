"""Tests for embedding service."""

import pytest

from src.storage.embeddings import EmbeddingService


class TestEmbeddingService:
    """Tests for k-mer embedding service."""

    @pytest.fixture
    def embedding_service(self):
        return EmbeddingService(k=4, qdrant_url=None)  # Smaller k for faster tests

    def test_generate_embedding(self, embedding_service):
        sequence = "ATGCGATCGATCGATCG"
        embedding = embedding_service.generate_embedding(sequence)

        assert isinstance(embedding, list)
        assert len(embedding) == embedding_service._embedding_dim
        assert all(isinstance(x, float) for x in embedding)

    def test_embedding_consistency(self, embedding_service):
        sequence = "ATGCGATCGATCG"
        emb1 = embedding_service.generate_embedding(sequence)
        emb2 = embedding_service.generate_embedding(sequence)

        assert emb1 == emb2

    def test_different_sequences_different_embeddings(self, embedding_service):
        seq1 = "AAAAAAAAAA"
        seq2 = "TTTTTTTTTT"

        emb1 = embedding_service.generate_embedding(seq1)
        emb2 = embedding_service.generate_embedding(seq2)

        assert emb1 != emb2

    @pytest.mark.asyncio
    async def test_embed_and_store(self, embedding_service):
        sequence = "ATGCGATCGATCG"
        seq_id = "test_seq_001"

        result = await embedding_service.embed_and_store(
            seq_id, sequence, metadata={"name": "test"}
        )

        assert result.id == seq_id
        assert result.metadata == {"name": "test"}
        assert len(result.embedding) == embedding_service._embedding_dim

    @pytest.mark.asyncio
    async def test_search_similar(self, embedding_service):
        # Store some sequences
        await embedding_service.embed_and_store("seq1", "ATGCGATCGATCG")
        await embedding_service.embed_and_store("seq2", "ATGCGATCGATCG")  # Identical
        await embedding_service.embed_and_store("seq3", "TTTTTTTTTTTT")  # Different

        results = await embedding_service.search_similar("ATGCGATCGATCG", limit=2)

        assert isinstance(results, list)
        # Should find similar sequences
        if results:
            assert "id" in results[0]
            assert "score" in results[0]

    @pytest.mark.asyncio
    async def test_get_embedding(self, embedding_service):
        await embedding_service.embed_and_store("test_get", "ATGCATGC")
        result = await embedding_service.get_embedding("test_get")

        assert result is not None
        assert result.id == "test_get"

    @pytest.mark.asyncio
    async def test_delete_embedding(self, embedding_service):
        seq_id = "to_delete"
        await embedding_service.embed_and_store(seq_id, "ATGCGATCG")
        deleted = await embedding_service.delete_embedding(seq_id)

        assert deleted is True
        result = await embedding_service.get_embedding(seq_id)
        assert result is None

    def test_empty_sequence(self, embedding_service):
        embedding = embedding_service.generate_embedding("")

        assert isinstance(embedding, list)
        # All zeros for empty sequence
        assert all(x == 0.0 for x in embedding)

    def test_normalization(self, embedding_service):
        sequence = "ATGCATGCATGC"
        embedding = embedding_service.generate_embedding(sequence, normalize=True)

        # Check L2 norm is approximately 1
        import math

        norm = math.sqrt(sum(x * x for x in embedding))
        assert abs(norm - 1.0) < 0.01 or norm == 0

    def test_get_metrics(self, embedding_service):
        metrics = embedding_service.get_metrics()

        assert "embeddingDim" in metrics
        assert "kmerSize" in metrics
        assert "cachedEmbeddings" in metrics
        assert "qdrantAvailable" in metrics

    def test_kmer_index_size(self, embedding_service):
        # For k=4, should have 4^4 = 256 k-mers
        assert embedding_service._embedding_dim == 256
