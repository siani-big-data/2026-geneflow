from typing import Callable

from fastapi import APIRouter

from src.api.responses import HealthResponse
from src.storage import StorageProvider


def create_health_router(
    storage: StorageProvider,
    get_consumer_metrics: Callable[[], dict | None],
) -> APIRouter:
    """Create health router with routes configured."""
    router = APIRouter(tags=["Health"])

    @router.get(
        "/health",
        response_model=HealthResponse,
        summary="Service health check",
        description="Returns service status, storage health, and consumer metrics.",
    )
    async def health():
        storage_healthy = await storage.health_check()
        consumer_metrics = get_consumer_metrics()

        return HealthResponse(
            status="healthy" if storage_healthy else "degraded",
            storage_healthy=storage_healthy,
            consumer_metrics=consumer_metrics,
        )

    return router
