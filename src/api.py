import json
from datetime import datetime
from typing import Optional

from fastapi import Depends, FastAPI, Header, HTTPException, Query
from pydantic import BaseModel

from src.config import Settings
from src.retry import RetryHandler
from src.storage import StorageProvider


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
    status: str
    storage_healthy: bool
    consumer_metrics: Optional[dict] = None


class CategoriesResponse(BaseModel):
    categories: list[str]


class CategoryStatsResponse(BaseModel):
    category: str
    event_count: int
    first_date: Optional[str]
    last_date: Optional[str]
    file_count: int


class CategoryDatesResponse(BaseModel):
    category: str
    dates: list[str]
    count: int


class EventsResponse(BaseModel):
    category: str
    events: list[dict]
    count: int
    date: Optional[str] = None
    start_date: Optional[str] = None
    end_date: Optional[str] = None


class ReplayResponse(BaseModel):
    category: str
    events: list[dict]
    count: int
    first_date: Optional[str] = None
    last_date: Optional[str] = None


class DLQResponse(BaseModel):
    events: list[dict]
    count: int
    date: Optional[str] = None


class DLQRetryResponse(BaseModel):
    success: bool
    event_id: Optional[str] = None
    message: str


class DLQRetryAllResponse(BaseModel):
    succeeded: int
    failed: int
    total: int


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
            description="Query API for the GeneFlow event store",
            version="1.0.0",
        )

        self._setup_routes()

    def set_consumer_metrics_callback(self, callback) -> None:
        """Connect consumer metrics callback."""
        self._consumer_metrics_callback = callback

    def _setup_routes(self) -> None:
        """Configure routes."""

        # === Health ===
        @self.app.get("/health", response_model=HealthResponse)
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
        @self.app.get("/categories", response_model=CategoriesResponse)
        async def list_categories(_: None = Depends(self._verify_api_key)):
            categories = await self.storage.list_categories()
            return CategoriesResponse(categories=categories)

        @self.app.get("/categories/{category}/stats", response_model=CategoryStatsResponse)
        async def get_category_stats(category: str, _: None = Depends(self._verify_api_key)):
            stats = await self.storage.get_stats(category)
            return CategoryStatsResponse(
                category=category,
                event_count=stats.get("event_count", 0),
                first_date=stats.get("first_date"),
                last_date=stats.get("last_date"),
                file_count=stats.get("file_count", 0),
            )

        @self.app.get("/categories/{category}/dates", response_model=CategoryDatesResponse)
        async def get_category_dates(category: str, _: None = Depends(self._verify_api_key)):
            dates = await self.storage.list_dates(category)
            return CategoryDatesResponse(
                category=category,
                dates=[d.strftime("%Y-%m-%d") for d in dates],
                count=len(dates),
            )

        # === Events ===
        @self.app.get("/events/{category}", response_model=EventsResponse)
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
        @self.app.get("/replay/{category}", response_model=ReplayResponse)
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
        @self.app.get("/dlq", response_model=DLQResponse)
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

        @self.app.get("/dlq/all", response_model=DLQResponse)
        async def get_all_dlq(_: None = Depends(self._verify_api_key)):
            events = await self.retry_handler.get_all_dlq_events()
            return DLQResponse(events=events, count=len(events))

        @self.app.post("/dlq/retry/{event_id}", response_model=DLQRetryResponse)
        async def retry_dlq_event(event_id: str, _: None = Depends(self._verify_api_key)):
            success = await self.retry_handler.replay_dlq_event(event_id)
            return DLQRetryResponse(
                success=success,
                event_id=event_id,
                message="Event replayed successfully" if success else "Failed to replay event",
            )

        @self.app.post("/dlq/retry-all", response_model=DLQRetryAllResponse)
        async def retry_all_dlq(
            date: Optional[str] = Query(None),
            _: None = Depends(self._verify_api_key),
        ):
            target = datetime.strptime(date, "%Y-%m-%d") if date else None
            result = await self.retry_handler.replay_all_dlq(target)
            return DLQRetryAllResponse(**result)
