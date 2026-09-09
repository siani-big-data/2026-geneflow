"""Caching service using Redis.

Caches:
- BLAST results (TTL: 24h)
- Analysis results (TTL: 1h)
- LLM responses (TTL: 30min)
"""

import hashlib
import json
from datetime import timedelta
from typing import Any, Optional

import structlog

logger = structlog.get_logger()


class CacheService:
    """Redis-based caching service."""

    # Default TTLs
    TTL_BLAST = timedelta(hours=24)
    TTL_ANALYSIS = timedelta(hours=1)
    TTL_LLM = timedelta(minutes=30)
    TTL_EXTERNAL_API = timedelta(hours=6)

    def __init__(self, redis_url: str = "redis://localhost:6379"):
        self.redis_url = redis_url
        self._redis = None
        self._local_cache: dict[str, Any] = {}
        self._hits = 0
        self._misses = 0

    async def connect(self) -> bool:
        """Connect to Redis."""
        try:
            import redis.asyncio as redis

            self._redis = redis.from_url(self.redis_url)
            await self._redis.ping()
            logger.info("cache_connected", url=self.redis_url)
            return True
        except ImportError:
            logger.warning("redis not installed, using in-memory cache")
            return False
        except Exception as e:
            logger.warning("redis_connection_failed", error=str(e))
            return False

    async def disconnect(self) -> None:
        """Disconnect from Redis."""
        if self._redis:
            await self._redis.close()
            self._redis = None

    @property
    def is_connected(self) -> bool:
        """Check if connected to Redis."""
        return self._redis is not None

    def _make_key(self, namespace: str, key: str) -> str:
        """Create namespaced cache key."""
        return f"geneflow:ai:{namespace}:{key}"

    def _hash_key(self, data: str) -> str:
        """Create hash for cache key."""
        return hashlib.sha256(data.encode()).hexdigest()[:32]

    async def get(self, namespace: str, key: str) -> Optional[Any]:
        """Get value from cache.

        Args:
            namespace: Cache namespace (e.g., "blast", "analysis")
            key: Cache key

        Returns:
            Cached value or None
        """
        full_key = self._make_key(namespace, key)

        # Try Redis first
        if self._redis:
            try:
                value = await self._redis.get(full_key)
                if value:
                    self._hits += 1
                    return json.loads(value)
            except Exception as e:
                logger.error("cache_get_error", error=str(e))

        # Fallback to local cache
        if full_key in self._local_cache:
            self._hits += 1
            return self._local_cache[full_key]

        self._misses += 1
        return None

    async def set(
        self,
        namespace: str,
        key: str,
        value: Any,
        ttl: Optional[timedelta] = None,
    ) -> bool:
        """Set value in cache.

        Args:
            namespace: Cache namespace
            key: Cache key
            value: Value to cache (must be JSON-serializable)
            ttl: Time-to-live (default: 1 hour)

        Returns:
            True if successful
        """
        full_key = self._make_key(namespace, key)
        ttl = ttl or self.TTL_ANALYSIS

        # Store in Redis
        if self._redis:
            try:
                await self._redis.setex(
                    full_key,
                    int(ttl.total_seconds()),
                    json.dumps(value),
                )
                return True
            except Exception as e:
                logger.error("cache_set_error", error=str(e))

        # Fallback to local cache (no TTL)
        self._local_cache[full_key] = value
        return True

    async def delete(self, namespace: str, key: str) -> bool:
        """Delete value from cache."""
        full_key = self._make_key(namespace, key)

        if self._redis:
            try:
                await self._redis.delete(full_key)
            except Exception as e:
                logger.error("cache_delete_error", error=str(e))

        if full_key in self._local_cache:
            del self._local_cache[full_key]

        return True

    async def clear_namespace(self, namespace: str) -> int:
        """Clear all keys in a namespace."""
        pattern = self._make_key(namespace, "*")
        deleted = 0

        if self._redis:
            try:
                cursor = 0
                while True:
                    cursor, keys = await self._redis.scan(cursor, match=pattern, count=100)
                    if keys:
                        await self._redis.delete(*keys)
                        deleted += len(keys)
                    if cursor == 0:
                        break
            except Exception as e:
                logger.error("cache_clear_error", error=str(e))

        # Clear local cache
        prefix = self._make_key(namespace, "")
        local_keys = [k for k in self._local_cache if k.startswith(prefix)]
        for k in local_keys:
            del self._local_cache[k]
            deleted += 1

        return deleted

    # Convenience methods for specific cache types

    async def cache_blast_result(
        self,
        sequence: str,
        program: str,
        database: str,
        result: dict,
    ) -> bool:
        """Cache BLAST result."""
        key = self._hash_key(f"{sequence}:{program}:{database}")
        return await self.set("blast", key, result, self.TTL_BLAST)

    async def get_blast_result(
        self,
        sequence: str,
        program: str,
        database: str,
    ) -> Optional[dict]:
        """Get cached BLAST result."""
        key = self._hash_key(f"{sequence}:{program}:{database}")
        return await self.get("blast", key)

    async def cache_analysis_result(
        self,
        trace_id: str,
        analysis_type: str,
        result: dict,
    ) -> bool:
        """Cache analysis result."""
        key = f"{trace_id}:{analysis_type}"
        return await self.set("analysis", key, result, self.TTL_ANALYSIS)

    async def get_analysis_result(
        self,
        trace_id: str,
        analysis_type: str,
    ) -> Optional[dict]:
        """Get cached analysis result."""
        key = f"{trace_id}:{analysis_type}"
        return await self.get("analysis", key)

    async def cache_llm_response(
        self,
        prompt_hash: str,
        response: str,
    ) -> bool:
        """Cache LLM response."""
        return await self.set("llm", prompt_hash, {"response": response}, self.TTL_LLM)

    async def get_llm_response(self, prompt_hash: str) -> Optional[str]:
        """Get cached LLM response."""
        result = await self.get("llm", prompt_hash)
        return result.get("response") if result else None

    async def cache_external_api(
        self,
        api_name: str,
        endpoint: str,
        params: str,
        result: dict,
    ) -> bool:
        """Cache external API response."""
        key = self._hash_key(f"{api_name}:{endpoint}:{params}")
        return await self.set("external", key, result, self.TTL_EXTERNAL_API)

    async def get_external_api(
        self,
        api_name: str,
        endpoint: str,
        params: str,
    ) -> Optional[dict]:
        """Get cached external API response."""
        key = self._hash_key(f"{api_name}:{endpoint}:{params}")
        return await self.get("external", key)

    def get_metrics(self) -> dict:
        """Get cache metrics."""
        total = self._hits + self._misses
        hit_rate = self._hits / total if total > 0 else 0

        return {
            "connected": self.is_connected,
            "hits": self._hits,
            "misses": self._misses,
            "hitRate": round(hit_rate, 3),
            "localCacheSize": len(self._local_cache),
        }
