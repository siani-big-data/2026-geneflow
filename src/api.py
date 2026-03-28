"""Health API for GeneFlow AI service."""

from datetime import datetime, timezone
from typing import Optional

from fastapi import FastAPI
from pydantic import BaseModel

from src.models import ServiceStatus


class HealthResponse(BaseModel):
    """Health check response."""

    status: str
    timestamp: str
    version: str = "1.0.0"
    redis: str = "unknown"
    claude: str = "unknown"


class ServiceHealthInfo(BaseModel):
    """Detailed service health info."""

    status: str
    analysesCompleted: int
    analysesFailed: int
    lastAnalysisAt: Optional[str] = None
    averageProcessingTimeMs: float


# Global references (set by main.py)
_service_status: ServiceStatus = ServiceStatus.STOPPED
_redis_healthy: bool = False
_claude_configured: bool = False
_metrics: dict = {}


def create_app() -> FastAPI:
    """Create FastAPI application."""
    app = FastAPI(
        title="GeneFlow AI",
        description="AI-powered sequence analysis for GeneFlow platform",
        version="1.0.0",
        docs_url=None,
        redoc_url=None,
    )

    @app.get("/health", response_model=HealthResponse)
    async def health_check() -> HealthResponse:
        """
        Health check endpoint for Docker/K8s.

        Returns service status and component health.
        """
        if _service_status == ServiceStatus.RUNNING and _redis_healthy:
            overall_status = "healthy"
        elif _service_status == ServiceStatus.RUNNING:
            overall_status = "degraded"
        else:
            overall_status = "unhealthy"

        return HealthResponse(
            status=overall_status,
            timestamp=datetime.now(timezone.utc).isoformat(),
            redis="connected" if _redis_healthy else "disconnected",
            claude="configured" if _claude_configured else "not_configured",
        )

    @app.get("/health/details", response_model=ServiceHealthInfo)
    async def health_details() -> ServiceHealthInfo:
        """Get detailed health info."""
        return ServiceHealthInfo(
            status=_service_status.value,
            analysesCompleted=_metrics.get("analysesCompleted", 0),
            analysesFailed=_metrics.get("analysesFailed", 0),
            lastAnalysisAt=_metrics.get("lastAnalysisAt"),
            averageProcessingTimeMs=_metrics.get("averageProcessingTimeMs", 0.0),
        )

    @app.get("/ready")
    async def readiness_check() -> dict:
        """
        Readiness check for K8s.

        Returns 200 if service is ready to handle requests.
        """
        if _service_status != ServiceStatus.RUNNING:
            from fastapi import HTTPException

            raise HTTPException(status_code=503, detail="Service not running")

        if not _redis_healthy:
            from fastapi import HTTPException

            raise HTTPException(status_code=503, detail="Redis not connected")

        return {"ready": True}

    @app.get("/live")
    async def liveness_check() -> dict:
        """
        Liveness check for K8s.

        Returns 200 if the process is alive.
        """
        return {"alive": True}

    return app


def set_service_status(status: ServiceStatus) -> None:
    """Set service status."""
    global _service_status
    _service_status = status


def set_redis_health(healthy: bool) -> None:
    """Set Redis connection health status."""
    global _redis_healthy
    _redis_healthy = healthy


def set_claude_configured(configured: bool) -> None:
    """Set Claude API configuration status."""
    global _claude_configured
    _claude_configured = configured


def set_metrics(metrics: dict) -> None:
    """Set service metrics."""
    global _metrics
    _metrics = metrics


# Create app instance
app = create_app()
