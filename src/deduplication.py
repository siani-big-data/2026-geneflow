import asyncio
from datetime import datetime, timedelta
from typing import Optional

import structlog

logger = structlog.get_logger()


class EventDeduplicator:
    """
    Event deduplicator by eventId.

    Maintains an in-memory set of seen eventIds with TTL
    to avoid persisting the same event twice.
    """

    def __init__(
        self,
        ttl_hours: int = 24,
        max_size: int = 100000,
        cleanup_interval: float = 300.0,  # 5 minutes
    ):
        self.ttl = timedelta(hours=ttl_hours)
        self.max_size = max_size
        self.cleanup_interval = cleanup_interval

        self._seen: dict[str, datetime] = {}
        self._lock = asyncio.Lock()
        self._cleanup_task: Optional[asyncio.Task] = None
        self._running = False

    async def start(self) -> None:
        """Start the deduplicator."""
        self._running = True
        self._cleanup_task = asyncio.create_task(self._cleanup_loop())
        logger.info("deduplicator_started", ttl_hours=self.ttl.total_seconds() / 3600)

    async def stop(self) -> None:
        """Stop the deduplicator."""
        self._running = False
        if self._cleanup_task:
            self._cleanup_task.cancel()
            try:
                await self._cleanup_task
            except asyncio.CancelledError:
                pass
        logger.info("deduplicator_stopped", seen_count=len(self._seen))

    async def is_duplicate(self, event_id: str) -> bool:
        """Check if an event was already seen."""
        async with self._lock:
            return event_id in self._seen

    async def mark_seen(self, event_id: str) -> None:
        """Mark an event as seen."""
        async with self._lock:
            self._seen[event_id] = datetime.utcnow()

            # If we exceed max_size, cleanup immediately
            if len(self._seen) > self.max_size:
                await self._cleanup()

    async def _cleanup_loop(self) -> None:
        """Periodic cleanup loop."""
        while self._running:
            try:
                await asyncio.sleep(self.cleanup_interval)
                async with self._lock:
                    await self._cleanup()
            except asyncio.CancelledError:
                break
            except Exception as e:
                logger.error("deduplicator_cleanup_error", error=str(e))

    async def _cleanup(self) -> None:
        """Remove expired entries."""
        now = datetime.utcnow()
        expired = [event_id for event_id, seen_at in self._seen.items() if now - seen_at > self.ttl]

        for event_id in expired:
            del self._seen[event_id]

        if expired:
            logger.debug("deduplicator_cleanup", removed=len(expired), remaining=len(self._seen))

    @property
    def size(self) -> int:
        """Number of events in memory."""
        return len(self._seen)
