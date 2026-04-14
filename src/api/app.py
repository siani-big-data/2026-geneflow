"""FastAPI application factory."""

from typing import Callable

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from src.api.auth import get_api_key_dependency
from src.api.exceptions import register_exception_handlers
from src.api.middleware import CorrelationIdMiddleware, RequestLoggingMiddleware
from src.api.routes.categories import router as categories_router, setup_categories_routes
from src.api.routes.dlq import router as dlq_router, setup_dlq_routes
from src.api.routes.events import router as events_router, setup_events_routes
from src.api.routes.health import router as health_router, setup_health_routes
from src.api.routes.replay import router as replay_router, setup_replay_routes
from src.api.services import CategoryStatsService, DLQService, EventsQueryService
from src.config import Settings
from src.retry import RetryHandler
from src.storage import StorageProvider

TAGS_METADATA = [
    {"name": "Health", "description": "Service health and metrics endpoints."},
    {"name": "Categories", "description": "List and inspect event categories."},
    {"name": "Events", "description": "Query events by category, date, and filters."},
    {"name": "Replay", "description": "Replay events for system reconstruction."},
    {"name": "DLQ", "description": "Dead Letter Queue management for failed events."},
]

API_DESCRIPTION = """
## Event Store API

The GeneFlow Datalake is the **immutable source of truth** for the GeneFlow platform.
It consumes all events from Redis Streams and persists them in JSONL format.

### Features

- **Query events** by category, date, and type
- **Replay events** for system reconstruction
- **Inspect DLQ** for failed events
- **Health monitoring** with consumer metrics

### Authentication

Protected endpoints require an `X-API-Key` header.
"""


def create_app(
    storage: StorageProvider,
    settings: Settings,
    retry_handler: RetryHandler,
    get_consumer_metrics: Callable[[], dict | None] = lambda: None,
) -> FastAPI:
    """Create and configure the FastAPI application."""

    app = FastAPI(
        title="GeneFlow Datalake API",
        description=API_DESCRIPTION,
        version="1.0.0",
        openapi_tags=TAGS_METADATA,
        contact={"name": "GeneFlow Team"},
        license_info={"name": "Proprietary"},
    )

    _setup_middleware(app, settings)
    register_exception_handlers(app)

    verify_api_key = get_api_key_dependency(settings)

    events_service = EventsQueryService(storage)
    category_service = CategoryStatsService(storage)
    dlq_service = DLQService(retry_handler)

    setup_health_routes(health_router, storage, get_consumer_metrics)
    setup_categories_routes(categories_router, category_service, verify_api_key)
    setup_events_routes(events_router, events_service, verify_api_key)
    setup_replay_routes(replay_router, storage, verify_api_key)
    setup_dlq_routes(dlq_router, dlq_service, verify_api_key)

    app.include_router(health_router)
    app.include_router(categories_router)
    app.include_router(events_router)
    app.include_router(replay_router)
    app.include_router(dlq_router)

    return app


def _setup_middleware(app: FastAPI, settings: Settings) -> None:
    """Configure application middleware."""
    app.add_middleware(
        CORSMiddleware,
        allow_origins=settings.cors_origins,
        allow_credentials=settings.cors_allow_credentials,
        allow_methods=settings.cors_allow_methods,
        allow_headers=settings.cors_allow_headers,
    )
    app.add_middleware(RequestLoggingMiddleware, log_requests=settings.log_requests)
    app.add_middleware(CorrelationIdMiddleware)
