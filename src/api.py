import json
from datetime import datetime
from typing import Optional

from fastapi import Depends, FastAPI, Header, HTTPException, Query
from fastapi.openapi.utils import get_openapi
from pydantic import BaseModel, Field

from src.config import Settings
from src.models import EventCategory
from src.retry import RetryHandler
from src.storage import StorageProvider


# === OpenAPI Tags ===
TAGS_METADATA = [
    {
        "name": "Health",
        "description": "Service health and metrics endpoints.",
    },
    {
        "name": "Categories",
        "description": "List and inspect event categories.",
    },
    {
        "name": "Events",
        "description": "Query events by category, date, and filters.",
    },
    {
        "name": "Replay",
        "description": "Replay events for system reconstruction.",
    },
    {
        "name": "DLQ",
        "description": "Dead Letter Queue management for failed events.",
    },
]


def create_api_key_dependency(settings: Settings):
    """Create API key validation dependency."""

    async def verify_api_key(x_api_key: Optional[str] = Header(None)) -> None:
        # If no API key configured, skip validation
        if not settings.api_key:
            return

        if x_api_key != settings.api_key:
            raise HTTPException(status_code=401, detail="Invalid or missing API key")

    return verify_api_key


# === Response Models ===


class HealthResponse(BaseModel):
    """Service health status."""

    status: str = Field(..., description="Service status", examples=["healthy", "degraded"])
    storage_healthy: bool = Field(..., description="Storage provider health")
    consumer_metrics: Optional[dict] = Field(
        None, description="Redis consumer metrics (events processed, etc.)"
    )

    model_config = {"json_schema_extra": {"example": {
        "status": "healthy",
        "storage_healthy": True,
        "consumer_metrics": {"events_processed": 1234, "events_per_second": 45.2}
    }}}


class CategoriesResponse(BaseModel):
    """List of available categories."""

    categories: list[str] = Field(..., description="Event category names")

    model_config = {"json_schema_extra": {"example": {
        "categories": ["users", "traces", "studies", "alignments"]
    }}}


class AvailableCategoriesResponse(BaseModel):
    """All valid event categories."""

    categories: list[str] = Field(..., description="Valid category names from enum")
    count: int = Field(..., description="Number of categories")

    model_config = {"json_schema_extra": {"example": {
        "categories": ["users", "studies", "traces", "alignments",
                       "subscriptions", "plans", "ai", "blast", "system"],
        "count": 9
    }}}


class CategoryStatsResponse(BaseModel):
    """Statistics for a category."""

    category: str = Field(..., description="Category name")
    event_count: int = Field(..., description="Total number of events")
    first_date: Optional[str] = Field(None, description="First event date (YYYY-MM-DD)")
    last_date: Optional[str] = Field(None, description="Last event date (YYYY-MM-DD)")
    file_count: int = Field(..., description="Number of JSONL files")

    model_config = {"json_schema_extra": {"example": {
        "category": "users",
        "event_count": 15420,
        "first_date": "2026-01-01",
        "last_date": "2026-03-25",
        "file_count": 84
    }}}


class CategoryDatesResponse(BaseModel):
    """Available dates for a category."""

    category: str = Field(..., description="Category name")
    dates: list[str] = Field(..., description="Available dates (YYYY-MM-DD)")
    count: int = Field(..., description="Number of dates")

    model_config = {"json_schema_extra": {"example": {
        "category": "users",
        "dates": ["2026-03-23", "2026-03-24", "2026-03-25"],
        "count": 3
    }}}


class EventsResponse(BaseModel):
    """Query results for events."""

    category: str = Field(..., description="Category name")
    events: list[dict] = Field(..., description="List of events")
    count: int = Field(..., description="Total count (before pagination)")
    date: Optional[str] = Field(None, description="Queried date")
    start_date: Optional[str] = Field(None, description="Range start date")
    end_date: Optional[str] = Field(None, description="Range end date")

    model_config = {"json_schema_extra": {"example": {
        "category": "users",
        "events": [
            {
                "eventId": "550e8400-e29b-41d4-a716-446655440000",
                "type": "UserRegistered",
                "timestamp": "2026-03-25T10:30:00.000Z",
                "data": {"userId": "user-123", "email": "scientist@lab.org"}
            }
        ],
        "count": 1,
        "date": "2026-03-25"
    }}}


class ReplayResponse(BaseModel):
    """Replay results for system reconstruction."""

    category: str = Field(..., description="Category name")
    events: list[dict] = Field(..., description="Events sorted chronologically")
    count: int = Field(..., description="Total event count")
    first_date: Optional[str] = Field(None, description="First event date")
    last_date: Optional[str] = Field(None, description="Last event date")


class DLQResponse(BaseModel):
    """Dead Letter Queue contents."""

    events: list[dict] = Field(..., description="Failed events in DLQ")
    count: int = Field(..., description="Number of failed events")
    date: Optional[str] = Field(None, description="Queried date")

    model_config = {"json_schema_extra": {"example": {
        "events": [
            {
                "eventId": "failed-event-123",
                "category": "users",
                "lastError": "Storage timeout",
                "retryCount": 5,
                "movedToDlqAt": "2026-03-25T10:35:00.000Z"
            }
        ],
        "count": 1,
        "date": "2026-03-25"
    }}}


class DLQRetryResponse(BaseModel):
    """Result of retrying a DLQ event."""

    success: bool = Field(..., description="Whether retry succeeded")
    event_id: Optional[str] = Field(None, description="Event ID")
    message: str = Field(..., description="Result message")


class DLQRetryAllResponse(BaseModel):
    """Result of retrying all DLQ events."""

    succeeded: int = Field(..., description="Number of successful retries")
    failed: int = Field(..., description="Number of failed retries")
    total: int = Field(..., description="Total events processed")


# === API Class ===


class DatalakeAPI:
    """REST API for the Datalake."""

    def __init__(
        self,
        storage: StorageProvider,
        settings: Settings,
        retry_handler: RetryHandler,
    ):
        self.storage = storage
        self.settings = settings
        self.retry_handler = retry_handler
        self._consumer_metrics_callback = None
        self._verify_api_key = create_api_key_dependency(settings)

        self.app = FastAPI(
            title="GeneFlow Datalake API",
            description="""
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
            """,
            version="1.0.0",
            openapi_tags=TAGS_METADATA,
            contact={
                "name": "GeneFlow Team",
            },
            license_info={
                "name": "Proprietary",
            },
        )

        self._setup_routes()

    def set_consumer_metrics_callback(self, callback) -> None:
        """Connect consumer metrics callback."""
        self._consumer_metrics_callback = callback

    def _setup_routes(self) -> None:
        """Configure routes."""

        # === Health ===
        @self.app.get(
            "/health",
            response_model=HealthResponse,
            tags=["Health"],
            summary="Service health check",
            description="Returns service status, storage health, and consumer metrics.",
        )
        async def health():
            storage_healthy = await self.storage.health_check()
            consumer_metrics = None
            if self._consumer_metrics_callback:
                consumer_metrics = self._consumer_metrics_callback()

            return HealthResponse(
                status="healthy" if storage_healthy else "degraded",
                storage_healthy=storage_healthy,
                consumer_metrics=consumer_metrics,
            )

        # === Categories ===
        @self.app.get(
            "/categories",
            response_model=CategoriesResponse,
            tags=["Categories"],
            summary="List categories with data",
            description="Returns event categories that have stored data.",
        )
        async def list_categories(_: None = Depends(self._verify_api_key)):
            categories = await self.storage.list_categories()
            return CategoriesResponse(categories=categories)

        @self.app.get(
            "/categories/available",
            response_model=AvailableCategoriesResponse,
            tags=["Categories"],
            summary="List all valid categories",
            description="Returns all valid event categories defined in the system.",
        )
        async def list_available_categories(_: None = Depends(self._verify_api_key)):
            categories = [c.value for c in EventCategory]
            return AvailableCategoriesResponse(categories=categories, count=len(categories))

        @self.app.get(
            "/categories/{category}/stats",
            response_model=CategoryStatsResponse,
            tags=["Categories"],
            summary="Get category statistics",
            description="Returns event count, date range, and file count for a category.",
        )
        async def get_category_stats(category: str, _: None = Depends(self._verify_api_key)):
            stats = await self.storage.get_stats(category)
            return CategoryStatsResponse(
                category=category,
                event_count=stats.get("event_count", 0),
                first_date=stats.get("first_date"),
                last_date=stats.get("last_date"),
                file_count=stats.get("file_count", 0),
            )

        @self.app.get(
            "/categories/{category}/dates",
            response_model=CategoryDatesResponse,
            tags=["Categories"],
            summary="List available dates",
            description="Returns all dates with events for a category.",
        )
        async def get_category_dates(category: str, _: None = Depends(self._verify_api_key)):
            dates = await self.storage.list_dates(category)
            return CategoryDatesResponse(
                category=category,
                dates=[d.strftime("%Y-%m-%d") for d in dates],
                count=len(dates),
            )

        # === Events ===
        @self.app.get(
            "/events/{category}",
            response_model=EventsResponse,
            tags=["Events"],
            summary="Query events",
            description="Query events by category with optional date range, type filter, and pagination.",
        )
        async def get_events(
            category: str,
            date: Optional[str] = Query(None, description="Date YYYY-MM-DD"),
            start_date: Optional[str] = Query(None),
            end_date: Optional[str] = Query(None),
            event_type: Optional[str] = Query(None),
            limit: int = Query(1000, ge=1, le=10000),
            offset: int = Query(0, ge=0),
            _: None = Depends(self._verify_api_key),
        ):
            try:
                if date:
                    target = datetime.strptime(date, "%Y-%m-%d")
                    lines = await self.storage.read_events(category, target)
                elif start_date and end_date:
                    start = datetime.strptime(start_date, "%Y-%m-%d")
                    end = datetime.strptime(end_date, "%Y-%m-%d")
                    lines = await self.storage.read_events_range(category, start, end)
                else:
                    target = datetime.utcnow()
                    lines = await self.storage.read_events(category, target)
                    date = target.strftime("%Y-%m-%d")

                # Parse and filter
                events = []
                for line in lines:
                    try:
                        event = json.loads(line)
                        if event_type and event.get("type") != event_type:
                            continue
                        events.append(event)
                    except json.JSONDecodeError:
                        continue

                # Paginate
                total = len(events)
                events = events[offset : offset + limit]

                return EventsResponse(
                    category=category,
                    events=events,
                    count=total,
                    date=date,
                    start_date=start_date,
                    end_date=end_date,
                )

            except ValueError as e:
                raise HTTPException(400, f"Invalid date format: {e}")

        # === Replay ===
        @self.app.get(
            "/replay/{category}",
            response_model=ReplayResponse,
            tags=["Replay"],
            summary="Replay events",
            description="Get all events for a category, sorted chronologically. Use for system reconstruction.",
        )
        async def replay(
            category: str,
            from_date: Optional[str] = Query(None, alias="from"),
            _: None = Depends(self._verify_api_key),
        ):
            dates = await self.storage.list_dates(category)

            if not dates:
                return ReplayResponse(category=category, events=[], count=0)

            if from_date:
                start = datetime.strptime(from_date, "%Y-%m-%d")
                dates = [d for d in dates if d >= start]

            if not dates:
                return ReplayResponse(category=category, events=[], count=0)

            lines = await self.storage.read_events_range(category, dates[0], dates[-1])

            events = []
            for line in lines:
                try:
                    events.append(json.loads(line))
                except json.JSONDecodeError:
                    continue

            # Sort by timestamp
            events.sort(key=lambda e: e.get("timestamp", ""))

            return ReplayResponse(
                category=category,
                events=events,
                count=len(events),
                first_date=dates[0].strftime("%Y-%m-%d"),
                last_date=dates[-1].strftime("%Y-%m-%d"),
            )

        # === DLQ ===
        @self.app.get(
            "/dlq",
            response_model=DLQResponse,
            tags=["DLQ"],
            summary="Get failed events",
            description="Get events from the Dead Letter Queue for a specific date (default: today).",
        )
        async def get_dlq(
            date: Optional[str] = Query(None, description="Date YYYY-MM-DD, default today"),
            _: None = Depends(self._verify_api_key),
        ):
            if date:
                target = datetime.strptime(date, "%Y-%m-%d")
                events = await self.retry_handler.get_dlq_events(target)
            else:
                events = await self.retry_handler.get_dlq_events()

            return DLQResponse(
                events=events,
                count=len(events),
                date=date,
            )

        @self.app.get(
            "/dlq/all",
            response_model=DLQResponse,
            tags=["DLQ"],
            summary="Get all failed events",
            description="Get all events from the Dead Letter Queue across all dates.",
        )
        async def get_all_dlq(_: None = Depends(self._verify_api_key)):
            events = await self.retry_handler.get_all_dlq_events()
            return DLQResponse(events=events, count=len(events))

        @self.app.post(
            "/dlq/retry/{event_id}",
            response_model=DLQRetryResponse,
            tags=["DLQ"],
            summary="Retry a failed event",
            description="Attempt to reprocess a specific event from the DLQ.",
        )
        async def retry_dlq_event(event_id: str, _: None = Depends(self._verify_api_key)):
            success = await self.retry_handler.replay_dlq_event(event_id)
            return DLQRetryResponse(
                success=success,
                event_id=event_id,
                message="Event replayed successfully" if success else "Failed to replay event",
            )

        @self.app.post(
            "/dlq/retry-all",
            response_model=DLQRetryAllResponse,
            tags=["DLQ"],
            summary="Retry all failed events",
            description="Attempt to reprocess all events in the DLQ.",
        )
        async def retry_all_dlq(
            date: Optional[str] = Query(None, description="Filter by date (YYYY-MM-DD)"),
            _: None = Depends(self._verify_api_key),
        ):
            target = datetime.strptime(date, "%Y-%m-%d") if date else None
            result = await self.retry_handler.replay_all_dlq(target)
            return DLQRetryAllResponse(**result)
