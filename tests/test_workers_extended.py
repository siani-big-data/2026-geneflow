"""Extended tests covering BaseWorker lifecycle and TraceWorker storage paths."""

import asyncio
from unittest.mock import AsyncMock, MagicMock

import pytest

from src.config import Settings
from src.models import WorkerStatus
from src.workers import TraceWorker
from src.workers.base import BaseWorker


class _CountingWorker(BaseWorker):
    """BaseWorker subclass that counts processed jobs."""

    def __init__(self, redis, publisher, settings, should_fail: bool = False):
        super().__init__(redis, publisher, settings)
        self.processed: list[tuple[str, dict]] = []
        self._should_fail = should_fail

    @property
    def name(self) -> str:
        return "Counter"

    @property
    def stream_name(self) -> str:
        return "test:stream"

    async def process_job(self, job_id: str, job_data: dict) -> None:
        if self._should_fail:
            raise RuntimeError("forced failure")
        self.processed.append((job_id, job_data))


@pytest.fixture
def settings():
    return Settings(eventbus_enabled=False)


@pytest.fixture
def mock_publisher():
    return AsyncMock()


def _make_redis(messages=None):
    redis = AsyncMock()
    redis.xgroup_create = AsyncMock()
    redis.xack = AsyncMock()
    redis.xreadgroup = AsyncMock(return_value=messages or [])
    return redis


class TestBaseWorkerLifecycle:
    @pytest.mark.asyncio
    async def test_ensuREDACTED(self, settings, mock_publisher):
        redis = _make_redis()
        worker = _CountingWorker(redis, mock_publisher, settings)

        await worker._ensuREDACTED()

        redis.xgroup_create.assert_called_once()

    @pytest.mark.asyncio
    async def test_ensuREDACTED(self, settings, mock_publisher):
        redis = _make_redis()
        redis.xgroup_create.side_effect = Exception("BUSYGROUP Consumer Group exists")
        worker = _CountingWorker(redis, mock_publisher, settings)

        await worker._ensuREDACTED()

    @pytest.mark.asyncio
    async def test_ensuREDACTED(self, settings, mock_publisher):
        redis = _make_redis()
        redis.xgroup_create.side_effect = RuntimeError("network failure")
        worker = _CountingWorker(redis, mock_publisher, settings)

        with pytest.raises(RuntimeError):
            await worker._ensuREDACTED()

    @pytest.mark.asyncio
    async def test_process_message_increments_metrics(self, settings, mock_publisher):
        redis = _make_redis()
        worker = _CountingWorker(redis, mock_publisher, settings)

        await worker._process_message("1-0", {b"data": b'{"traceId": "t"}'})

        assert worker.metrics.jobsProcessed == 1
        assert worker.metrics.lastJobAt is not None
        redis.xack.assert_called_once()
        assert worker.processed[0][0] == "1-0"
        assert worker.processed[0][1] == {"traceId": "t"}

    @pytest.mark.asyncio
    async def test_process_message_failuREDACTED(self, settings, mock_publisher):
        redis = _make_redis()
        worker = _CountingWorker(redis, mock_publisher, settings, should_fail=True)

        await worker._process_message("1-0", {b"data": b'{"x": 1}'})

        assert worker.metrics.jobsFailed == 1
        assert worker.metrics.jobsProcessed == 0
        redis.xack.assert_called_once()

    @pytest.mark.asyncio
    async def test_process_message_updates_average_time(self, settings, mock_publisher):
        redis = _make_redis()
        worker = _CountingWorker(redis, mock_publisher, settings)

        await worker._process_message("1-0", {b"data": b'{"a": 1}'})
        await worker._process_message("2-0", {b"data": b'{"b": 2}'})

        assert worker.metrics.jobsProcessed == 2
        assert worker.metrics.averageProcessingTimeMs >= 0.0

    @pytest.mark.asyncio
    async def test_stop_transitions_status(self, settings, mock_publisher):
        redis = _make_redis()
        worker = _CountingWorker(redis, mock_publisher, settings)
        worker._running = True
        worker._status = WorkerStatus.RUNNING

        await worker.stop()

        assert worker._running is False
        assert worker.status == WorkerStatus.STOPPED

    @pytest.mark.asyncio
    async def test_consume_loop_processes_messages(self, settings, mock_publisher):
        messages = [(b"test:stream", [(b"1-0", {b"data": b'{"k": "v"}'})])]
        redis = _make_redis(messages)
        worker = _CountingWorker(redis, mock_publisher, settings)

        async def stop_after_first(*args, **kwargs):
            worker._running = False
            return messages

        redis.xreadgroup.side_effect = stop_after_first
        worker._running = True

        await worker._consume_loop()

        assert worker.processed[0][1] == {"k": "v"}

    @pytest.mark.asyncio
    async def test_consume_loop_handles_empty(self, settings, mock_publisher):
        redis = _make_redis()

        async def empty(*args, **kwargs):
            worker._running = False
            return []

        worker = _CountingWorker(redis, mock_publisher, settings)
        redis.xreadgroup.side_effect = empty
        worker._running = True

        await worker._consume_loop()
        assert worker.metrics.jobsProcessed == 0

    @pytest.mark.asyncio
    async def test_consume_loop_swallows_errors(self, settings, mock_publisher):
        redis = _make_redis()
        worker = _CountingWorker(redis, mock_publisher, settings)
        call_count = {"n": 0}

        async def maybe_fail(*args, **kwargs):
            call_count["n"] += 1
            if call_count["n"] == 1:
                raise RuntimeError("transient")
            worker._running = False
            return []

        redis.xreadgroup.side_effect = maybe_fail
        worker._running = True

        async def fast_sleep(_):
            return None

        import src.workers.base as base_module

        original_sleep = base_module.asyncio.sleep
        base_module.asyncio.sleep = fast_sleep
        try:
            await worker._consume_loop()
        finally:
            base_module.asyncio.sleep = original_sleep

        assert call_count["n"] >= 2

    @pytest.mark.asyncio
    async def test_consume_loop_handles_cancellation(self, settings, mock_publisher):
        redis = _make_redis()
        redis.xreadgroup.side_effect = asyncio.CancelledError()
        worker = _CountingWorker(redis, mock_publisher, settings)
        worker._running = True

        await worker._consume_loop()


class TestTraceWorkerStorage:
    @pytest.fixture
    def settings(self):
        return Settings(eventbus_enabled=False)

    @pytest.fixture
    def storage(self):
        storage = MagicMock()
        storage.get = AsyncMock(return_value=b">seq1\nACGTACGT\n")
        storage.put = AsyncMock(return_value="path")
        return storage

    @pytest.mark.asyncio
    async def test_process_job_with_storage_stores_result(self, settings, storage):
        publisher = AsyncMock()
        worker = TraceWorker(AsyncMock(), publisher, settings, storage_provider=storage)

        await worker.process_job(
            "msg-1",
            {
                "traceId": "t-1",
                "studyId": "s-1",
                "fileName": "test.fasta",
                "storagePath": "test.fasta",
                "format": "fasta",
            },
        )

        assert storage.put.await_count >= 1
        publisher.publish.assert_called()
        event = publisher.publish.call_args[0][0]
        assert event.traceId == "t-1"

    @pytest.mark.asyncio
    async def test_process_job_publishes_failuREDACTED(self, settings):
        publisher = AsyncMock()
        storage = MagicMock()
        storage.get = AsyncMock(side_effect=FileNotFoundError("missing"))
        worker = TraceWorker(AsyncMock(), publisher, settings, storage_provider=storage)

        with pytest.raises(FileNotFoundError):
            await worker.process_job(
                "msg-1",
                {
                    "traceId": "t-2",
                    "studyId": "s-1",
                    "fileName": "x.fasta",
                    "storagePath": "x.fasta",
                    "format": "fasta",
                },
            )

        published = publisher.publish.call_args[0][0]
        assert published.traceId == "t-2"
        assert "missing" in published.error

    @pytest.mark.asyncio
    async def test_fetch_trace_data_falls_back_to_filesystem(self, settings, tmp_path):
        publisher = AsyncMock()
        worker = TraceWorker(AsyncMock(), publisher, settings, storage_provider=None)

        file_path = tmp_path / "x.fasta"
        file_path.write_bytes(b">a\nACGT\n")

        from src.models import TraceFormat, TraceProcessingJob

        job = TraceProcessingJob(
            traceId="t",
            studyId="s",
            fileName="x.fasta",
            storagePath=str(file_path),
            format=TraceFormat.FASTA,
        )

        data = await worker._fetch_trace_data(job)
        assert data == b">a\nACGT\n"

    @pytest.mark.asyncio
    async def test_fetch_trace_data_missing_file_raises_value_error(self, settings):
        publisher = AsyncMock()
        worker = TraceWorker(AsyncMock(), publisher, settings, storage_provider=None)

        from src.models import TraceFormat, TraceProcessingJob

        job = TraceProcessingJob(
            traceId="t",
            studyId="s",
            fileName="nope",
            storagePath="/nonexistent/path/x.fasta",
            format=TraceFormat.FASTA,
        )

        with pytest.raises(ValueError, match="not found"):
            await worker._fetch_trace_data(job)
