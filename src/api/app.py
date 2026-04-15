"""FastAPI application factory."""

from fastapi import FastAPI

from src.api.middleware import CorrelationIdMiddleware, RequestLoggingMiddleware
from src.api.routes import health_router


def create_app() -> FastAPI:
    """Create and configure the FastAPI application.

    Returns:
        Configured FastAPI application instance.
    """
    app = FastAPI(
        title="GeneFlow Analysis Worker",
        description="Bioinformatics analysis worker for GeneFlow platform",
        version="2.0.0",
        docs_url=None,  # Disable docs in production
        redoc_url=None,
    )

    # Add middleware (order matters - first added is outermost)
    app.add_middleware(RequestLoggingMiddleware)
    app.add_middleware(CorrelationIdMiddleware)

    # Include routers
    app.include_router(health_router)

    return app
