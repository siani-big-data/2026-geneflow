from typing import Callable, Optional

from fastapi import APIRouter, Depends, Query

from src.api.responses import DLQResponse, DLQRetryAllResponse, DLQRetryResponse
from src.api.services import DLQService


def create_dlq_router(
    dlq_service: DLQService,
    verify_api_key: Callable,
) -> APIRouter:
    """Create DLQ router with routes configured."""
    router = APIRouter(prefix="/dlq", tags=["DLQ"])

    @router.get(
        "",
        response_model=DLQResponse,
        summary="Get failed events",
        description="Get events from the Dead Letter Queue for a specific date (default: today).",
    )
    async def get_dlq(
        date: Optional[str] = Query(None, description="Date YYYY-MM-DD, default today"),
        _: None = Depends(verify_api_key),
    ):
        result = await dlq_service.get_events(date)
        return DLQResponse(events=result.events, count=result.count, date=result.date)

    @router.get(
        "/all",
        response_model=DLQResponse,
        summary="Get all failed events",
        description="Get all events from the Dead Letter Queue across all dates.",
    )
    async def get_all_dlq(_: None = Depends(verify_api_key)):
        result = await dlq_service.get_all_events()
        return DLQResponse(events=result.events, count=result.count)

    @router.post(
        "/retry/{event_id}",
        response_model=DLQRetryResponse,
        summary="Retry a failed event",
        description="Attempt to reprocess a specific event from the DLQ.",
    )
    async def retry_dlq_event(event_id: str, _: None = Depends(verify_api_key)):
        result = await dlq_service.retry_event(event_id)
        return DLQRetryResponse(
            success=result.success,
            event_id=result.event_id,
            message=result.message,
        )

    @router.post(
        "/retry-all",
        response_model=DLQRetryAllResponse,
        summary="Retry all failed events",
        description="Attempt to reprocess all events in the DLQ.",
    )
    async def retry_all_dlq(
        date: Optional[str] = Query(None, description="Filter by date (YYYY-MM-DD)"),
        _: None = Depends(verify_api_key),
    ):
        result = await dlq_service.retry_all(date)
        return DLQRetryAllResponse(
            succeeded=result.succeeded,
            failed=result.failed,
            total=result.total,
        )

    return router
