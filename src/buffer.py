import asyncio
from collections import defaultdict
from datetime import datetime
from pathlib import Path
from typing import Awaitable, Callable

import aiofiles
import structlog

logger = structlog.get_logger()

# Callback type for flush: (category, date, lines, pending_acks) -> None
FlushCallback = Callable[
    [str, datetime, list[str], list[tuple[str, str]]],
    Awaitable[None],
]


class EventBuffer:
    """
    Buffer that accumulates events and flushes in batches.

    Features:
    - WAL for pre-flush durability
    - Flush by size or time interval
    - Groups by (category, date)
    - Stores pending ACKs to confirm after flush
    """

    def __init__(
        self,
        flush_callback: FlushCallback,
        max_size: int = 100,
        flush_interval: float = 5.0,
        wal_path: str = "./data/wal",
    ):
        self.flush_callback = flush_callback
        self.max_size = max_size
        self.flush_interval = flush_interval
        self.wal_path = Path(wal_path)

        # Buffer: {(category, date_str): [event_lines]}
        self._buffer: dict[tuple[str, str], list[str]] = defaultdict(list)
        # Pending ACKs: {(category, date_str): [(stream, msg_id)]}
        self._pending_acks: dict[tuple[str, str], list[tuple[str, str]]] = defaultdict(list)

        self._count = 0
        self._lock = asyncio.Lock()
        self._flush_task: asyncio.Task | None = None
        self._running = False

    async def start(self) -> None:
        """Start the buffer."""
        self._running = True
        self.wal_path.mkdir(parents=True, exist_ok=True)

        # Recover events from WAL
        await self._recover_from_wal()

        # Start periodic flush loop
        self._flush_task = asyncio.create_task(self._flush_loop())
        logger.info("buffer_started", wal_path=str(self.wal_path))

    async def stop(self) -> None:
        """Stop the buffer, final flush."""
        self._running = False

        if self._flush_task:
            self._flush_task.cancel()
            try:
                await self._flush_task
            except asyncio.CancelledError:
                pass

        # Final flush
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

        async with self._lock:
            # 1. Write to WAL first (durability)
            await self._write_to_wal(category, date_str, event_line)

            # 2. Add to buffer
            self._buffer[key].append(event_line)
            self._pending_acks[key].append((stream_name, msg_id))
            self._count += 1

            # 3. Flush if we hit the limit
            if self._count >= self.max_size:
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

            # Copy and clear buffer
            to_flush = dict(self._buffer)
            to_ack = dict(self._pending_acks)
            self._buffer = defaultdict(list)
            self._pending_acks = defaultdict(list)
            self._count = 0

        # Flush each group outside the lock
        for (category, date_str), lines in to_flush.items():
            if not lines:
                continue

            try:
                date = datetime.strptime(date_str, "%Y-%m-%d")
                acks = to_ack.get((category, date_str), [])

                # Call callback (persists and does ACK)
                await self.flush_callback(category, date, lines, acks)

                # Clear WAL after success
                await self._clear_wal(category, date_str)

                logger.debug("buffer_flushed", category=category, date=date_str, count=len(lines))

            except Exception as e:
                logger.error("flush_failed", category=category, date=date_str, error=str(e))
                # Return to buffer for retry
                async with self._lock:
                    self._buffer[(category, date_str)].extend(lines)
                    self._pending_acks[(category, date_str)].extend(
                        to_ack.get((category, date_str), [])
                    )
                    self._count += len(lines)
                raise

    async def _write_to_wal(self, category: str, date_str: str, event_line: str) -> None:
        """Write event to WAL."""
        wal_file = self.wal_path / f"{category}_{date_str}.wal"
        async with aiofiles.open(wal_file, mode="a", encoding="utf-8") as f:
            await f.write(event_line + "\n")

    async def _clear_wal(self, category: str, date_str: str) -> None:
        """Delete WAL after successful flush."""
        wal_file = self.wal_path / f"{category}_{date_str}.wal"
        if wal_file.exists():
            wal_file.unlink()

    async def _recover_from_wal(self) -> None:
        """Recover events from WAL on startup."""
        if not self.wal_path.exists():
            return

        recovered = 0
        for wal_file in self.wal_path.glob("*.wal"):
            try:
                # Parse name: {category}_{date}.wal
                name = wal_file.stem
                parts = name.rsplit("_", 1)
                if len(parts) != 2:
                    continue

                category, date_str = parts

                async with aiofiles.open(wal_file, mode="r", encoding="utf-8") as f:
                    content = await f.read()

                lines = [line for line in content.split("\n") if line.strip()]
                if lines:
                    self._buffer[(category, date_str)].extend(lines)
                    # No ACKs for WAL-recovered events (that info was lost)
                    self._count += len(lines)
                    recovered += len(lines)

            except Exception as e:
                logger.error("wal_recovery_failed", file=str(wal_file), error=str(e))

        if recovered > 0:
            logger.info("wal_recovered", count=recovered)

    @property
    def size(self) -> int:
        """Current buffer size."""
        return self._count
