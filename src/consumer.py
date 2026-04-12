import asyncio
import json
from datetime import datetime
from typing import Optional

import redis.asyncio as redis
import structlog

from src.buffer import EventBuffer
from src.config import Settings
from src.deduplication import EventDeduplicator
from src.models import DatalakeEvent, EventBusMessage, EventCategory
from src.mounters.engine import MounterEngine
from src.retry import RetryHandler
from src.storage import StorageProvider

logger = structlog.get_logger()


class DatalakeConsumer:
    """
    Redis Streams consumer for the Datalake.

    - Consumes from all streams (9 categories)
    - Uses Consumer Groups for scalability
    - Deduplicates by eventId
    - Buffer with WAL for durability
    - XACK only after confirmed persistence
    """

    def __init__(
        self,
        settings: Settings,
        storage: StorageProvider,
        mounter_engine: Optional[MounterEngine] = None,
    ):
        self.settings = settings
        self.storage = storage
        self.mounter_engine = mounter_engine
        self.redis: Optional[redis.Redis] = None
        self._running = False

        # Components
        self.buffer = EventBuffer(
            flush_callback=self._on_flush,
            max_size=settings.buffer_max_size,
            flush_interval=settings.buffer_flush_interval,
            wal_path=settings.wal_path,
        )

        self.retry_handler = RetryHandler(
            max_retries=settings.retry_max_attempts,
            base_delay=settings.retry_base_delay,
            max_delay=settings.retry_max_delay,
            dlq_path=settings.dlq_path,
        )

        self.deduplicator = EventDeduplicator(
            ttl_hours=settings.dedup_ttl_hours,
            max_size=settings.dedup_max_size,
        )

        # Metrics
        self._events_received = 0
        self._events_persisted = 0
        self._events_duplicates = 0
        self._errors = 0

    @property
    def _streams(self) -> dict[str, str]:
        """Mapping of stream_name → category."""
        return {
            f"{self.settings.redis_stream_prefix}:{cat.value}": cat.value for cat in EventCategory
        }

    @property
    def metrics(self) -> dict:
        """Consumer metrics."""
        return {
            "events_received": self._events_received,
            "events_persisted": self._events_persisted,
            "events_duplicates": self._events_duplicates,
            "errors": self._errors,
            "buffer_size": self.buffer.size,
            "dedup_size": self.deduplicator.size,
            "retry": self.retry_handler.metrics,
        }

    async def start(self) -> None:
        """Start the consumer."""
        logger.info(
            "consumer_starting",
            redis_url=self.settings.redis_url,
            group=self.settings.redis_consumer_group,
            consumer=self.settings.redis_consumer_name,
            categories=[cat.value for cat in EventCategory],
        )

        # Connect to Redis
        self.redis = redis.from_url(
            self.settings.redis_url,
            decode_responses=True,
        )

        # Verify storage
        if not await self.storage.health_check():
            raise RuntimeError("Storage health check failed")

        # Create consumer groups
        await self._ensuREDACTED()

        # Start components
        await self.buffer.start()
        await self.retry_handler.start(self._retry_event)
        await self.deduplicator.start()

        # Start consume loop
        self._running = True
        await self._consume_loop()

    async def stop(self) -> None:
        """Stop the consumer."""
        logger.info("consumer_stopping", metrics=self.metrics)
        self._running = False

        await self.buffer.stop()
        await self.retry_handler.stop()
        await self.deduplicator.stop()

        if self.redis:
            await self.redis.close()

        await self.storage.close()

    async def _ensuREDACTED(self) -> None:
        """Create consumer groups if they don't exist."""
        for stream_name in self._streams.keys():
            try:
                await self.redis.xgroup_create(
                    stream_name,
                    self.settings.redis_consumer_group,
                    id="0",
                    mkstream=True,
                )
                logger.info("consumer_group_created", stream=stream_name)
            except redis.ResponseError as e:
                if "BUSYGROUP" in str(e):
                    logger.debug("consumer_group_exists", stream=stream_name)
                else:
                    raise

    async def _consume_loop(self) -> None:
        """Main consume loop."""
        streams_to_read = {name: ">" for name in self._streams.keys()}

        while self._running:
            try:
                messages = await self.redis.xreadgroup(
                    groupname=self.settings.redis_consumer_group,
                    consumername=self.settings.redis_consumer_name,
                    streams=streams_to_read,
                    count=self.settings.batch_size,
                    block=self.settings.redis_block_ms,
                )

                if not messages:
                    continue

                for stream_name, entries in messages:
                    category = self._streams.get(stream_name, "unknown")
                    for msg_id, data in entries:
                        await self._process_message(stream_name, category, msg_id, data)

            except redis.ConnectionError as e:
                logger.error("redis_connection_error", error=str(e))
                self._errors += 1
                await asyncio.sleep(5)
            except Exception as e:
                logger.error("consume_loop_error", error=str(e))
                self._errors += 1
                await asyncio.sleep(1)

    async def _process_message(
        self,
        stream_name: str,
        category: str,
        msg_id: str,
        data: dict,
    ) -> None:
        """Process a message from Redis."""
        try:
            self._events_received += 1

            # Parse message
            event_msg = EventBusMessage.from_redis(data)

            # Check duplicate
            if await self.deduplicator.is_duplicate(event_msg.eventId):
                self._events_duplicates += 1
                # Immediate ACK for duplicates
                await self.redis.xack(
                    stream_name,
                    self.settings.redis_consumer_group,
                    msg_id,
                )
                logger.debug("duplicate_skipped", event_id=event_msg.eventId)
                return

            # Parse timestamp
            try:
                timestamp = datetime.fromtimestamp(event_msg.timestamp / 1000)
            except (ValueError, TypeError):
                timestamp = datetime.utcnow()

            # Parse data
            try:
                event_data = (
                    json.loads(event_msg.data)
                    if isinstance(event_msg.data, str)
                    else event_msg.data
                )
            except json.JSONDecodeError:
                event_data = {"raw": event_msg.data}

            # Create normalized event
            event = DatalakeEvent(
                eventId=event_msg.eventId,
                type=event_msg.type,
                category=category,
                timestamp=timestamp,
                streamId=msg_id,
                data=event_data,
            )

            # Add to buffer (NO XACK here)
            await self.buffer.add(
                category=category,
                date=timestamp,
                event_line=event.to_json_line(),
                stream_name=stream_name,
                msg_id=msg_id,
            )

            # Mark as seen
            await self.deduplicator.mark_seen(event_msg.eventId)

            logger.debug(
                "event_buffered",
                event_id=event_msg.eventId,
                type=event_msg.type,
                category=category,
            )

        except Exception as e:
            self._errors += 1
            logger.error(
                "process_message_failed",
                stream=stream_name,
                msg_id=msg_id,
                error=str(e),
            )

    async def _on_flush(
        self,
        category: str,
        date: datetime,
        event_lines: list[str],
        pending_acks: list[tuple[str, str]],
    ) -> None:
        """Callback when buffer flushes."""
        try:
            # Persist events to storage (JSONL files)
            await self.storage.append_events_batch(category, date, event_lines)
            self._events_persisted += len(event_lines)

            # Dispatch to mounters (PostgreSQL, etc.)
            if self.mounter_engine:
                for event_line in event_lines:
                    try:
                        event = json.loads(event_line)
                        # Add category to event for mounter routing
                        event["category"] = category
                        await self.mounter_engine._dispatch_event(event)
                    except Exception as e:
                        logger.warning(
                            "mounter_dispatch_failed",
                            category=category,
                            error=str(e),
                        )

            # XACK only after persisting
            for stream_name, msg_id in pending_acks:
                await self.redis.xack(
                    stream_name,
                    self.settings.redis_consumer_group,
                    msg_id,
                )

            logger.info(
                "events_persisted",
                category=category,
                date=date.strftime("%Y-%m-%d"),
                count=len(event_lines),
            )

        except Exception as e:
            self._errors += 1
            # Add to retry
            for event_line in event_lines:
                await self.retry_handler.add_failed_event(
                    category=category,
                    date=date,
                    event_line=event_line,
                    error=str(e),
                )
            raise

    async def _retry_event(
        self,
        category: str,
        date: datetime,
        event_line: str,
    ) -> None:
        """Callback to retry an event."""
        await self.storage.append_events_batch(category, date, [event_line])
        self._events_persisted += 1
