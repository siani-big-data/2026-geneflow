"""Branch coverage tests for src/consumer/consumer.py - covering missing lines/branches."""

import tempfile
from datetime import datetime
from pathlib import Path
from unittest.mock import AsyncMock, MagicMock, patch

import pytest
import redis.asyncio as aioredis

from src.config import Settings
from src.consumer import DatalakeConsumer


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
        retry_base_delay=0.05,
        retry_max_delay=0.2,
        dedup_ttl_hours=1,
        dedup_max_size=100,
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
def consumer(test_settings, mock_storage):
    return DatalakeConsumer(settings=test_settings, storage=mock_storage)


class TestStartFailureBranches:
    """Tests for start() unhealthy storage and error paths (lines 101-112)."""

    @pytest.mark.asyncio
    async def test_start_raises_when_storage_unhealthy(self, consumer, mock_storage):
        """Cover line 103-104: storage.health_check returns False -> RuntimeError."""
        mock_storage.health_check.return_value = False

        with patch("redis.asyncio.from_url") as mock_from_url:
            mock_redis = MagicMock()
            mock_redis.close = AsyncMock()
            mock_from_url.return_value = mock_redis

            with pytest.raises(RuntimeError, match="Storage health check failed"):
                await consumer.start()


class TestEnsureConsumerGroupsBranch:
    """Tests for _ensuREDACTED raising non-BUSYGROUP errors (line 143)."""

    @pytest.mark.asyncio
    async def test_non_busygroup_response_error_raises(self, consumer):
        """Cover line 142-143: non-BUSYGROUP error reraises."""
        consumer.redis = MagicMock()
        consumer.redis.xgroup_create = AsyncMock(
            side_effect=aioredis.ResponseError("ERR unknown error")
        )

        with pytest.raises(aioredis.ResponseError):
            await consumer._ensuREDACTED()


class TestConsumeLoop:
    """Tests for the main consume loop (lines 145-176)."""

    @pytest.mark.asyncio
    async def test_consume_loop_processes_messages(self, consumer):
        """Cover lines 151-167: read messages and dispatch to _process_message."""
        consumer.redis = MagicMock()
        consumer.redis.xack = AsyncMock()

        # Return a batch then stop the loop
        call_count = {"n": 0}

        async def fake_xreadgroup(**kwargs):
            call_count["n"] += 1
            if call_count["n"] == 1:
                return [
                    (
                        "geneflow-datalake:events:users",
                        [
                            (
                                "1-0",
                                {
                                    "eventId": "evt-1",
                                    "type": "UserRegistered",
                                    "timestamp": "1711357800000",
                                    "data": "{}",
                                },
                            )
                        ],
                    )
                ]
            consumer._running = False
            return []

        consumer.redis.xreadgroup = fake_xreadgroup

        await consumer.buffer.start()
        await consumer.deduplicator.start()
        try:
            consumer._running = True
            await consumer._consume_loop()
        finally:
            await consumer.buffer.stop()
            await consumer.deduplicator.stop()

        assert consumer._events_received >= 1

    @pytest.mark.asyncio
    async def test_consume_loop_empty_messages_continues(self, consumer):
        """Cover line 161-162: empty messages -> continue (loops without processing)."""
        consumer.redis = MagicMock()

        call_count = {"n": 0}

        async def fake_xreadgroup(**kwargs):
            call_count["n"] += 1
            if call_count["n"] >= 2:
                consumer._running = False
            return []

        consumer.redis.xreadgroup = fake_xreadgroup

        consumer._running = True
        await consumer._consume_loop()

        # Should have looped at least twice without crashing
        assert call_count["n"] >= 2

    @pytest.mark.asyncio
    async def test_consume_loop_handles_connection_error(self, consumer):
        """Cover lines 169-172: redis ConnectionError increments errors and sleeps."""
        consumer.redis = MagicMock()

        call_count = {"n": 0}

        async def fake_xreadgroup(**kwargs):
            call_count["n"] += 1
            if call_count["n"] == 1:
                raise aioredis.ConnectionError("connection lost")
            consumer._running = False
            return []

        consumer.redis.xreadgroup = fake_xreadgroup

        with patch("asyncio.sleep", new_callable=AsyncMock) as mock_sleep:
            consumer._running = True
            await consumer._consume_loop()

        assert consumer._errors >= 1
        assert mock_sleep.called

    @pytest.mark.asyncio
    async def test_consume_loop_handles_generic_exception(self, consumer):
        """Cover lines 173-176: generic Exception in loop."""
        consumer.redis = MagicMock()

        call_count = {"n": 0}

        async def fake_xreadgroup(**kwargs):
            call_count["n"] += 1
            if call_count["n"] == 1:
                raise RuntimeError("boom")
            consumer._running = False
            return []

        consumer.redis.xreadgroup = fake_xreadgroup

        with patch("asyncio.sleep", new_callable=AsyncMock):
            consumer._running = True
            await consumer._consume_loop()

        assert consumer._errors >= 1


class TestProcessMessageErrorBranch:
    """Tests for _process_message error path (lines 219-226)."""

    @pytest.mark.asyncio
    async def test_process_message_handles_parse_exception(self, consumer):
        """Cover lines 219-226: exception in parse increments error counter."""
        consumer.redis = MagicMock()
        consumer.redis.xack = AsyncMock()
        # Force the parser to raise
        consumer._message_parser = MagicMock()
        consumer._message_parser.parse_redis_message = MagicMock(
            side_effect=RuntimeError("parse failed")
        )

        await consumer.buffer.start()
        await consumer.deduplicator.start()
        try:
            await consumer._process_message(
                "stream:users",
                "users",
                "1-0",
                {"eventId": "x"},
            )
        finally:
            await consumer.buffer.stop()
            await consumer.deduplicator.stop()

        assert consumer._errors >= 1


class TestOnFlushMounterErrorBranch:
    """Tests for _on_flush mounter dispatch errors (lines 246-247)."""

    @pytest.mark.asyncio
    async def test_on_flush_handles_mounter_dispatch_exception(self, test_settings, mock_storage):
        """Cover lines 246-247: mounter dispatch raises exception, gets logged but not raised."""
        engine = MagicMock()
        engine._dispatch_event = AsyncMock(side_effect=RuntimeError("dispatch failed"))

        consumer = DatalakeConsumer(
            settings=test_settings, storage=mock_storage, mounter_engine=engine
        )
        consumer.redis = MagicMock()
        consumer.redis.xack = AsyncMock()

        event_lines = ['{"eventId": "1", "type": "X"}']
        # Should NOT raise even though mounter raises
        await consumer._on_flush("users", datetime.utcnow(), event_lines, [])

        assert consumer._events_persisted == 1

    @pytest.mark.asyncio
    async def test_on_flush_skips_mounter_when_no_engine(self, consumer, mock_storage):
        """Cover branch 240->253: no mounter_engine -> skip dispatch loop."""
        consumer.mounter_engine = None
        consumer.redis = MagicMock()
        consumer.redis.xack = AsyncMock()

        event_lines = ['{"eventId": "1", "type": "X"}']
        await consumer._on_flush("users", datetime.utcnow(), event_lines, [])

        assert consumer._events_persisted == 1
