"""Correlation ID middleware for request tracing."""

import uuid
from collections.abc import Callable

from starlette.middleware.base import BaseHTTPMiddleware
from starlette.requests import Request
from starlette.responses import Response

CORRELATION_ID_HEADER = "X-Correlation-ID"


class CorrelationIdMiddleware(BaseHTTPMiddleware):
    """Middleware that ensures every request has a correlation ID."""

    async def dispatch(self, request: Request, call_next: Callable) -> Response:
        """Process request and add correlation ID."""
        correlation_id = request.headers.get(CORRELATION_ID_HEADER)

        if not correlation_id:
            correlation_id = str(uuid.uuid4())

        request.state.correlation_id = correlation_id

        response = await call_next(request)

        response.headers[CORRELATION_ID_HEADER] = correlation_id

        return response
