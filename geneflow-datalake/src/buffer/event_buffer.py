"""Event buffer with batching and WAL."""

import asyncio
from collections import defaultdict
from datetime import datetime
from typing import Awaitable, Callable

import structlog

from src.buffer.wal import WriteAheadLog
from src.constants import BUFFER_DEFAULT_MAX_SIZE, BUFFER_FLUSH_INTERVAL_SECONDS

logger = structlog.get_logger()

FlushCallback = Callable[
    [str, datetime, list[str], list[tuple[str, str]]],
    Awaitable[None],
]


class EventBuffer:
    """Buffer that accumulates events and flushes in batches.

    Features:
    - WAL for pre-flush durability
    - Flush by size or time interval
    - Groups by (category, date)
    - Stores pending ACKs to confirm after flush
    """

    def __init__(
        self,
        flush_callback: FlushCallback,
        max_size: int = BUFFER_DEFAULT_MAX_SIZE,
        flush_interval: float = BUFFER_FLUSH_INTERVAL_SECONDS,
        wal_path: str = "./data/wal",
    ):
        self.flush_callback = flush_callback
        self.max_size = max_size
        self.flush_interval = flush_interval

        self._wal = WriteAheadLog(wal_path)
        self._buffer: dict[tuple[str, str], list[str]] = defaultdict(list)
        self._pending_acks: dict[tuple[str, str], list[tuple[str, str]]] = defaultdict(list)

        self._count = 0
        self._lock = asyncio.Lock()
        self._flush_task: asyncio.Task | None = None
        self._running = False

    async def start(self) -> None:
        """Start the buffer."""
        self._running = True
        await self._wal.initialize()
        await self._recover_from_wal()
        self._flush_task = asyncio.create_task(self._flush_loop())
        logger.info("buffer_started")

    async def stop(self) -> None:
        """Stop the buffer with final flush."""
        self._running = False
        if self._flush_task:
            self._flush_task.cancel()
            try:
                await self._flush_task
            except asyncio.CancelledError:
                pass
        await self._flush_all()
        logger.info("buffer_stopped")

    async def add(
        self,
        category: str,
        date: datetime,
        event_line: str,
        stream_name: str,
        msg_id: str,
    ) -> None:
        """Add event to buffer."""
        date_str = date.strftime("%Y-%m-%d")
        key = (category, date_str)
        should_flush = False

        async with self._lock:
            await self._wal.write(category, date_str, event_line)
            self._buffer[key].append(event_line)
            self._pending_acks[key].append((stream_name, msg_id))
            self._count += 1
            should_flush = self._count >= self.max_size

        if should_flush:
            await self._flush_all()

    async def _flush_loop(self) -> None:
        """Periodic flush loop."""
        while self._running:
            try:
                await asyncio.sleep(self.flush_interval)
                await self._flush_all()
            except asyncio.CancelledError:
                break
            except Exception as e:
                logger.error("flush_loop_error", error=str(e))

    async def _flush_all(self) -> None:
        """Flush all events from the buffer."""
        async with self._lock:
            if not self._buffer:
                return

            to_flush = dict(self._buffer)
            to_ack = dict(self._pending_acks)
            self._buffer = defaultdict(list)
            self._pending_acks = defaultdict(list)
            self._count = 0

        for (category, date_str), lines in to_flush.items():
            if not lines:
                continue

            try:
                date = datetime.strptime(date_str, "%Y-%m-%d")
                acks = to_ack.get((category, date_str), [])
                await self.flush_callback(category, date, lines, acks)
                await self._wal.clear(category, date_str)
                logger.debug("buffer_flushed", category=category, date=date_str, count=len(lines))
            except Exception as e:
                logger.error("flush_failed", category=category, date=date_str, error=str(e))
                async with self._lock:
                    self._buffer[(category, date_str)].extend(lines)
                    self._pending_acks[(category, date_str)].extend(
                        to_ack.get((category, date_str), [])
                    )
                    self._count += len(lines)
                raise

    async def _recover_from_wal(self) -> None:
        """Recover events from WAL on startup."""
        recovered = await self._wal.recover()
        for key, lines in recovered.items():
            self._buffer[key].extend(lines)
            self._count += len(lines)

    @property
    def size(self) -> int:
        """Current buffer size."""
        return self._count
