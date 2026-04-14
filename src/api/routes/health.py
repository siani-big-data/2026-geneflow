from typing import Callable

from fastapi import APIRouter

from src.api.responses import HealthResponse
from src.storage import StorageProvider

router = APIRouter(tags=["Health"])


def setup_health_routes(
    router: APIRouter,
    storage: StorageProvider,
    get_consumer_metrics: Callable[[], dict | None],
) -> None:
    """Configure health routes."""

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
