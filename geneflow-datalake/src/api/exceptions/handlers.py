import structlog
from fastapi import FastAPI, Request
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse
from starlette.exceptions import HTTPException as StarletteHTTPException

from src.api.middleware.correlation import get_correlation_id

logger = structlog.get_logger(__name__)

HTTP_ERROR_CODES = {
    400: "bad_request",
    401: "unauthorized",
    403: "forbidden",
    404: "not_found",
    405: "method_not_allowed",
    409: "conflict",
    422: "validation_error",
    429: "too_many_requests",
    500: "internal_error",
    503: "service_unavailable",
}


def register_exception_handlers(app: FastAPI) -> None:
    """Register global exception handlers on the FastAPI app."""

    @app.exception_handler(StarletteHTTPException)
    async def http_exception_handler(request: Request, exc: StarletteHTTPException):
        correlation_id = get_correlation_id()
        return JSONResponse(
            status_code=exc.status_code,
            content={
                "error": HTTP_ERROR_CODES.get(exc.status_code, "error"),
                "message": str(exc.detail),
                "detail": None,
                "correlation_id": correlation_id,
            },
        )

    @app.exception_handler(RequestValidationError)
    async def validation_exception_handler(request: Request, exc: RequestValidationError):
        correlation_id = get_correlation_id()
        errors = exc.errors()
        first_error = errors[0] if errors else {}
        field = ".".join(str(loc) for loc in first_error.get("loc", []))
        msg = first_error.get("msg", "Validation error")

        return JSONResponse(
            status_code=422,
            content={
                "error": "validation_error",
                "message": f"Invalid value for '{field}': {msg}",
                "detail": errors,
                "correlation_id": correlation_id,
            },
        )

    @app.exception_handler(Exception)
    async def unhandled_exception_handler(request: Request, exc: Exception):
        correlation_id = get_correlation_id()
        logger.error(
            "unhandled_exception",
            correlation_id=correlation_id,
            error=str(exc),
            error_type=type(exc).__name__,
            path=request.url.path,
        )

        return JSONResponse(
            status_code=500,
            content={
                "error": "internal_error",
                "message": "An unexpected error occurred",
                "detail": None,
                "correlation_id": correlation_id,
            },
        )
