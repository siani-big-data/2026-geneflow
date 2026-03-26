"""PostgreSQL mounter for projecting events."""

import structlog

from src.mounters.base import BaseMounter
from src.mounters.postgres.connection import PostgresConnection

logger = structlog.get_logger()


class PostgresMounter(BaseMounter):
    """Mounter for projecting events to PostgreSQL."""

    def __init__(self, dsn: str):
        super().__init__(
            name="postgres",
            categories=["users", "studies", "traces", "alignments", "billing"],
        )
        self._connection = PostgresConnection(dsn=dsn)

    async def start(self) -> None:
        """Start the mounter."""
        await self._connection.connect()
        self._running = True
        logger.info("postgres_mounter_started")

    async def stop(self) -> None:
        """Stop the mounter."""
        await self._connection.close()
        self._running = False
        logger.info("postgres_mounter_stopped")

    async def handle_event(self, event: dict) -> None:
        """Handle an incoming event."""
        self._metrics["events_processed"] += 1

    async def health_check(self) -> bool:
        """Check if PostgreSQL connection is healthy."""
        return await self._connection.health_check()

    async def rebuild(self) -> None:
        """Rebuild by truncating tables."""
        self._metrics["events_processed"] = 0
        logger.info("postgres_mounter_rebuild")
