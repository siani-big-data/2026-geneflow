"""Base worker class for Redis Streams consumers."""

import asyncio
import json
from abc import ABC, abstractmethod
from datetime import datetime, timezone
from typing import Any, Optional

import structlog
from redis.asyncio import Redis

from src.config import Settings
from src.events.publisher import EventBusPublisher
from src.models import WorkerMetrics, WorkerStatus

logger = structlog.get_logger()


class BaseWorker(ABC):
    """
    Abstract base class for Redis Streams workers.

    Handles stream consumption, consumer groups, and job processing.
    """

    def __init__(
        self,
        redis: Redis,
        publisher: EventBusPublisher,
        settings: Settings,
    ):
        self._redis = redis
        self._publisher = publisher
        self._settings = settings
        self._status = WorkerStatus.STOPPED
        self._metrics = WorkerMetrics()
        self._running = False
        self._consumer_group = settings.redis_consumer_group
        self._consumer_name = settings.redis_consumer_name

    @property
    @abstractmethod
    def name(self) -> str:
        """Worker name for logging."""
        pass

    @property
    @abstractmethod
    def stream_name(self) -> str:
        """Redis stream to consume from."""
        pass

    @abstractmethod
    async def process_job(self, job_id: str, job_data: dict[str, Any]) -> None:
        """Process a single job. Must be implemented by subclasses."""
        pass

    @property
    def status(self) -> WorkerStatus:
        """Current worker status."""
        return self._status

    @property
    def metrics(self) -> WorkerMetrics:
        """Worker metrics."""
        return self._metrics

    async def start(self) -> None:
        """Start the worker."""
        self._status = WorkerStatus.STARTING
        logger.info("worker_starting", worker=self.name, stream=self.stream_name)

        # Ensure consumer group exists
        await self._ensuREDACTED()

        self._running = True
        self._status = WorkerStatus.RUNNING
        logger.info("worker_started", worker=self.name)

        # Start consuming
        await self._consume_loop()

    async def stop(self) -> None:
        """Stop the worker gracefully."""
        self._status = WorkerStatus.STOPPING
        logger.info("worker_stopping", worker=self.name)

        self._running = False
        self._status = WorkerStatus.STOPPED

        logger.info(
            "worker_stopped",
            worker=self.name,
            jobs_processed=self._metrics.jobsProcessed,
            jobs_failed=self._metrics.jobsFailed,
        )

    async def _ensuREDACTED(self) -> None:
        """Create consumer group if it doesn't exist."""
        try:
            await self._redis.xgroup_create(
                self.stream_name,
                self._consumer_group,
                id="0",
                mkstream=True,
            )
            logger.info(
                "consumer_group_created",
                stream=self.stream_name,
                group=self._consumer_group,
            )
        except Exception as e:
            # Group already exists
            if "BUSYGROUP" in str(e):
                logger.debug(
                    "consumer_group_exists",
                    stream=self.stream_name,
                    group=self._consumer_group,
                )
            else:
                raise

    async def _consume_loop(self) -> None:
        """Main consumption loop."""
        while self._running:
            try:
                # Read from stream
                messages = await self._redis.xreadgroup(
                    groupname=self._consumer_group,
                    consumername=self._consumer_name,
                    streams={self.stream_name: ">"},
                    count=1,
                    block=self._settings.redis_block_ms,
                )

                if not messages:
                    continue

                for stream, entries in messages:
                    for message_id, data in entries:
                        await self._process_message(message_id, data)

            except asyncio.CancelledError:
                logger.info("worker_cancelled", worker=self.name)
                break
            except Exception as e:
                logger.error(
                    "consume_error",
                    worker=self.name,
                    error=str(e),
                    error_type=type(e).__name__,
                )
                await asyncio.sleep(1)  # Back off on error

    async def _process_message(
        self,
        message_id: str,
        data: dict[str, Any],
    ) -> None:
        """Process a single message from the stream."""
        start_time = datetime.now(timezone.utc)

        try:
            # Parse job data
            job_data = self._parse_message_data(data)

            logger.info(
                "job_received",
                worker=self.name,
                message_id=message_id,
                job_data_keys=list(job_data.keys()),
            )

            # Process the job
            await self.process_job(message_id, job_data)

            # Acknowledge the message
            await self._redis.xack(
                self.stream_name,
                self._consumer_group,
                message_id,
            )

            # Update metrics
            self._metrics.jobsProcessed += 1
            self._metrics.lastJobAt = datetime.now(timezone.utc)

            processing_time = (
                datetime.now(timezone.utc) - start_time
            ).total_seconds() * 1000

            # Update average processing time
            total_jobs = self._metrics.jobsProcessed
            current_avg = self._metrics.averageProcessingTimeMs
            self._metrics.averageProcessingTimeMs = (
                (current_avg * (total_jobs - 1) + processing_time) / total_jobs
            )

            logger.info(
                "job_completed",
                worker=self.name,
                message_id=message_id,
                processing_time_ms=round(processing_time, 2),
            )

        except Exception as e:
            self._metrics.jobsFailed += 1

            logger.error(
                "job_failed",
                worker=self.name,
                message_id=message_id,
                error=str(e),
                error_type=type(e).__name__,
            )

            # Still acknowledge to avoid reprocessing
            # In production, you might want dead-letter queue handling
            await self._redis.xack(
                self.stream_name,
                self._consumer_group,
                message_id,
            )

    def _parse_message_data(self, data: dict[str, Any]) -> dict[str, Any]:
        """Parse message data from Redis."""
        # Redis returns bytes, decode to string
        parsed = {}
        for key, value in data.items():
            if isinstance(key, bytes):
                key = key.decode("utf-8")
            if isinstance(value, bytes):
                value = value.decode("utf-8")

            # Try to parse JSON
            try:
                parsed[key] = json.loads(value)
            except (json.JSONDecodeError, TypeError):
                parsed[key] = value

        # If there's a 'data' key with nested content, flatten it
        if "data" in parsed and isinstance(parsed["data"], dict):
            return parsed["data"]

        return parsed
