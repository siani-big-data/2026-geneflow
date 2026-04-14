import json
from datetime import datetime
from typing import Callable, Optional

from fastapi import APIRouter, Depends, Query

from src.api.responses import ReplayResponse
from src.constants import DATE_FORMAT
from src.storage import StorageProvider

router = APIRouter(tags=["Replay"])


def setup_replay_routes(
    router: APIRouter,
    storage: StorageProvider,
    verify_api_key: Callable,
) -> None:
    """Configure replay routes."""

    @router.get(
        "/replay/{category}",
        response_model=ReplayResponse,
        summary="Replay events",
        description="Get all events for a category, sorted chronologically. Use for system reconstruction.",
    )
    async def replay(
        category: str,
        from_date: Optional[str] = Query(None, alias="from"),
        _: None = Depends(verify_api_key),
    ):
        dates = await storage.list_dates(category)

        if not dates:
            return ReplayResponse(category=category, events=[], count=0)

        if from_date:
            start = datetime.strptime(from_date, DATE_FORMAT)
            dates = [d for d in dates if d >= start]

        if not dates:
            return ReplayResponse(category=category, events=[], count=0)

        lines = await storage.read_events_range(category, dates[0], dates[-1])

        events = []
        for line in lines:
            try:
                events.append(json.loads(line))
            except json.JSONDecodeError:
                continue

        events.sort(key=lambda e: e.get("timestamp", ""))

        return ReplayResponse(
            category=category,
            events=events,
            count=len(events),
            first_date=dates[0].strftime(DATE_FORMAT),
            last_date=dates[-1].strftime(DATE_FORMAT),
        )
