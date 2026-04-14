"""Retry handler with exponential backoff and DLQ."""

import asyncio
import json
from datetime import datetime, timedelta
from pathlib import Path
from typing import Awaitable, Callable, Optional
from uuid import uuid4

import aiofiles
import structlog

from src.constants import RETRY_BASE_DELAY_SECONDS, RETRY_MAX_ATTEMPTS, RETRY_MAX_DELAY_SECONDS
from src.models import RetryableEvent

logger = structlog.get_logger()

RetryCallback = Callable[[str, datetime, str], Awaitable[None]]


class RetryHandler:
    """Handles retries with exponential backoff and DLQ.

    - Failed events go to retry queue
    - Exponential backoff between attempts
    - After max_retries -> DLQ
    - API to view and retry DLQ events
    """

    def __init__(
        self,
        max_retries: int = RETRY_MAX_ATTEMPTS,
        base_delay: float = RETRY_BASE_DELAY_SECONDS,
        max_delay: float = RETRY_MAX_DELAY_SECONDS,
        dlq_path: str = "./data/dlq",
        check_interval: float = 1.0,
    ):
        self.max_retries = max_retries
        self.base_delay = base_delay
        self.max_delay = max_delay
        self.dlq_path = Path(dlq_path)
        self.check_interval = check_interval

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
        self.dlq_path.mkdir(parents=True, exist_ok=True)
        self._retry_task = asyncio.create_task(self._retry_loop())
        logger.info("retry_handler_started", dlq_path=str(self.dlq_path))

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
                await self._move_to_dlq(event, "shutdown")
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
            nextRetryAt=self._calculate_next_retry(0),
        )

        async with self._lock:
            self._queue.append(event)

        logger.warning("event_added_to_retry", event_id=event.id, category=category, error=error)

    def _calculate_next_retry(self, retry_count: int) -> datetime:
        """Calculate next retry time with exponential backoff."""
        delay = min(self.base_delay * (2**retry_count), self.max_delay)
        return datetime.utcnow() + timedelta(seconds=delay)

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
                await self._move_to_dlq(event, str(e))
            else:
                event.nextRetryAt = self._calculate_next_retry(event.retryCount)
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

    async def _move_to_dlq(self, event: RetryableEvent, error: str) -> None:
        """Move event to DLQ."""
        self._events_to_dlq += 1

        dlq_record = {
            "eventId": event.id,
            "category": event.category,
            "date": event.date.isoformat(),
            "eventLine": event.eventLine,
            "retryCount": event.retryCount,
            "lastError": error,
            "createdAt": event.createdAt.isoformat(),
            "movedToDlqAt": datetime.utcnow().isoformat(),
        }

        today = datetime.utcnow().strftime("%Y-%m-%d")
        dlq_file = self.dlq_path / f"{today}.jsonl"

        async with aiofiles.open(dlq_file, mode="a", encoding="utf-8") as f:
            await f.write(json.dumps(dlq_record, ensuREDACTED=False) + "\n")

        logger.error(
            "event_moved_to_dlq",
            event_id=event.id,
            category=event.category,
            retries=event.retryCount,
            error=error,
        )

    async def get_dlq_events(self, date: Optional[datetime] = None) -> list[dict]:
        """Get events from DLQ."""
        if date is None:
            date = datetime.utcnow()

        dlq_file = self.dlq_path / f"{date.strftime('%Y-%m-%d')}.jsonl"
        if not dlq_file.exists():
            return []

        events = []
        async with aiofiles.open(dlq_file, mode="r", encoding="utf-8") as f:
            content = await f.read()

        for line in content.split("\n"):
            if line.strip():
                try:
                    events.append(json.loads(line))
                except json.JSONDecodeError:
                    pass

        return events

    async def get_all_dlq_events(self) -> list[dict]:
        """Get all events from DLQ."""
        all_events = []

        for dlq_file in sorted(self.dlq_path.glob("*.jsonl")):
            async with aiofiles.open(dlq_file, mode="r", encoding="utf-8") as f:
                content = await f.read()

            for line in content.split("\n"):
                if line.strip():
                    try:
                        all_events.append(json.loads(line))
                    except json.JSONDecodeError:
                        pass

        return all_events

    async def replay_dlq_event(self, event_id: str) -> bool:
        """Retry a specific event from DLQ."""
        for dlq_file in self.dlq_path.glob("*.jsonl"):
            async with aiofiles.open(dlq_file, mode="r", encoding="utf-8") as f:
                content = await f.read()

            for line in content.split("\n"):
                if line.strip():
                    try:
                        record = json.loads(line)
                        if record.get("eventId") == event_id:
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

        logger.warning("dlq_event_not_found", event_id=event_id)
        return False

    async def replay_all_dlq(self, date: Optional[datetime] = None) -> dict:
        """Retry all events from DLQ."""
        events = await self.get_dlq_events(date) if date else await self.get_all_dlq_events()

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
