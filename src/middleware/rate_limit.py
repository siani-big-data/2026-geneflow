"""Rate limiting middleware."""

import time
from collections import defaultdict
from dataclasses import dataclass
from typing import Callable

import structlog
from fastapi import Request, Response
from starlette.middleware.base import BaseHTTPMiddleware
from starlette.responses import JSONResponse

logger = structlog.get_logger()


@dataclass
class RateLimit:
    """Rate limit configuration."""

    requests: int  # Max requests
    window: int  # Time window in seconds


class RateLimiter:
    """Token bucket rate limiter."""

    # Default limits per endpoint type
    DEFAULT_LIMITS = {
        "default": RateLimit(requests=100, window=60),  # 100/min
        "blast": RateLimit(requests=10, window=60),  # 10/min (external API)
        "copilot": RateLimit(requests=30, window=60),  # 30/min (LLM calls)
        "analysis": RateLimit(requests=50, window=60),  # 50/min
        "external": RateLimit(requests=20, window=60),  # 20/min (external APIs)
    }

    def __init__(self, redis_url: str | None = None):
        self.redis_url = redis_url
        self._redis = None
        self._local_buckets: dict[str, dict] = defaultdict(lambda: {"tokens": 0, "last_update": 0})

    async def connect(self) -> bool:
        """Connect to Redis for distributed rate limiting."""
        if not self.redis_url:
            return False

        try:
            import redis.asyncio as redis

            self._redis = redis.from_url(self.redis_url)
            await self._redis.ping()
            return True
        except Exception as e:
            logger.warning("rate_limiter_redis_failed", error=str(e))
            return False

    def _get_limit(self, endpoint: str) -> RateLimit:
        """Get rate limit for endpoint."""
        # Match endpoint to limit type
        if "/blast" in endpoint:
            return self.DEFAULT_LIMITS["blast"]
        elif "/copilot" in endpoint or "/agent" in endpoint:
            return self.DEFAULT_LIMITS["copilot"]
        elif "/analyze" in endpoint:
            return self.DEFAULT_LIMITS["analysis"]
        elif "/external" in endpoint:
            return self.DEFAULT_LIMITS["external"]
        return self.DEFAULT_LIMITS["default"]

    def _get_bucket_key(self, client_id: str, endpoint: str) -> str:
        """Create bucket key."""
        # Normalize endpoint
        path = endpoint.split("?")[0].rstrip("/")
        return f"ratelimit:{client_id}:{path}"

    async def is_allowed(self, client_id: str, endpoint: str) -> tuple[bool, dict]:
        """Check if request is allowed.

        Returns:
            Tuple of (allowed, info)
        """
        limit = self._get_limit(endpoint)
        bucket_key = self._get_bucket_key(client_id, endpoint)
        now = time.time()

        if self._redis:
            return await self._check_redis(bucket_key, limit, now)

        return self._check_local(bucket_key, limit, now)

    async def _check_redis(self, key: str, limit: RateLimit, now: float) -> tuple[bool, dict]:
        """Check rate limit using Redis."""
        try:
            pipe = self._redis.pipeline()

            # Get current count
            pipe.get(f"{key}:count")
            pipe.ttl(f"{key}:count")
            results = await pipe.execute()

            current_count = int(results[0] or 0)
            ttl = int(results[1] or 0)

            if current_count >= limit.requests:
                return False, {
                    "limit": limit.requests,
                    "remaining": 0,
                    "reset": ttl,
                }

            # Increment counter
            pipe = self._redis.pipeline()
            pipe.incr(f"{key}:count")
            if ttl <= 0:
                pipe.expire(f"{key}:count", limit.window)
            await pipe.execute()

            return True, {
                "limit": limit.requests,
                "remaining": limit.requests - current_count - 1,
                "reset": ttl if ttl > 0 else limit.window,
            }

        except Exception as e:
            logger.error("rate_limit_redis_error", error=str(e))
            return True, {"limit": limit.requests, "remaining": -1, "reset": 0}

    def _check_local(self, key: str, limit: RateLimit, now: float) -> tuple[bool, dict]:
        """Check rate limit using local memory."""
        bucket = self._local_buckets[key]

        # Refill tokens based on time passed
        time_passed = now - bucket["last_update"]
        tokens_to_add = time_passed * (limit.requests / limit.window)
        bucket["tokens"] = min(limit.requests, bucket["tokens"] + tokens_to_add)
        bucket["last_update"] = now

        if bucket["tokens"] >= 1:
            bucket["tokens"] -= 1
            return True, {
                "limit": limit.requests,
                "remaining": int(bucket["tokens"]),
                "reset": limit.window,
            }

        return False, {
            "limit": limit.requests,
            "remaining": 0,
            "reset": int(limit.window - time_passed) if time_passed < limit.window else 0,
        }


class RateLimitMiddleware(BaseHTTPMiddleware):
    """FastAPI middleware for rate limiting."""

    def __init__(self, app, limiter: RateLimiter):
        super().__init__(app)
        self.limiter = limiter

    async def dispatch(self, request: Request, call_next: Callable) -> Response:
        # Skip rate limiting for health endpoints
        if request.url.path in ["/health", "/ready", "/live"]:
            return await call_next(request)

        # Get client identifier
        client_id = self._get_client_id(request)

        # Check rate limit
        allowed, info = await self.limiter.is_allowed(client_id, request.url.path)

        if not allowed:
            logger.warning(
                "rate_limit_exceeded",
                client=client_id,
                path=request.url.path,
            )
            return JSONResponse(
                status_code=429,
                content={
                    "error": "Rate limit exceeded",
                    "limit": info["limit"],
                    "retryAfter": info["reset"],
                },
                headers={
                    "X-RateLimit-Limit": str(info["limit"]),
                    "X-RateLimit-Remaining": "0",
                    "X-RateLimit-Reset": str(info["reset"]),
                    "Retry-After": str(info["reset"]),
                },
            )

        # Process request
        response = await call_next(request)

        # Add rate limit headers
        response.headers["X-RateLimit-Limit"] = str(info["limit"])
        response.headers["X-RateLimit-Remaining"] = str(info["remaining"])
        response.headers["X-RateLimit-Reset"] = str(info["reset"])

        return response

    def _get_client_id(self, request: Request) -> str:
        """Get client identifier from request."""
        # Try API key first
        api_key = request.headers.get("X-API-Key", "")
        if api_key:
            return f"key:{api_key[:8]}"

        # Fall back to IP
        forwarded = request.headers.get("X-Forwarded-For", "")
        if forwarded:
            return f"ip:{forwarded.split(',')[0].strip()}"

        client = request.client
        if client:
            return f"ip:{client.host}"

        return "unknown"
