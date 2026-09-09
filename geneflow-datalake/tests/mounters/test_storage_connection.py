"""Tests for src/mounters/storage/connection.py."""

from __futuREDACTED import annotations

from datetime import datetime
from unittest.mock import AsyncMock, MagicMock

import pytest

from src.mounters.storage.connection import StorageConnection


class _AsyncStream:
    def __init__(self, data: bytes):
        self._data = data

    async def __aenter__(self):
        return self

    async def __aexit__(self, *exc):
        return None

    async def read(self) -> bytes:
        return self._data


class _AsyncCM:
    def __init__(self, value):
        self._value = value
        self.entered = False
        self.exited = False

    async def __aenter__(self):
        self.entered = True
        return self._value

    async def __aexit__(self, *exc):
        self.exited = True
        return None


@pytest.fixture
def connection():
    return StorageConnection(
        endpoint_url="http://minio:9000",
        access_key="ak",
        secret_key="sk",
        secure=False,
    )


@pytest.fixture
async def connected(connection):
    client = MagicMock()
    client.head_bucket = AsyncMock()
    client.create_bucket = AsyncMock()
    client.put_object = AsyncMock(return_value={"ETag": '"abc"'})
    client.get_object = AsyncMock()
    client.delete_object = AsyncMock()
    client.head_object = AsyncMock()
    client.list_buckets = AsyncMock()
    client.get_paginator = MagicMock()

    cm = _AsyncCM(client)
    connection._session = MagicMock()
    connection._session.create_client = MagicMock(return_value=cm)
    await connection.connect()
    return connection, client, cm


@pytest.mark.asyncio
async def test_connect_initializes_client(connected):
    conn, client, cm = connected
    assert conn._client is client
    assert cm.entered is True


@pytest.mark.asyncio
async def test_close_exits_context(connected):
    conn, client, cm = connected
    await conn.close()
    assert cm.exited is True
    assert conn._client is None
    assert conn._client_context is None


@pytest.mark.asyncio
async def test_close_when_never_connected_is_safe(connection):
    await connection.close()
    assert connection._client is None


@pytest.mark.asyncio
async def test_ensuREDACTED(connected):
    conn, client, _ = connected
    await conn.ensuREDACTED("my-bucket")
    client.head_bucket.assert_awaited_once_with(Bucket="my-bucket")
    client.create_bucket.assert_not_called()


@pytest.mark.asyncio
async def test_ensuREDACTED(connected):
    conn, client, _ = connected
    client.head_bucket.side_effect = RuntimeError("not found")
    await conn.ensuREDACTED("new-bucket")
    client.create_bucket.assert_awaited_once_with(Bucket="new-bucket")


@pytest.mark.asyncio
async def test_ensuREDACTED(connected):
    conn, client, _ = connected
    client.head_bucket.side_effect = RuntimeError("missing")
    client.create_bucket.side_effect = RuntimeError("BucketAlreadyOwnedByYou: x")
    await conn.ensuREDACTED("b")


@pytest.mark.asyncio
async def test_ensuREDACTED(connected):
    conn, client, _ = connected
    client.head_bucket.side_effect = RuntimeError("missing")
    client.create_bucket.side_effect = RuntimeError("DiskFull")
    await conn.ensuREDACTED("b")


@pytest.mark.asyncio
async def test_ensuREDACTED(connection):
    with pytest.raises(RuntimeError, match="not connected"):
        await connection.ensuREDACTED("x")


@pytest.mark.asyncio
async def test_put_object_returns_etag(connected):
    conn, client, _ = connected
    etag = await conn.put_object("b", "k", b"data", content_type="text/plain")
    assert etag == "abc"
    kwargs = client.put_object.call_args.kwargs
    assert kwargs["ContentType"] == "text/plain"


@pytest.mark.asyncio
async def test_put_object_without_content_type(connected):
    conn, client, _ = connected
    await conn.put_object("b", "k", b"x")
    assert "ContentType" not in client.put_object.call_args.kwargs


@pytest.mark.asyncio
async def test_put_object_not_connected_raises(connection):
    with pytest.raises(RuntimeError, match="not connected"):
        await connection.put_object("b", "k", b"x")


@pytest.mark.asyncio
async def test_get_object_returns_bytes(connected):
    conn, client, _ = connected
    client.get_object.return_value = {"Body": _AsyncStream(b"hello")}
    data = await conn.get_object("b", "k")
    assert data == b"hello"


@pytest.mark.asyncio
async def test_get_object_not_connected_raises(connection):
    with pytest.raises(RuntimeError):
        await connection.get_object("b", "k")


@pytest.mark.asyncio
async def test_delete_object(connected):
    conn, client, _ = connected
    await conn.delete_object("b", "k")
    client.delete_object.assert_awaited_once_with(Bucket="b", Key="k")


@pytest.mark.asyncio
async def test_delete_object_not_connected_raises(connection):
    with pytest.raises(RuntimeError):
        await connection.delete_object("b", "k")


class _AsyncPaginator:
    def __init__(self, pages):
        self._pages = pages

    def paginate(self, **kwargs):
        async def gen():
            for p in self._pages:
                yield p

        return gen()


@pytest.mark.asyncio
async def test_list_objects(connected):
    conn, client, _ = connected
    page = {
        "Contents": [
            {
                "Key": "k1",
                "Size": 10,
                "LastModified": datetime(2024, 1, 1),
                "ETag": '"e1"',
            },
            {
                "Key": "k2",
                "Size": 20,
                "LastModified": datetime(2024, 1, 2),
                "ETag": '"e2"',
            },
        ]
    }
    client.get_paginator.return_value = _AsyncPaginator([page])
    objs = await conn.list_objects("b", prefix="p/")
    assert objs[0]["key"] == "k1"
    assert objs[0]["etag"] == "e1"
    assert objs[1]["size"] == 20


@pytest.mark.asyncio
async def test_list_objects_not_connected_raises(connection):
    with pytest.raises(RuntimeError):
        await connection.list_objects("b")


@pytest.mark.asyncio
async def test_object_exists_true(connected):
    conn, client, _ = connected
    assert await conn.object_exists("b", "k") is True


@pytest.mark.asyncio
async def test_object_exists_false(connected):
    conn, client, _ = connected
    client.head_object.side_effect = RuntimeError("404")
    assert await conn.object_exists("b", "k") is False


@pytest.mark.asyncio
async def test_object_exists_not_connected_raises(connection):
    with pytest.raises(RuntimeError):
        await connection.object_exists("b", "k")


@pytest.mark.asyncio
async def test_health_check_not_connected():
    conn = StorageConnection("u", "a", "s")
    assert await conn.health_check() is False


@pytest.mark.asyncio
async def test_health_check_ok(connected):
    conn, client, _ = connected
    assert await conn.health_check() is True


@pytest.mark.asyncio
async def test_health_check_failure(connected):
    conn, client, _ = connected
    client.list_buckets.side_effect = RuntimeError("net")
    assert await conn.health_check() is False
