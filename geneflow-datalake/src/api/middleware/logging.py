import time

import structlog
from starlette.middleware.base import BaseHTTPMiddleware
from starlette.requests import Request
from starlette.responses import Response

from src.api.middleware.correlation import get_correlation_id

logger = structlog.get_logger(__name__)


class RequestLoggingMiddleware(BaseHTTPMiddleware):
    """Middleware that logs request/response information."""

    def __init__(self, app, log_requests: bool = True, skip_paths: set[str] | None = None):
        super().__init__(app)
        self.log_requests = log_requests
        self.skip_paths = skip_paths or {"/health"}

    async def dispatch(self, request: Request, call_next) -> Response:
        if request.url.path in self.skip_paths:
            return await call_next(request)

        correlation_id = get_correlation_id()
        start_time = time.perf_counter()

        if self.log_requests:
            logger.info(
                "request_started",
                correlation_id=correlation_id,
                method=request.method,
                path=request.url.path,
                query=str(request.query_params) if request.query_params else None,
            )

        try:
            response = await call_next(request)
        except Exception as exc:
            duration_ms = (time.perf_counter() - start_time) * 1000
            logger.error(
                "request_failed",
                correlation_id=correlation_id,
                method=request.method,
                path=request.url.path,
                duration_ms=round(duration_ms, 2),
                error=str(exc),
            )
            raise

        duration_ms = (time.perf_counter() - start_time) * 1000

        if self.log_requests:
            log_method = logger.info if response.status_code < 400 else logger.warning
            log_method(
                "request_completed",
                correlation_id=correlation_id,
                method=request.method,
                path=request.url.path,
                status_code=response.status_code,
                duration_ms=round(duration_ms, 2),
            )

        return response
