"""Tests for HTTP and Supabase storage providers using httpx mocks."""

from unittest.mock import AsyncMock, MagicMock, patch

import httpx
import pytest

from src.storage.base import StorageError
from src.storage.http import HTTPStorageProvider
from src.storage.supabase import SupabaseStorageProvider


def _make_response(
    status_code: int = 200,
    content: bytes = b"",
    text: str = "",
    headers: dict | None = None,
    json_data=None,
) -> MagicMock:
    """Build a fake httpx.Response."""
    resp = MagicMock(spec=httpx.Response)
    resp.status_code = status_code
    resp.content = content
    resp.text = text
    resp.headers = headers or {}
    if json_data is not None:
        resp.json.return_value = json_data
    if status_code >= 400:
        resp.raise_for_status.side_effect = httpx.HTTPStatusError(
            "error", request=MagicMock(), response=resp
        )
    else:
        resp.raise_for_status.return_value = None
    return resp


def _client_with(method_name: str, response: MagicMock) -> MagicMock:
    """Build a mocked async client where the named method returns the response."""
    client = MagicMock()
    client.__aenter__ = AsyncMock(return_value=client)
    client.__aexit__ = AsyncMock(return_value=None)
    setattr(client, method_name, AsyncMock(return_value=response))
    return client


def _patch_async_client(client: MagicMock):
    return patch("httpx.AsyncClient", return_value=client)


class TestHTTPStorageProviderMocked:
    """HTTPStorageProvider behavior with mocked httpx."""

    @pytest.fixture
    def provider(self):
        return HTTPStorageProvider(timeout=5.0)

    @pytest.mark.asyncio
    async def test_get_success(self, provider):
        response = _make_response(200, content=b"hello", headers={"content-length": "5"})
        client = _client_with("get", response)

        with _patch_async_client(client):
            result = await provider.get("https://example.com/file.bin")

        assert result == b"hello"

    @pytest.mark.asyncio
    async def test_get_too_large(self, provider):
        response = _make_response(
            200,
            content=b"x" * 1000,
            headers={"content-length": str(provider._max_size + 1)},
        )
        client = _client_with("get", response)

        with _patch_async_client(client):
            with pytest.raises(StorageError, match="too large"):
                await provider.get("https://example.com/big.bin")

    @pytest.mark.asyncio
    async def test_get_404_raises_file_not_found(self, provider):
        response = _make_response(404)
        client = _client_with("get", response)

        with _patch_async_client(client):
            with pytest.raises(FileNotFoundError):
                await provider.get("https://example.com/missing")

    @pytest.mark.asyncio
    async def test_get_5xx_raises_storage_error(self, provider):
        response = _make_response(500)
        client = _client_with("get", response)

        with _patch_async_client(client):
            with pytest.raises(StorageError, match="HTTP error 500"):
                await provider.get("https://example.com/oops")

    @pytest.mark.asyncio
    async def test_get_request_error(self, provider):
        client = MagicMock()
        client.__aenter__ = AsyncMock(return_value=client)
        client.__aexit__ = AsyncMock(return_value=None)
        client.get = AsyncMock(side_effect=httpx.RequestError("boom"))

        with _patch_async_client(client):
            with pytest.raises(StorageError, match="Request failed"):
                await provider.get("https://example.com/x")

    @pytest.mark.asyncio
    async def test_exists_true(self, provider):
        response = _make_response(200)
        client = _client_with("head", response)

        with _patch_async_client(client):
            assert await provider.exists("https://example.com/file") is True

    @pytest.mark.asyncio
    async def test_exists_false_on_error(self, provider):
        client = MagicMock()
        client.__aenter__ = AsyncMock(return_value=client)
        client.__aexit__ = AsyncMock(return_value=None)
        client.head = AsyncMock(side_effect=httpx.RequestError("nope"))

        with _patch_async_client(client):
            assert await provider.exists("https://example.com/file") is False

    @pytest.mark.asyncio
    async def test_get_metadata(self, provider):
        response = _make_response(
            200,
            headers={
                "content-type": "text/plain",
                "content-length": "42",
                "last-modified": "today",
                "etag": "abc",
            },
        )
        client = _client_with("head", response)

        with _patch_async_client(client):
            meta = await provider.get_metadata("https://example.com/f")

        assert meta["content_type"] == "text/plain"
        assert meta["content_length"] == "42"
        assert meta["etag"] == "abc"

    @pytest.mark.asyncio
    async def test_get_metadata_failure(self, provider):
        response = _make_response(500)
        client = _client_with("head", response)

        with _patch_async_client(client):
            with pytest.raises(StorageError):
                await provider.get_metadata("https://example.com/f")

    @pytest.mark.asyncio
    async def test_validate_url_invalid_scheme_via_exists(self, provider):
        with pytest.raises(StorageError, match="Invalid URL scheme"):
            await provider.exists("ftp://example.com/file")


class TestSupabaseStorageProviderMocked:
    """SupabaseStorageProvider behavior with mocked httpx."""

    @pytest.fixture
    def provider(self):
        return SupabaseStorageProvider(
            url="https://test.supabase.co",
            key="key",
            bucket="bucket",
        )

    @pytest.mark.asyncio
    async def test_get_success(self, provider):
        response = _make_response(200, content=b"payload")
        client = _client_with("get", response)

        with _patch_async_client(client):
            data = await provider.get("file.bin")

        assert data == b"payload"

    @pytest.mark.asyncio
    async def test_get_404(self, provider):
        response = _make_response(404)
        client = _client_with("get", response)

        with _patch_async_client(client):
            with pytest.raises(FileNotFoundError):
                await provider.get("missing")

    @pytest.mark.asyncio
    async def test_get_other_error(self, provider):
        response = _make_response(500, text="boom")
        client = _client_with("get", response)

        with _patch_async_client(client):
            with pytest.raises(StorageError, match="Supabase error 500"):
                await provider.get("oops")

    @pytest.mark.asyncio
    async def test_put_new_object(self, provider):
        response = _make_response(200)
        client = _client_with("post", response)

        with _patch_async_client(client):
            url = await provider.put("a.bin", b"data", content_type="application/octet-stream")

        assert "bucket" in url and "a.bin" in url

    @pytest.mark.asyncio
    async def test_put_existing_falls_back_to_update(self, provider):
        post_response = _make_response(400, text="already exists")
        put_response = _make_response(200)

        client = MagicMock()
        client.__aenter__ = AsyncMock(return_value=client)
        client.__aexit__ = AsyncMock(return_value=None)
        client.post = AsyncMock(return_value=post_response)
        client.put = AsyncMock(return_value=put_response)

        with _patch_async_client(client):
            url = await provider.put("a.bin", b"data")

        assert client.put.await_count == 1
        assert "bucket" in url

    @pytest.mark.asyncio
    async def test_put_http_error(self, provider):
        response = _make_response(500, text="server error")
        client = _client_with("post", response)

        with _patch_async_client(client):
            with pytest.raises(StorageError, match="Supabase error 500"):
                await provider.put("a.bin", b"x")

    @pytest.mark.asyncio
    async def test_delete_success(self, provider):
        response = _make_response(200)
        client = _client_with("delete", response)

        with _patch_async_client(client):
            assert await provider.delete("a.bin") is True

    @pytest.mark.asyncio
    async def test_delete_not_found(self, provider):
        response = _make_response(404)
        client = _client_with("delete", response)

        with _patch_async_client(client):
            assert await provider.delete("a.bin") is False

    @pytest.mark.asyncio
    async def test_delete_error(self, provider):
        response = _make_response(500, text="x")
        client = _client_with("delete", response)

        with _patch_async_client(client):
            with pytest.raises(StorageError, match="Supabase error 500"):
                await provider.delete("a.bin")

    @pytest.mark.asyncio
    async def test_exists_true(self, provider):
        response = _make_response(200)
        client = _client_with("head", response)

        with _patch_async_client(client):
            assert await provider.exists("a.bin") is True

    @pytest.mark.asyncio
    async def test_exists_false_on_exception(self, provider):
        client = MagicMock()
        client.__aenter__ = AsyncMock(return_value=client)
        client.__aexit__ = AsyncMock(return_value=None)
        client.head = AsyncMock(side_effect=httpx.RequestError("x"))

        with _patch_async_client(client):
            assert await provider.exists("a.bin") is False

    @pytest.mark.asyncio
    async def test_list_returns_names(self, provider):
        response = _make_response(200, json_data=[{"name": "a"}, {"name": "b"}, {"other": "skip"}])
        client = _client_with("post", response)

        with _patch_async_client(client):
            names = await provider.list("prefix/")

        assert names == ["a", "b"]

    @pytest.mark.asyncio
    async def test_list_error(self, provider):
        response = _make_response(500, text="x")
        client = _client_with("post", response)

        with _patch_async_client(client):
            with pytest.raises(StorageError, match="Supabase error 500"):
                await provider.list("p")

    @pytest.mark.asyncio
    async def test_health_check_true(self, provider):
        response = _make_response(200)
        client = _client_with("get", response)

        with _patch_async_client(client):
            assert await provider.health_check() is True

    @pytest.mark.asyncio
    async def test_health_check_false_on_exception(self, provider):
        client = MagicMock()
        client.__aenter__ = AsyncMock(return_value=client)
        client.__aexit__ = AsyncMock(return_value=None)
        client.get = AsyncMock(side_effect=httpx.RequestError("x"))

        with _patch_async_client(client):
            assert await provider.health_check() is False

    @pytest.mark.asyncio
    async def test_get_signed_url(self, provider):
        response = _make_response(200, json_data={"signedURL": "/storage/v1/sign/...?token=abc"})
        client = _client_with("post", response)

        with _patch_async_client(client):
            url = await provider.get_signed_url("a.bin", expires_in=60)

        assert url.startswith("https://test.supabase.co")
        assert "sign" in url

    @pytest.mark.asyncio
    async def test_get_signed_url_error(self, provider):
        response = _make_response(500, text="x")
        client = _client_with("post", response)

        with _patch_async_client(client):
            with pytest.raises(StorageError, match="signed URL"):
                await provider.get_signed_url("a.bin")
