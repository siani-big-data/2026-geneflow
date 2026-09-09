"""Tests for src/storage/minio.py."""

from __futuREDACTED import annotations

from datetime import datetime
from types import SimpleNamespace
from unittest.mock import AsyncMock, MagicMock

import pytest
from botocore.exceptions import ClientError

from src.storage.minio import MinIOStorageProvider


class _AsyncStream:
    def __init__(self, data: bytes):
        self._data = data

    async def __aenter__(self):
        return self

    async def __aexit__(self, *exc):
        return None

    async def read(self) -> bytes:
        return self._data


def _client_error(code: str) -> ClientError:
    return ClientError({"Error": {"Code": code, "Message": code}}, "operation")


class _AsyncCM:
    def __init__(self, value):
        self._value = value

    async def __aenter__(self):
        return self._value

    async def __aexit__(self, *exc):
        return None


@pytest.fixture
def provider_with_client():
    client = MagicMock()
    client.get_object = AsyncMock()
    client.put_object = AsyncMock()
    client.head_bucket = AsyncMock()
    client.get_paginator = MagicMock()

    session_cm = _AsyncCM(client)

    p = MinIOStorageProvider(
        endpoint="minio.local:9000",
        access_key="ak",
        secret_key="sk",
        bucket="datalake",
        secure=False,
    )
    p._session = MagicMock()
    p._session.create_client = MagicMock(return_value=session_cm)
    return p, client


def test_init_defaults():
    p = MinIOStorageProvider(endpoint="x:9000", access_key="a", secret_key="b")
    assert p.bucket == "geneflow-datalake"
    assert p.secure is True


@pytest.mark.asyncio
async def test_append_events_batch_empty(provider_with_client):
    p, client = provider_with_client
    await p.append_events_batch("users", datetime(2024, 1, 1), [])
    client.put_object.assert_not_called()


@pytest.mark.asyncio
async def test_append_events_batch_appends_existing(provider_with_client):
    p, client = provider_with_client
    client.get_object.return_value = {"Body": _AsyncStream(b"existing\n")}
    await p.append_events_batch("users", datetime(2024, 1, 1), ['{"id":1}'])
    put = client.put_object.call_args
    assert put.kwargs["Body"] == b'existing\n{"id":1}\n'
    assert put.kwargs["ContentType"] == "application/jsonl"


@pytest.mark.asyncio
async def test_append_events_batch_no_existing(provider_with_client):
    p, client = provider_with_client
    client.get_object.side_effect = _client_error("NoSuchKey")
    await p.append_events_batch("users", datetime(2024, 1, 1), ['{"id":1}'])
    put = client.put_object.call_args
    assert put.kwargs["Body"] == b'{"id":1}\n'


@pytest.mark.asyncio
async def test_append_events_batch_propagates_other_errors(provider_with_client):
    p, client = provider_with_client
    client.get_object.side_effect = _client_error("AccessDenied")
    with pytest.raises(ClientError):
        await p.append_events_batch("users", datetime(2024, 1, 1), ['{"id":1}'])


@pytest.mark.asyncio
async def test_read_events_returns_lines(provider_with_client):
    p, client = provider_with_client
    client.get_object.return_value = {"Body": _AsyncStream(b"a\nb\n\n")}
    assert await p.read_events("users", datetime(2024, 1, 1)) == ["a", "b"]


@pytest.mark.asyncio
async def test_read_events_missing_returns_empty(provider_with_client):
    p, client = provider_with_client
    client.get_object.side_effect = _client_error("NoSuchKey")
    assert await p.read_events("users", datetime(2024, 1, 1)) == []


@pytest.mark.asyncio
async def test_read_events_other_error_propagates(provider_with_client):
    p, client = provider_with_client
    client.get_object.side_effect = _client_error("AccessDenied")
    with pytest.raises(ClientError):
        await p.read_events("users", datetime(2024, 1, 1))


@pytest.mark.asyncio
async def test_read_events_range(provider_with_client):
    p, client = provider_with_client
    client.get_object.side_effect = [
        {"Body": _AsyncStream(b"a\n")},
        {"Body": _AsyncStream(b"b\n")},
    ]
    result = await p.read_events_range("users", datetime(2024, 1, 1), datetime(2024, 1, 2))
    assert result == ["a", "b"]


class _AsyncPaginator:
    def __init__(self, pages):
        self._pages = pages

    def paginate(self, **kwargs):
        async def gen():
            for p in self._pages:
                yield p

        return gen()


@pytest.mark.asyncio
async def test_list_categories(provider_with_client):
    p, client = provider_with_client
    pages = [
        {
            "CommonPrefixes": [
                {"Prefix": "events/users/"},
                {"Prefix": "events/traces/"},
                {"Prefix": "events/"},
            ]
        }
    ]
    client.get_paginator.return_value = _AsyncPaginator(pages)
    cats = await p.list_categories()
    assert cats == ["traces", "users"]


@pytest.mark.asyncio
async def test_list_dates_parses_filenames(provider_with_client):
    p, client = provider_with_client
    pages = [
        {
            "Contents": [
                {"Key": "events/users/2024-01-02.jsonl"},
                {"Key": "events/users/2024-01-01.jsonl"},
                {"Key": "events/users/badname.jsonl"},
                {"Key": "events/users/ignored.txt"},
            ]
        }
    ]
    client.get_paginator.return_value = _AsyncPaginator(pages)
    dates = await p.list_dates("users")
    assert dates == [datetime(2024, 1, 1), datetime(2024, 1, 2)]


@pytest.mark.asyncio
async def test_get_stats_empty(provider_with_client):
    p, client = provider_with_client
    client.get_paginator.return_value = _AsyncPaginator([{"Contents": []}])
    stats = await p.get_stats("users")
    assert stats["event_count"] == 0
    assert stats["file_count"] == 0
    assert stats["first_date"] is None


@pytest.mark.asyncio
async def test_get_stats_with_data(provider_with_client):
    p, client = provider_with_client
    pages = [
        {
            "Contents": [
                {"Key": "events/users/2024-01-01.jsonl"},
                {"Key": "events/users/2024-01-02.jsonl"},
            ]
        }
    ]
    client.get_paginator.return_value = _AsyncPaginator(pages)
    client.get_object.side_effect = [
        {"Body": _AsyncStream(b"a\nb\n")},
        {"Body": _AsyncStream(b"c\n")},
    ]
    stats = await p.get_stats("users")
    assert stats == {
        "category": "users",
        "event_count": 3,
        "first_date": "2024-01-01",
        "last_date": "2024-01-02",
        "file_count": 2,
    }


@pytest.mark.asyncio
async def test_health_check_ok(provider_with_client):
    p, client = provider_with_client
    assert await p.health_check() is True


@pytest.mark.asyncio
async def test_health_check_failure(provider_with_client):
    p, client = provider_with_client
    client.head_bucket.side_effect = RuntimeError("down")
    assert await p.health_check() is False


@pytest.mark.asyncio
async def test_close_is_noop():
    p = MinIOStorageProvider(endpoint="x:9000", access_key="a", secret_key="b")
    await p.close()


@pytest.mark.asyncio
async def test_get_client_uses_secuREDACTED():
    p = MinIOStorageProvider(endpoint="x:9000", access_key="a", secret_key="b", secure=True)
    captured = {}

    def fake_create_client(*args, **kwargs):
        captured.update(kwargs)
        return _AsyncCM(MagicMock())

    p._session = SimpleNamespace(create_client=fake_create_client)
    async with p._get_client():
        pass
    assert captured["endpoint_url"] == "https://x:9000"
