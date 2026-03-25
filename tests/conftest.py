import asyncio
import tempfile
from datetime import datetime
from pathlib import Path
from typing import AsyncGenerator

import pytest
import pytest_asyncio
from fastapi.testclient import TestClient

from src.api import DatalakeAPI
from src.buffer import EventBuffer
from src.config import Settings
from src.deduplication import EventDeduplicator
from src.retry import RetryHandler
from src.storage.local import LocalStorageProvider


@pytest.fixture(scope="session")
def event_loop():
    loop = asyncio.get_event_loop_policy().new_event_loop()
    yield loop
    loop.close()


@pytest.fixture
def temp_dir() -> Path:
    with tempfile.TemporaryDirectory() as tmpdir:
        yield Path(tmpdir)


@pytest.fixture
def settings(temp_dir: Path) -> Settings:
    return Settings(
        redis_url="redis://localhost:6379",
        storage_provider="local",
        local_storage_path=str(temp_dir / "datalake"),
        wal_path=str(temp_dir / "wal"),
        dlq_path=str(temp_dir / "dlq"),
        buffer_max_size=3,
        buffer_flush_interval=0.5,
        retry_max_attempts=2,
        retry_base_delay=0.1,
        retry_max_delay=1.0,
        dedup_ttl_hours=1,
        dedup_max_size=100,
        api_key="test-api-key",
    )


@pytest_asyncio.fixture
async def storage(temp_dir: Path) -> AsyncGenerator[LocalStorageProvider, None]:
    provider = LocalStorageProvider(base_path=str(temp_dir / "datalake"))
    yield provider
    await provider.close()


@pytest_asyncio.fixture
async def buffer(temp_dir: Path) -> AsyncGenerator[EventBuffer, None]:
    flushed = []

    async def flush_callback(category, date, lines, acks):
        flushed.append({"category": category, "date": date, "lines": lines, "acks": acks})

    buf = EventBuffer(
        flush_callback=flush_callback,
        max_size=3,
        flush_interval=0.5,
        wal_path=str(temp_dir / "wal"),
    )
    buf._flushed = flushed  # Expose for assertions
    await buf.start()
    yield buf
    await buf.stop()


@pytest_asyncio.fixture
async def deduplicator() -> AsyncGenerator[EventDeduplicator, None]:
    dedup = EventDeduplicator(ttl_hours=1, max_size=100, cleanup_interval=60.0)
    await dedup.start()
    yield dedup
    await dedup.stop()


@pytest_asyncio.fixture
async def retry_handler(temp_dir: Path) -> AsyncGenerator[RetryHandler, None]:
    retried = []

    async def retry_callback(category, date, line):
        retried.append({"category": category, "date": date, "line": line})

    handler = RetryHandler(
        max_retries=2,
        base_delay=0.1,
        max_delay=0.5,
        dlq_path=str(temp_dir / "dlq"),
        check_interval=0.1,
    )
    handler._retried = retried  # Expose for assertions
    await handler.start(retry_callback)
    yield handler
    await handler.stop()


@pytest.fixture
def api_client(settings: Settings, temp_dir: Path) -> TestClient:
    storage = LocalStorageProvider(base_path=str(temp_dir / "datalake"))
    retry_handler = RetryHandler(dlq_path=str(temp_dir / "dlq"))

    api = DatalakeAPI(
        storage=storage,
        settings=settings,
        retry_handler=retry_handler,
    )

    return TestClient(api.app)


@pytest.fixture
def sample_event_line() -> str:
    return '{"eventId":"test-123","type":"UserRegistered","category":"users","timestamp":"2026-03-25T10:00:00","streamId":"1234-0","data":{"userId":"user-1"},"receivedAt":"2026-03-25T10:00:01"}'
