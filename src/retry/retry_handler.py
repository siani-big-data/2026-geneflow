"""Retry handler with exponential backoff."""

import asyncio
from datetime import datetime
from typing import Awaitable, Callable, Optional
from uuid import uuid4

import structlog

from src.constants import RETRY_MAX_ATTEMPTS
from src.models import RetryableEvent
from src.retry.backoff import ExponentialBackoff
from src.retry.dlq import DeadLetterQueue

logger = structlog.get_logger()

RetryCallback = Callable[[str, datetime, str], Awaitable[None]]


class RetryHandler:
    """Handles retries with exponential backoff and DLQ."""

    def __init__(
        self,
        max_retries: int = RETRY_MAX_ATTEMPTS,
        base_delay: float = 1.0,
        max_delay: float = 300.0,
        dlq_path: str = "./data/dlq",
        check_interval: float = 1.0,
    ):
        self.max_retries = max_retries
        self.check_interval = check_interval

        self._backoff = ExponentialBackoff(base_delay, max_delay)
        self._dlq = DeadLetterQueue(dlq_path)

        self._queue: list[RetryableEvent] = []
        self._lock = asyncio.Lock()
        self._retry_task: Optional[asyncio.Task] = None
        self._retry_callback: Optional[RetryCallback] = None
        self._running = False

        self._retries_attempted = 0
        self._retries_succeeded = 0
        self._events_to_dlq = 0

    async def start(self, retry_callback: RetryCallback) -> None:
        """Start the handler."""
        self._retry_callback = retry_callback
        self._running = True
        await self._dlq.initialize()
        self._retry_task = asyncio.create_task(self._retry_loop())
        logger.info("retry_handler_started")

    async def stop(self) -> None:
        """Stop the handler, move pending to DLQ."""
        self._running = False
        if self._retry_task:
            self._retry_task.cancel()
            try:
                await self._retry_task
            except asyncio.CancelledError:
                pass

        async with self._lock:
            for event in self._queue:
                await self._dlq.add(event, "shutdown")
                self._events_to_dlq += 1
            self._queue.clear()

        logger.info("retry_handler_stopped", metrics=self.metrics)

    async def add_failed_event(
        self,
        category: str,
        date: datetime,
        event_line: str,
        error: str,
    ) -> None:
        """Add a failed event to the retry queue."""
        event = RetryableEvent(
            id=str(uuid4()),
            category=category,
            date=date,
            eventLine=event_line,
            retryCount=0,
            lastError=error,
            nextRetryAt=self._backoff.next_retry_at(0),
        )

        async with self._lock:
            self._queue.append(event)

        logger.warning("event_added_to_retry", event_id=event.id, category=category, error=error)

    async def _retry_loop(self) -> None:
        """Main retry loop."""
        while self._running:
            try:
                await asyncio.sleep(self.check_interval)
                now = datetime.utcnow()
                to_retry = []

                async with self._lock:
                    remaining = []
                    for event in self._queue:
                        if event.nextRetryAt and event.nextRetryAt <= now:
                            to_retry.append(event)
                        else:
                            remaining.append(event)
                    self._queue = remaining

                for event in to_retry:
                    await self._retry_event(event)

            except asyncio.CancelledError:
                break
            except Exception as e:
                logger.error("retry_loop_error", error=str(e))

    async def _retry_event(self, event: RetryableEvent) -> None:
        """Attempt to retry an event."""
        self._retries_attempted += 1

        try:
            await self._retry_callback(event.category, event.date, event.eventLine)
            self._retries_succeeded += 1
            logger.info(
                "retry_succeeded",
                event_id=event.id,
                category=event.category,
                attempt=event.retryCount + 1,
            )
        except Exception as e:
            event.retryCount += 1
            event.lastError = str(e)

            if event.retryCount >= self.max_retries:
                await self._dlq.add(event, str(e))
                self._events_to_dlq += 1
            else:
                event.nextRetryAt = self._backoff.next_retry_at(event.retryCount)
                async with self._lock:
                    self._queue.append(event)

                logger.warning(
                    "retry_failed",
                    event_id=event.id,
                    attempt=event.retryCount,
                    max_retries=self.max_retries,
                    next_retry=event.nextRetryAt.isoformat(),
                    error=str(e),
                )

    async def get_dlq_events(self, date: datetime | None = None) -> list[dict]:
        """Get events from DLQ."""
        return await self._dlq.get_events(date)

    async def get_all_dlq_events(self) -> list[dict]:
        """Get all events from DLQ."""
        return await self._dlq.get_all_events()

    async def replay_dlq_event(self, event_id: str) -> bool:
        """Retry a specific event from DLQ."""
        record = await self._dlq.find_event(event_id)
        if not record:
            logger.warning("dlq_event_not_found", event_id=event_id)
            return False

        try:
            await self._retry_callback(
                record["category"],
                datetime.fromisoformat(record["date"]),
                record["eventLine"],
            )
            logger.info("dlq_event_replayed", event_id=event_id)
            return True
        except Exception as e:
            logger.error("dlq_replay_failed", event_id=event_id, error=str(e))
            return False

    async def replay_all_dlq(self, date: datetime | None = None) -> dict:
        """Retry all events from DLQ."""
        events = await self._dlq.get_events(date) if date else await self._dlq.get_all_events()

        succeeded = 0
        failed = 0

        for event in events:
            try:
                await self._retry_callback(
                    event["category"],
                    datetime.fromisoformat(event["date"]),
                    event["eventLine"],
                )
                succeeded += 1
            except Exception:
                failed += 1

        return {"succeeded": succeeded, "failed": failed, "total": len(events)}

    @property
    def metrics(self) -> dict:
        """Handler metrics."""
        return {
            "queue_size": len(self._queue),
            "retries_attempted": self._retries_attempted,
            "retries_succeeded": self._retries_succeeded,
            "events_to_dlq": self._events_to_dlq,
        }

    @property
    def queue_size(self) -> int:
        """Current retry queue size."""
        return len(self._queue)
