from datetime import datetime, timedelta

import pytest

from src.storage.local import LocalStorageProvider


@pytest.mark.asyncio
async def test_append_and_read(storage: LocalStorageProvider):
    date = datetime(2026, 3, 25)
    lines = ['{"eventId": "1"}', '{"eventId": "2"}']

    await storage.append_events_batch("users", date, lines)
    result = await storage.read_events("users", date)

    assert len(result) == 2
    assert result[0] == '{"eventId": "1"}'
    assert result[1] == '{"eventId": "2"}'


@pytest.mark.asyncio
async def test_read_nonexistent_returns_empty(storage: LocalStorageProvider):
    date = datetime(2026, 3, 25)

    result = await storage.read_events("nonexistent", date)

    assert result == []


@pytest.mark.asyncio
async def test_append_creates_directories(storage: LocalStorageProvider):
    date = datetime(2026, 3, 25)

    await storage.append_events_batch("newcategory", date, ['{"test": true}'])
    result = await storage.read_events("newcategory", date)

    assert len(result) == 1


@pytest.mark.asyncio
async def test_read_events_range(storage: LocalStorageProvider):
    date1 = datetime(2026, 3, 24)
    date2 = datetime(2026, 3, 25)
    date3 = datetime(2026, 3, 26)

    await storage.append_events_batch("users", date1, ['{"day": 1}'])
    await storage.append_events_batch("users", date2, ['{"day": 2}'])
    await storage.append_events_batch("users", date3, ['{"day": 3}'])

    result = await storage.read_events_range("users", date1, date3)

    assert len(result) == 3


@pytest.mark.asyncio
async def test_list_categories(storage: LocalStorageProvider):
    date = datetime(2026, 3, 25)

    await storage.append_events_batch("users", date, ['{"test": 1}'])
    await storage.append_events_batch("traces", date, ['{"test": 2}'])

    categories = await storage.list_categories()

    assert "users" in categories
    assert "traces" in categories


@pytest.mark.asyncio
async def test_list_dates(storage: LocalStorageProvider):
    date1 = datetime(2026, 3, 24)
    date2 = datetime(2026, 3, 25)

    await storage.append_events_batch("users", date1, ['{"test": 1}'])
    await storage.append_events_batch("users", date2, ['{"test": 2}'])

    dates = await storage.list_dates("users")

    assert len(dates) == 2
    assert date1 in dates
    assert date2 in dates


@pytest.mark.asyncio
async def test_get_stats(storage: LocalStorageProvider):
    date1 = datetime(2026, 3, 24)
    date2 = datetime(2026, 3, 25)

    await storage.append_events_batch("users", date1, ['{"e": 1}', '{"e": 2}'])
    await storage.append_events_batch("users", date2, ['{"e": 3}'])

    stats = await storage.get_stats("users")

    assert stats["category"] == "users"
    assert stats["event_count"] == 3
    assert stats["file_count"] == 2
    assert stats["first_date"] == "2026-03-24"
    assert stats["last_date"] == "2026-03-25"


@pytest.mark.asyncio
async def test_get_stats_empty_category(storage: LocalStorageProvider):
    stats = await storage.get_stats("nonexistent")

    assert stats["event_count"] == 0
    assert stats["file_count"] == 0
    assert stats["first_date"] is None


@pytest.mark.asyncio
async def test_health_check(storage: LocalStorageProvider):
    result = await storage.health_check()

    assert result is True


@pytest.mark.asyncio
async def test_append_empty_list_does_nothing(storage: LocalStorageProvider):
    date = datetime(2026, 3, 25)

    await storage.append_events_batch("users", date, [])
    result = await storage.read_events("users", date)

    assert result == []
