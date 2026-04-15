"""Analysis API wrapper."""

from typing import TYPE_CHECKING

from fastapi import FastAPI

from src.api.app import create_app
from src.api.routes.health import set_redis_health, set_workers

if TYPE_CHECKING:
    from src.config import Settings
    from src.workers.base import BaseWorker


class AnalysisAPI:
    """Wrapper for the Analysis API.

    Provides a clean interface for managing the FastAPI application
    and its dependencies.
    """

    def __init__(self, settings: "Settings"):
        """Initialize the API.

        Args:
            settings: Application settings.
        """
        self.settings = settings
        self.app: FastAPI = create_app()
        self._workers: dict[str, "BaseWorker"] = {}

    def register_workers(self, workers: dict[str, "BaseWorker"]) -> None:
        """Register workers for health monitoring.

        Args:
            workers: Dictionary of worker name to worker instance.
        """
        self._workers = workers
        set_workers(workers)

    def set_redis_health(self, healthy: bool) -> None:
        """Update Redis health status.

        Args:
            healthy: Whether Redis connection is healthy.
        """
        set_redis_health(healthy)

    @property
    def workers(self) -> dict[str, "BaseWorker"]:
        """Get registered workers."""
        return self._workers
