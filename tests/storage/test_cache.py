"""Tests for cache service."""

import pytest

from src.storage.cache import CacheService


class TestCacheService:
    """Tests for Redis/in-memory cache service."""

    @pytest.fixture
    def cache_service(self):
        return CacheService(redis_url="redis://localhost:6379")

    @pytest.mark.asyncio
    async def test_set_and_get(self, cache_service):
        await cache_service.set("test", "key1", {"data": "value"})
        result = await cache_service.get("test", "key1")

        assert result == {"data": "value"}

    @pytest.mark.asyncio
    async def test_get_nonexistent(self, cache_service):
        result = await cache_service.get("test", "nonexistent_key")
        assert result is None

    @pytest.mark.asyncio
    async def test_delete(self, cache_service):
        await cache_service.set("test", "delete_me", "value")
        await cache_service.delete("test", "delete_me")
        result = await cache_service.get("test", "delete_me")

        assert result is None

    @pytest.mark.asyncio
    async def test_namespace_isolation(self, cache_service):
        await cache_service.set("ns1", "key", "value1")
        await cache_service.set("ns2", "key", "value2")

        result1 = await cache_service.get("ns1", "key")
        result2 = await cache_service.get("ns2", "key")

        assert result1 == "value1"
        assert result2 == "value2"

    @pytest.mark.asyncio
    async def test_cache_blast_result(self, cache_service):
        blast_result = {"hits": [{"id": "seq1", "score": 100}]}
        await cache_service.cache_blast_result("ATGC", "blastn", "nt", blast_result)
        result = await cache_service.get_blast_result("ATGC", "blastn", "nt")

        assert result == blast_result

    @pytest.mark.asyncio
    async def test_cache_analysis_result(self, cache_service):
        analysis = {"quality_enhanced": 95, "trimmed": True}
        await cache_service.cache_analysis_result("trace123", "quality_enhanced", analysis)
        result = await cache_service.get_analysis_result("trace123", "quality_enhanced")

        assert result == analysis

    @pytest.mark.asyncio
    async def test_cache_llm_response(self, cache_service):
        await cache_service.cache_llm_response("prompt_hash", "This is a gene...")
        result = await cache_service.get_llm_response("prompt_hash")

        assert result == "This is a gene..."

    @pytest.mark.asyncio
    async def test_cache_external_api(self, cache_service):
        api_response = {"gene": "BRCA1", "location": "17q21"}
        await cache_service.cache_external_api("ensembl", "lookup", "BRCA1", api_response)
        result = await cache_service.get_external_api("ensembl", "lookup", "BRCA1")

        assert result == api_response

    def test_get_metrics(self, cache_service):
        metrics = cache_service.get_metrics()

        assert "hits" in metrics
        assert "misses" in metrics
        assert "connected" in metrics
        assert "hitRate" in metrics

    @pytest.mark.asyncio
    async def test_clear_namespace(self, cache_service):
        await cache_service.set("cleartest", "key1", "value1")
        await cache_service.set("cleartest", "key2", "value2")
        await cache_service.clear_namespace("cleartest")

        result1 = await cache_service.get("cleartest", "key1")
        result2 = await cache_service.get("cleartest", "key2")

        assert result1 is None
        assert result2 is None

    def test_make_key(self, cache_service):
        key = cache_service._make_key("blast", "abc123")
        assert "geneflow" in key
        assert "blast" in key
        assert "abc123" in key

    def test_hash_key(self, cache_service):
        hash1 = cache_service._hash_key("test_data")
        hash2 = cache_service._hash_key("test_data")
        hash3 = cache_service._hash_key("different_data")

        assert hash1 == hash2
        assert hash1 != hash3
        assert len(hash1) == 32
