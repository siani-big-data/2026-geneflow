from typing import Callable

from fastapi import APIRouter, Depends

from src.api.responses import (
    AvailableCategoriesResponse,
    CategoriesResponse,
    CategoryDatesResponse,
    CategoryStatsResponse,
)
from src.api.services import CategoryStatsService


def create_categories_router(
    category_service: CategoryStatsService,
    verify_api_key: Callable,
) -> APIRouter:
    """Create categories router with routes configured."""
    router = APIRouter(prefix="/categories", tags=["Categories"])

    @router.get(
        "",
        response_model=CategoriesResponse,
        summary="List categories with data",
        description="Returns event categories that have stored data.",
    )
    async def list_categories(_: None = Depends(verify_api_key)):
        categories = await category_service.list_categories_with_data()
        return CategoriesResponse(categories=categories)

    @router.get(
        "/available",
        response_model=AvailableCategoriesResponse,
        summary="List all valid categories",
        description="Returns all valid event categories defined in the system.",
    )
    async def list_available_categories(_: None = Depends(verify_api_key)):
        categories = category_service.list_available_categories()
        return AvailableCategoriesResponse(categories=categories, count=len(categories))

    @router.get(
        "/{category}/stats",
        response_model=CategoryStatsResponse,
        summary="Get category statistics",
        description="Returns event count, date range, and file count for a category.",
    )
    async def get_category_stats(category: str, _: None = Depends(verify_api_key)):
        stats = await category_service.get_stats(category)
        return CategoryStatsResponse(
            category=stats.category,
            event_count=stats.event_count,
            first_date=stats.first_date,
            last_date=stats.last_date,
            file_count=stats.file_count,
        )

    @router.get(
        "/{category}/dates",
        response_model=CategoryDatesResponse,
        summary="List available dates",
        description="Returns all dates with events for a category.",
    )
    async def get_category_dates(category: str, _: None = Depends(verify_api_key)):
        result = await category_service.get_dates(category)
        return CategoryDatesResponse(
            category=result.category,
            dates=result.dates,
            count=result.count,
        )

    return router
