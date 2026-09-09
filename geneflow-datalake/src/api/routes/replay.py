import json
from datetime import datetime
from typing import Callable, Optional

from fastapi import APIRouter, Depends, Query

from src.api.responses import ReplayResponse
from src.constants import DATE_FORMAT
from src.storage import StorageProvider


def create_replay_router(
    storage: StorageProvider,
    verify_api_key: Callable,
) -> APIRouter:
    """Create replay router with routes configured."""
    router = APIRouter(tags=["Replay"])

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
            return ReplayResponse(
                category=category, events=[], count=0, first_date=None, last_date=None
            )

        if from_date:
            start = datetime.strptime(from_date, DATE_FORMAT)
            dates = [d for d in dates if d >= start]

        if not dates:
            return ReplayResponse(
                category=category, events=[], count=0, first_date=None, last_date=None
            )

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

    return router
