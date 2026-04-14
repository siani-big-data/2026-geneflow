from typing import Callable

from fastapi import APIRouter, Depends

from src.api.responses import (
    AvailableCategoriesResponse,
    CategoriesResponse,
    CategoryDatesResponse,
    CategoryStatsResponse,
)
from src.models import EventCategory
from src.storage import StorageProvider

router = APIRouter(prefix="/categories", tags=["Categories"])


def setup_categories_routes(
    router: APIRouter,
    storage: StorageProvider,
    verify_api_key: Callable,
) -> None:
    """Configure category routes."""

    @router.get(
        "",
        response_model=CategoriesResponse,
        summary="List categories with data",
        description="Returns event categories that have stored data.",
    )
    async def list_categories(_: None = Depends(verify_api_key)):
        categories = await storage.list_categories()
        return CategoriesResponse(categories=categories)

    @router.get(
        "/available",
        response_model=AvailableCategoriesResponse,
        summary="List all valid categories",
        description="Returns all valid event categories defined in the system.",
    )
    async def list_available_categories(_: None = Depends(verify_api_key)):
        categories = [c.value for c in EventCategory]
        return AvailableCategoriesResponse(categories=categories, count=len(categories))

    @router.get(
        "/{category}/stats",
        response_model=CategoryStatsResponse,
        summary="Get category statistics",
        description="Returns event count, date range, and file count for a category.",
    )
    async def get_category_stats(category: str, _: None = Depends(verify_api_key)):
        stats = await storage.get_stats(category)
        return CategoryStatsResponse(
            category=category,
            event_count=stats.get("event_count", 0),
            first_date=stats.get("first_date"),
            last_date=stats.get("last_date"),
            file_count=stats.get("file_count", 0),
        )

    @router.get(
        "/{category}/dates",
        response_model=CategoryDatesResponse,
        summary="List available dates",
        description="Returns all dates with events for a category.",
    )
    async def get_category_dates(category: str, _: None = Depends(verify_api_key)):
        dates = await storage.list_dates(category)
        return CategoryDatesResponse(
            category=category,
            dates=[d.strftime("%Y-%m-%d") for d in dates],
            count=len(dates),
        )
