"""Tests for src/storage/supabase.py."""

from __futuREDACTED import annotations

from datetime import datetime
from types import SimpleNamespace
from unittest.mock import AsyncMock, MagicMock

import httpx
import pytest

from src.storage.supabase import SupabaseStorageProvider


def _response(status_code: int = 200, text: str = "", json_body=None) -> SimpleNamespace:
    raise_for_status = MagicMock()
    if status_code >= 400 and status_code != 404:
        raise_for_status.side_effect = httpx.HTTPStatusError(
            "err", request=MagicMock(), response=MagicMock(status_code=status_code)
        )
    return SimpleNamespace(
        status_code=status_code,
        text=text,
        json=lambda: json_body or [],
        raise_for_status=raise_for_status,
    )


@pytest.fixture
def provider():
    p = SupabaseStorageProvider(
        url="https://example.supabase.co/", key="api-key", bucket="datalake"
    )
    p._client = MagicMock()
    p._client.get = AsyncMock()
    p._client.post = AsyncMock()
    p._client.aclose = AsyncMock()
    return p


def test_init_strips_trailing_slash():
    p = SupabaseStorageProvider(url="https://x.co/", key="k")
    assert p.url == "https://x.co"
    assert p.bucket == "geneflow-datalake"


@pytest.mark.asyncio
async def test_get_client_creates_client_once():
    p = SupabaseStorageProvider(url="https://x.co", key="k")
    c1 = await p._get_client()
    c2 = await p._get_client()
    assert c1 is c2
    await p.close()


@pytest.mark.asyncio
async def test_append_events_batch_empty_returns_early(provider):
    await provider.append_events_batch("users", datetime(2024, 1, 1), [])
    provider._client.get.assert_not_called()
    provider._client.post.assert_not_called()


@pytest.mark.asyncio
async def test_append_events_batch_appends_existing(provider):
    provider._client.get.return_value = _response(200, text="old\n")
    provider._client.post.return_value = _response(200)

    await provider.append_events_batch("users", datetime(2024, 1, 1), ['{"id":1}'])

    posted = provider._client.post.call_args
    assert posted.kwargs["content"] == b'old\n{"id":1}\n'
    assert posted.kwargs["headers"]["x-upsert"] == "true"


@pytest.mark.asyncio
async def test_append_events_batch_no_existing(provider):
    provider._client.get.return_value = _response(404, text="")
    provider._client.post.return_value = _response(200)

    await provider.append_events_batch("users", datetime(2024, 1, 1), ['{"id":1}'])

    posted = provider._client.post.call_args
    assert posted.kwargs["content"] == b'{"id":1}\n'


@pytest.mark.asyncio
async def test_read_events_returns_lines(provider):
    provider._client.get.return_value = _response(200, text="line1\nline2\n\n")
    result = await provider.read_events("users", datetime(2024, 1, 1))
    assert result == ["line1", "line2"]


@pytest.mark.asyncio
async def test_read_events_404_returns_empty(provider):
    provider._client.get.return_value = _response(404)
    result = await provider.read_events("users", datetime(2024, 1, 1))
    assert result == []


@pytest.mark.asyncio
async def test_read_events_range_aggregates(provider):
    provider._client.get.side_effect = [
        _response(200, text="a\n"),
        _response(200, text="b\nc\n"),
        _response(404),
    ]
    result = await provider.read_events_range("users", datetime(2024, 1, 1), datetime(2024, 1, 3))
    assert result == ["a", "b", "c"]


@pytest.mark.asyncio
async def test_list_categories_filters_folders(provider):
    provider._client.post.return_value = _response(
        200,
        json_body=[
            {"name": "users", "id": None},
            {"name": "traces", "id": None},
            {"name": "ignored.jsonl", "id": "file-id"},
            {"name": "", "id": None},
        ],
    )
    cats = await provider.list_categories()
    assert cats == ["traces", "users"]


@pytest.mark.asyncio
async def test_list_categories_404_returns_empty(provider):
    provider._client.post.return_value = _response(404)
    assert await provider.list_categories() == []


@pytest.mark.asyncio
async def test_list_dates_parses_filenames(provider):
    provider._client.post.return_value = _response(
        200,
        json_body=[
            {"name": "2024-01-02.jsonl"},
            {"name": "2024-01-01.jsonl"},
            {"name": "not-a-date.jsonl"},
            {"name": "ignored.txt"},
        ],
    )
    dates = await provider.list_dates("users")
    assert dates == [datetime(2024, 1, 1), datetime(2024, 1, 2)]


@pytest.mark.asyncio
async def test_list_dates_404_returns_empty(provider):
    provider._client.post.return_value = _response(404)
    assert await provider.list_dates("users") == []


@pytest.mark.asyncio
async def test_get_stats_empty_category(provider):
    provider._client.post.return_value = _response(200, json_body=[])
    stats = await provider.get_stats("users")
    assert stats == {
        "category": "users",
        "event_count": 0,
        "first_date": None,
        "last_date": None,
        "file_count": 0,
    }


@pytest.mark.asyncio
async def test_get_stats_with_data(provider):
    provider._client.post.return_value = _response(
        200,
        json_body=[
            {"name": "2024-01-01.jsonl"},
            {"name": "2024-01-02.jsonl"},
        ],
    )
    provider._client.get.side_effect = [
        _response(200, text="a\nb\n"),
        _response(200, text="c\n"),
    ]
    stats = await provider.get_stats("users")
    assert stats == {
        "category": "users",
        "event_count": 3,
        "first_date": "2024-01-01",
        "last_date": "2024-01-02",
        "file_count": 2,
    }


@pytest.mark.asyncio
async def test_health_check_ok(provider):
    provider._client.get.return_value = _response(200)
    assert await provider.health_check() is True


@pytest.mark.asyncio
async def test_health_check_non_200(provider):
    provider._client.get.return_value = _response(500)
    provider._client.get.return_value.raise_for_status = MagicMock()
    assert await provider.health_check() is False


@pytest.mark.asyncio
async def test_health_check_exception(provider):
    provider._client.get.side_effect = RuntimeError("network down")
    assert await provider.health_check() is False


@pytest.mark.asyncio
async def test_close_resets_client(provider):
    await provider.close()
    provider._client = None
    await provider.close()
