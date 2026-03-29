"""Tests for rate limiting middleware."""

import pytest

from src.middleware.rate_limit import RateLimit, RateLimiter


class TestRateLimiter:
    """Tests for token bucket rate limiter."""

    @pytest.fixture
    def rate_limiter(self):
        return RateLimiter(redis_url=None)  # In-memory mode

    @pytest.mark.asyncio
    async def test_allow_under_limit(self, rate_limiter):
        client_id = "test_client"
        endpoint = "/api/test"

        allowed, info = await rate_limiter.is_allowed(client_id, endpoint)

        assert allowed is True
        assert info["remaining"] >= 0
        assert "limit" in info
        assert "reset" in info

    @pytest.mark.asyncio
    async def test_rate_limit_exceeded(self, rate_limiter):
        client_id = "spam_client"
        endpoint = "/api/blast/search"  # blast has 10/min limit

        # Make many requests quickly
        allowed = True
        for _ in range(15):
            allowed, info = await rate_limiter.is_allowed(client_id, endpoint)
            if not allowed:
                break

        # Should eventually be rate limited
        assert allowed is False or info["remaining"] == 0

    @pytest.mark.asyncio
    async def test_different_endpoints_separate_limits(self, rate_limiter):
        client_id = "test_client"

        # Use up some of blast limit
        for _ in range(8):
            await rate_limiter.is_allowed(client_id, "/api/blast/search")

        # Different endpoint should still have full capacity
        allowed, info = await rate_limiter.is_allowed(client_id, "/api/other")
        assert allowed is True
        assert info["remaining"] > 0

    @pytest.mark.asyncio
    async def test_different_clients_separate_limits(self, rate_limiter):
        # Client 1 uses some requests
        for _ in range(5):
            await rate_limiter.is_allowed("client1", "/api/blast")

        # Client 2 should still have their full limit
        allowed, info = await rate_limiter.is_allowed("client2", "/api/blast")
        assert allowed is True

    @pytest.mark.asyncio
    async def test_rate_limit_info_structure(self, rate_limiter):
        allowed, info = await rate_limiter.is_allowed("test", "/api/test")

        assert "limit" in info
        assert "remaining" in info
        assert "reset" in info
        assert isinstance(info["limit"], int)
        assert isinstance(info["remaining"], int)

    def test_endpoint_limit_detection(self, rate_limiter):
        blast_limit = rate_limiter._get_limit("/api/blast/search")
        copilot_limit = rate_limiter._get_limit("/api/copilot/chat")
        default_limit = rate_limiter._get_limit("/api/other")

        assert blast_limit.requests == 10
        assert copilot_limit.requests == 30
        assert default_limit.requests == 100

    def test_bucket_key_generation(self, rate_limiter):
        key = rate_limiter._get_bucket_key("client1", "/api/test?foo=bar")

        assert "client1" in key
        assert "api/test" in key
        assert "foo" not in key  # Query params stripped


class TestRateLimit:
    """Tests for RateLimit dataclass."""

    def test_create_rate_limit(self):
        limit = RateLimit(requests=100, window=60)

        assert limit.requests == 100
        assert limit.window == 60
