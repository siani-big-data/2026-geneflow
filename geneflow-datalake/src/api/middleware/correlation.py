import uuid
from contextvars import ContextVar

from starlette.middleware.base import BaseHTTPMiddleware
from starlette.requests import Request
from starlette.responses import Response

correlation_id_ctx: ContextVar[str] = ContextVar("correlation_id", default="")


class CorrelationIdMiddleware(BaseHTTPMiddleware):
    """Middleware that manages correlation IDs for request tracing."""

    async def dispatch(self, request: Request, call_next) -> Response:
        correlation_id = request.headers.get("X-Correlation-ID") or str(uuid.uuid4())
        correlation_id_ctx.set(correlation_id)

        response = await call_next(request)
        response.headers["X-Correlation-ID"] = correlation_id

        return response


def get_correlation_id() -> str:
    """Get the current correlation ID."""
    return correlation_id_ctx.get() or "unknown"
