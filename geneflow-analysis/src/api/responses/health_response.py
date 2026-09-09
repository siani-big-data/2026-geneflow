"""Health check response models."""

from typing import Optional

from pydantic import BaseModel


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


class ReadyResponse(BaseModel):
    """Readiness check response."""

    ready: bool


class LiveResponse(BaseModel):
    """Liveness check response."""

    alive: bool
