"""Health API for GeneFlow Analysis Worker."""

from datetime import datetime, timezone
from typing import Optional

from fastapi import FastAPI
from pydantic import BaseModel

from src.config import settings
from src.models import WorkerStatus


class HealthResponse(BaseModel):
    """Health check response."""

    status: str
    timestamp: str
    version: str = "2.0.0"
    workers: dict[str, str] = {}
    redis: str = "unknown"


class WorkerHealthInfo(BaseModel):
    """Health info for a worker."""

    status: str
    jobsProcessed: int
    jobsFailed: int
    lastJobAt: Optional[str] = None
    averageProcessingTimeMs: float


# Global references to workers (set by main.py)
_workers: dict = {}
_redis_healthy: bool = False


def create_app() -> FastAPI:
    """Create FastAPI application."""
    app = FastAPI(
        title="GeneFlow Analysis Worker",
        description="Bioinformatics analysis worker for GeneFlow platform",
        version="2.0.0",
        docs_url=None,  # Disable docs in production
        redoc_url=None,
    )

    @app.get("/health", response_model=HealthResponse)
    async def health_check() -> HealthResponse:
        """
        Health check endpoint for Docker/K8s.

        Returns worker status and basic metrics.
        """
        workers_status = {}

        for name, worker in _workers.items():
            workers_status[name] = worker.status.value

        # Determine overall status
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

    @app.get("/health/workers/{worker_name}", response_model=WorkerHealthInfo)
    async def worker_health(worker_name: str) -> WorkerHealthInfo:
        """Get detailed health info for a specific worker."""
        if worker_name not in _workers:
            from fastapi import HTTPException
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

    @app.get("/ready")
    async def readiness_check() -> dict:
        """
        Readiness check for K8s.

        Returns 200 if workers are ready to process jobs.
        """
        if not _workers:
            from fastapi import HTTPException
            raise HTTPException(status_code=503, detail="No workers registered")

        if not _redis_healthy:
            from fastapi import HTTPException
            raise HTTPException(status_code=503, detail="Redis not connected")

        running = [w for w in _workers.values() if w.status == WorkerStatus.RUNNING]
        if not running:
            from fastapi import HTTPException
            raise HTTPException(status_code=503, detail="No workers running")

        return {"ready": True}

    @app.get("/live")
    async def liveness_check() -> dict:
        """
        Liveness check for K8s.

        Returns 200 if the process is alive.
        """
        return {"alive": True}

    return app


def register_workers(workers: dict) -> None:
    """Register workers for health monitoring."""
    global _workers
    _workers = workers


def set_redis_health(healthy: bool) -> None:
    """Set Redis connection health status."""
    global _redis_healthy
    _redis_healthy = healthy


# Create app instance
app = create_app()
