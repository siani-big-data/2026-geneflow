"""Health check routes."""

from datetime import datetime, timezone
from typing import TYPE_CHECKING

from fastapi import APIRouter, HTTPException

from src.api.responses import HealthResponse, LiveResponse, ReadyResponse, WorkerHealthInfo
from src.models import WorkerStatus

if TYPE_CHECKING:
    from src.workers.base import BaseWorker

router = APIRouter(tags=["Health"])

_workers: dict[str, "BaseWorker"] = {}
_redis_healthy: bool = False


def set_workers(workers: dict[str, "BaseWorker"]) -> None:
    """Set workers for health monitoring."""
    global _workers
    _workers = workers


def set_redis_health(healthy: bool) -> None:
    """Set Redis connection health status."""
    global _redis_healthy
    _redis_healthy = healthy


def get_redis_health() -> bool:
    """Get Redis connection health status."""
    return _redis_healthy


def get_workers() -> dict[str, "BaseWorker"]:
    """Get registered workers."""
    return _workers


@router.get("/health", response_model=HealthResponse)
async def health_check() -> HealthResponse:
    """
    Health check endpoint for Docker/K8s.

    Returns worker status and basic metrics.
    """
    workers_status = {}

    for name, worker in _workers.items():
        workers_status[name] = worker.status.value

    if not _workers:
        overall_status = "degraded"
    elif all(w.status == WorkerStatus.RUNNING for w in _workers.values()):
        overall_status = "healthy"
    elif any(w.status == WorkerStatus.RUNNING for w in _workers.values()):
        overall_status = "degraded"
    else:
        overall_status = "unhealthy"

    return HealthResponse(
        status=overall_status,
        timestamp=datetime.now(timezone.utc).isoformat(),
        workers=workers_status,
        redis="connected" if _redis_healthy else "disconnected",
    )


@router.get("/health/workers/{worker_name}", response_model=WorkerHealthInfo)
async def worker_health(worker_name: str) -> WorkerHealthInfo:
    """Get detailed health info for a specific worker."""
    if worker_name not in _workers:
        raise HTTPException(status_code=404, detail=f"Worker {worker_name} not found")

    worker = _workers[worker_name]
    metrics = worker.metrics

    return WorkerHealthInfo(
        status=worker.status.value,
        jobsProcessed=metrics.jobsProcessed,
        jobsFailed=metrics.jobsFailed,
        lastJobAt=metrics.lastJobAt.isoformat() if metrics.lastJobAt else None,
        averageProcessingTimeMs=round(metrics.averageProcessingTimeMs, 2),
    )


@router.get("/ready", response_model=ReadyResponse)
async def readiness_check() -> ReadyResponse:
    """
    Readiness check for K8s.

    Returns 200 if workers are ready to process jobs.
    """
    if not _workers:
        raise HTTPException(status_code=503, detail="No workers registered")

    if not _redis_healthy:
        raise HTTPException(status_code=503, detail="Redis not connected")

    running = [w for w in _workers.values() if w.status == WorkerStatus.RUNNING]
    if not running:
        raise HTTPException(status_code=503, detail="No workers running")

    return ReadyResponse(ready=True)


@router.get("/live", response_model=LiveResponse)
async def liveness_check() -> LiveResponse:
    """
    Liveness check for K8s.

    Returns 200 if the process is alive.
    """
    return LiveResponse(alive=True)
