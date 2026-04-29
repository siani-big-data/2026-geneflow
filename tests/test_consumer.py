"""Tests for the Datalake consumer module."""

import tempfile
from datetime import datetime
from pathlib import Path
from unittest.mock import AsyncMock, MagicMock

import pytest

from src.config import Settings
from src.consumer import DatalakeConsumer
from src.models import EventCategory


@pytest.fixture
def temp_dir():
    with tempfile.TemporaryDirectory() as tmpdir:
        yield Path(tmpdir)


@pytest.fixture
def test_settings(temp_dir: Path) -> Settings:
    return Settings(
        redis_url="redis://localhost:6379",
        storage_provider="local",
        local_storage_path=str(temp_dir / "datalake"),
        wal_path=str(temp_dir / "wal"),
        dlq_path=str(temp_dir / "dlq"),
        buffer_max_size=10,
        buffer_flush_interval=1.0,
        retry_max_attempts=3,
        retry_base_delay=0.1,
        retry_max_delay=1.0,
        dedup_ttl_hours=1,
        dedup_max_size=1000,
        _env_file=None,
    )


@pytest.fixture
def mock_storage():
    storage = MagicMock()
    storage.health_check = AsyncMock(return_value=True)
    storage.append_events_batch = AsyncMock()
    storage.close = AsyncMock()
    return storage


@pytest.fixture
def mock_mounter_engine():
    engine = MagicMock()
    engine._dispatch_event = AsyncMock()
    return engine


@pytest.fixture
def consumer(test_settings, mock_storage, mock_mounter_engine):
    return DatalakeConsumer(
        settings=test_settings,
        storage=mock_storage,
        mounter_engine=mock_mounter_engine,
    )


class TestDatalakeConsumer:
    """Tests for DatalakeConsumer class."""

    def test_initialization(self, consumer, test_settings, mock_storage):
        assert consumer.settings == test_settings
        assert consumer.storage == mock_storage
        assert consumer.buffer is not None
        assert consumer.retry_handler is not None
        assert consumer.deduplicator is not None

    def test_streams_property(self, consumer, test_settings):
        streams = consumer._streams

        assert len(streams) == len(EventCategory)
        for cat in EventCategory:
            expected_key = f"{test_settings.redis_stream_prefix}:{cat.value}"
            assert expected_key in streams
            assert streams[expected_key] == cat.value

    def test_initial_metrics(self, consumer):
        metrics = consumer.metrics

        assert metrics["events_received"] == 0
        assert metrics["events_persisted"] == 0
        assert metrics["events_duplicates"] == 0
        assert metrics["errors"] == 0
        assert "buffer_size" in metrics
        assert "dedup_size" in metrics
        assert "retry" in metrics


class TestProcessMessage:
    """Tests for _process_message method."""

    @pytest.fixture
    async def consumer_with_buffer_started(self, consumer):
        consumer.redis = MagicMock()
        consumer.redis.xack = AsyncMock()
        await consumer.buffer.start()
        await consumer.deduplicator.start()
        yield consumer
        await consumer.buffer.stop()
        await consumer.deduplicator.stop()

    @pytest.mark.asyncio
    async def test_process_valid_message(self, consumer_with_buffer_started):
        consumer = consumer_with_buffer_started
        data = {
            "eventId": "test-123",
            "type": "UserRegistered",
            "timestamp": "1711357800000",
            "data": '{"userId": "user-1"}',
        }

        await consumer._process_message("stream:users", "users", "1-0", data)

        assert consumer._events_received == 1
        assert consumer.buffer.size == 1

    @pytest.mark.asyncio
    async def test_process_duplicate_message(self, consumer_with_buffer_started):
        consumer = consumer_with_buffer_started
        data = {
            "eventId": "dup-123",
            "type": "UserRegistered",
            "timestamp": "1711357800000",
            "data": "{}",
        }

        await consumer._process_message("stream:users", "users", "1-0", data)
        await consumer._process_message("stream:users", "users", "2-0", data)

        assert consumer._events_received == 2
        assert consumer._events_duplicates == 1
        assert consumer.buffer.size == 1

    @pytest.mark.asyncio
    async def test_process_message_increments_received(self, consumer_with_buffer_started):
        consumer = consumer_with_buffer_started
        data = {"eventId": "event-1", "type": "Test", "timestamp": "1711357800000", "data": "{}"}

        await consumer._process_message("stream:users", "users", "1-0", data)

        assert consumer._events_received == 1

    @pytest.mark.asyncio
    async def test_duplicate_message_is_acked(self, consumer_with_buffer_started):
        consumer = consumer_with_buffer_started
        data = {"eventId": "dup-event", "type": "Test", "timestamp": "1711357800000", "data": "{}"}

        await consumer._process_message("stream:users", "users", "1-0", data)
        await consumer._process_message("stream:users", "users", "2-0", data)

        consumer.redis.xack.assert_called()


class TestOnFlush:
    """Tests for _on_flush callback."""

    @pytest.fixture
    def consumer_with_mocked_redis(self, consumer):
        consumer.redis = MagicMock()
        consumer.redis.xack = AsyncMock()
        return consumer

    @pytest.mark.asyncio
    async def test_on_flush_persists_events(self, consumer_with_mocked_redis, mock_storage):
        consumer = consumer_with_mocked_redis
        event_lines = ['{"eventId": "1"}', '{"eventId": "2"}']
        pending_acks = [("stream:users", "1-0"), ("stream:users", "2-0")]

        await consumer._on_flush("users", datetime.utcnow(), event_lines, pending_acks)

        mock_storage.append_events_batch.assert_called_once()
        assert consumer._events_persisted == 2

    @pytest.mark.asyncio
    async def test_on_flush_acks_messages(self, consumer_with_mocked_redis):
        consumer = consumer_with_mocked_redis
        pending_acks = [("stream:users", "1-0"), ("stream:users", "2-0")]

        await consumer._on_flush("users", datetime.utcnow(), ["{}", "{}"], pending_acks)

        assert consumer.redis.xack.call_count == 2

    @pytest.mark.asyncio
    async def test_on_flush_dispatches_to_mounters(
        self, consumer_with_mocked_redis, mock_mounter_engine
    ):
        consumer = consumer_with_mocked_redis
        event_lines = ['{"eventId": "1", "type": "Test"}']

        await consumer._on_flush("users", datetime.utcnow(), event_lines, [])

        mock_mounter_engine._dispatch_event.assert_called_once()

    @pytest.mark.asyncio
    async def test_on_flush_error_adds_to_retry(self, consumer_with_mocked_redis, mock_storage):
        consumer = consumer_with_mocked_redis
        mock_storage.append_events_batch.side_effect = Exception("Storage error")
        event_lines = ['{"eventId": "1"}']

        with pytest.raises(Exception):
            await consumer._on_flush("users", datetime.utcnow(), event_lines, [])

        assert consumer._errors == 1


class TestRetryEvent:
    """Tests for _retry_event callback."""

    @pytest.mark.asyncio
    async def test_retry_event_persists(self, consumer, mock_storage):
        event_line = '{"eventId": "retry-1"}'

        await consumer._retry_event("users", datetime.utcnow(), event_line)

        mock_storage.append_events_batch.assert_called_once()
        assert consumer._events_persisted == 1


class TestStop:
    """Tests for stop method."""

    @pytest.mark.asyncio
    async def test_stop_closes_redis(self, consumer):
        consumer.redis = MagicMock()
        consumer.redis.close = AsyncMock()
        consumer.buffer._running = False
        consumer.deduplicator._running = False

        await consumer.stop()

        consumer.redis.close.assert_called_once()

    @pytest.mark.asyncio
    async def test_stop_closes_storage(self, consumer, mock_storage):
        consumer.buffer._running = False
        consumer.deduplicator._running = False

        await consumer.stop()

        mock_storage.close.assert_called_once()

    @pytest.mark.asyncio
    async def test_stop_sets_running_false(self, consumer):
        consumer._running = True
        consumer.buffer._running = False
        consumer.deduplicator._running = False

        await consumer.stop()

        assert consumer._running is False


class TestEnsureConsumerGroups:
    """Tests for _ensuREDACTED method."""

    @pytest.mark.asyncio
    async def test_creates_consumer_groups(self, consumer):
        consumer.redis = MagicMock()
        consumer.redis.xgroup_create = AsyncMock()

        await consumer._ensuREDACTED()

        call_count = len(EventCategory)
        assert consumer.redis.xgroup_create.call_count == call_count

    @pytest.mark.asyncio
    async def test_handles_existing_groups(self, consumer):
        import redis.asyncio as aioredis

        consumer.redis = MagicMock()
        consumer.redis.xgroup_create = AsyncMock(side_effect=aioredis.ResponseError("BUSYGROUP"))

        await consumer._ensuREDACTED()


class TestMetricsTracking:
    """Tests for metrics tracking."""

    @pytest.fixture
    async def consumer_with_buffer_started(self, consumer):
        consumer.redis = MagicMock()
        consumer.redis.xack = AsyncMock()
        await consumer.buffer.start()
        await consumer.deduplicator.start()
        yield consumer
        await consumer.buffer.stop()
        await consumer.deduplicator.stop()

    @pytest.mark.asyncio
    async def test_tracks_received_events(self, consumer_with_buffer_started):
        consumer = consumer_with_buffer_started
        data = {"eventId": "event-1", "type": "Test", "timestamp": "1711357800000", "data": "{}"}

        await consumer._process_message("stream:users", "users", "1-0", data)

        assert consumer.metrics["events_received"] == 1

    @pytest.mark.asyncio
    async def test_tracks_duplicate_events(self, consumer_with_buffer_started):
        consumer = consumer_with_buffer_started
        data = {"eventId": "dup-1", "type": "Test", "timestamp": "1711357800000", "data": "{}"}

        await consumer._process_message("stream:users", "users", "1-0", data)
        await consumer._process_message("stream:users", "users", "2-0", data)

        assert consumer.metrics["events_duplicates"] == 1

    @pytest.mark.asyncio
    async def test_tracks_buffer_size(self, consumer_with_buffer_started):
        consumer = consumer_with_buffer_started
        data = {"eventId": "event-1", "type": "Test", "timestamp": "1711357800000", "data": "{}"}

        await consumer._process_message("stream:users", "users", "1-0", data)

        assert consumer.metrics["buffer_size"] == 1

    @pytest.mark.asyncio
    async def test_tracks_dedup_size(self, consumer_with_buffer_started):
        consumer = consumer_with_buffer_started
        data = {"eventId": "event-1", "type": "Test", "timestamp": "1711357800000", "data": "{}"}

        await consumer._process_message("stream:users", "users", "1-0", data)

        assert consumer.metrics["dedup_size"] == 1
