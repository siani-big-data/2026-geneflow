"""Tests for API service layer."""

from datetime import datetime
from unittest.mock import AsyncMock, MagicMock

import pytest

from src.api.services.category_stats_service import CategoryStatsService
from src.api.services.dlq_service import DLQService
from src.api.services.events_query_service import EventsQueryService


class TestEventsQueryService:
    """Tests for EventsQueryService."""

    @pytest.fixture
    def mock_storage(self):
        storage = MagicMock()
        storage.read_events = AsyncMock(return_value=[])
        storage.read_events_range = AsyncMock(return_value=[])
        return storage

    @pytest.fixture
    def service(self, mock_storage):
        return EventsQueryService(mock_storage)

    @pytest.mark.asyncio
    async def test_query_events_by_date(self, service, mock_storage):
        mock_storage.read_events.return_value = [
            '{"eventId": "1", "type": "Test"}',
            '{"eventId": "2", "type": "Test"}',
        ]

        result = await service.query_events("users", date="2026-03-25")

        assert result.total_count == 2
        assert len(result.events) == 2
        assert result.category == "users"
        assert result.date == "2026-03-25"

    @pytest.mark.asyncio
    async def test_query_events_filter_by_type(self, service, mock_storage):
        mock_storage.read_events.return_value = [
            '{"eventId": "1", "type": "UserRegistered"}',
            '{"eventId": "2", "type": "UserUpdated"}',
            '{"eventId": "3", "type": "UserRegistered"}',
        ]

        result = await service.query_events("users", date="2026-03-25", event_type="UserRegistered")

        assert result.total_count == 2
        assert all(e["type"] == "UserRegistered" for e in result.events)

    @pytest.mark.asyncio
    async def test_query_events_pagination(self, service, mock_storage):
        mock_storage.read_events.return_value = [f'{{"eventId": "{i}"}}' for i in range(10)]

        result = await service.query_events("users", date="2026-03-25", limit=3, offset=2)

        assert result.total_count == 10
        assert len(result.events) == 3
        assert result.events[0]["eventId"] == "2"

    @pytest.mark.asyncio
    async def test_query_events_range(self, service, mock_storage):
        mock_storage.read_events_range.return_value = [
            '{"eventId": "1"}',
            '{"eventId": "2"}',
        ]

        result = await service.query_events("users", start_date="2026-03-24", end_date="2026-03-26")

        mock_storage.read_events_range.assert_called_once()
        assert result.total_count == 2
        assert result.start_date == "2026-03-24"
        assert result.end_date == "2026-03-26"

    @pytest.mark.asyncio
    async def test_query_events_skip_invalid_json(self, service, mock_storage):
        mock_storage.read_events.return_value = [
            '{"eventId": "1"}',
            "not json",
            '{"eventId": "2"}',
        ]

        result = await service.query_events("users", date="2026-03-25")

        assert result.total_count == 2

    @pytest.mark.asyncio
    async def test_query_events_default_date_is_today(self, service, mock_storage):
        mock_storage.read_events.return_value = []

        result = await service.query_events("users")

        assert result.date == datetime.utcnow().strftime("%Y-%m-%d")


class TestCategoryStatsService:
    """Tests for CategoryStatsService."""

    @pytest.fixture
    def mock_storage(self):
        storage = MagicMock()
        storage.list_categories = AsyncMock(return_value=[])
        storage.get_stats = AsyncMock(return_value={})
        storage.list_dates = AsyncMock(return_value=[])
        return storage

    @pytest.fixture
    def service(self, mock_storage):
        return CategoryStatsService(mock_storage)

    @pytest.mark.asyncio
    async def test_list_categories_with_data(self, service, mock_storage):
        mock_storage.list_categories.return_value = ["users", "traces"]

        result = await service.list_categories_with_data()

        assert result == ["users", "traces"]

    def test_list_available_categories(self, service):
        result = service.list_available_categories()

        assert "users" in result
        assert "traces" in result
        assert "studies" in result

    @pytest.mark.asyncio
    async def test_get_stats(self, service, mock_storage):
        mock_storage.get_stats.return_value = {
            "event_count": 150,
            "first_date": "2026-03-01",
            "last_date": "2026-03-25",
            "file_count": 25,
        }

        result = await service.get_stats("users")

        assert result.category == "users"
        assert result.event_count == 150
        assert result.first_date == "2026-03-01"
        assert result.last_date == "2026-03-25"
        assert result.file_count == 25

    @pytest.mark.asyncio
    async def test_get_stats_empty_category(self, service, mock_storage):
        mock_storage.get_stats.return_value = {
            "event_count": 0,
            "first_date": None,
            "last_date": None,
            "file_count": 0,
        }

        result = await service.get_stats("nonexistent")

        assert result.event_count == 0
        assert result.first_date is None

    @pytest.mark.asyncio
    async def test_get_dates(self, service, mock_storage):
        mock_storage.list_dates.return_value = [
            datetime(2026, 3, 24),
            datetime(2026, 3, 25),
        ]

        result = await service.get_dates("users")

        assert result.category == "users"
        assert result.count == 2
        assert "2026-03-24" in result.dates
        assert "2026-03-25" in result.dates


class TestDLQService:
    """Tests for DLQService."""

    @pytest.fixture
    def mock_retry_handler(self):
        handler = MagicMock()
        handler.get_dlq_events = AsyncMock(return_value=[])
        handler.get_all_dlq_events = AsyncMock(return_value=[])
        handler.replay_dlq_event = AsyncMock(return_value=True)
        handler.replay_all_dlq = AsyncMock(return_value={"succeeded": 0, "failed": 0, "total": 0})
        return handler

    @pytest.fixture
    def service(self, mock_retry_handler):
        return DLQService(mock_retry_handler)

    @pytest.mark.asyncio
    async def test_get_events_today(self, service, mock_retry_handler):
        mock_retry_handler.get_dlq_events.return_value = [
            {"eventId": "1"},
            {"eventId": "2"},
        ]

        result = await service.get_events()

        assert result.count == 2
        assert len(result.events) == 2

    @pytest.mark.asyncio
    async def test_get_events_by_date(self, service, mock_retry_handler):
        mock_retry_handler.get_dlq_events.return_value = [{"eventId": "1"}]

        result = await service.get_events(date="2026-03-25")

        mock_retry_handler.get_dlq_events.assert_called_once()
        assert result.date == "2026-03-25"
        assert result.count == 1

    @pytest.mark.asyncio
    async def test_get_all_events(self, service, mock_retry_handler):
        mock_retry_handler.get_all_dlq_events.return_value = [
            {"eventId": "1"},
            {"eventId": "2"},
            {"eventId": "3"},
        ]

        result = await service.get_all_events()

        mock_retry_handler.get_all_dlq_events.assert_called_once()
        assert result.count == 3

    @pytest.mark.asyncio
    async def test_retry_event_success(self, service, mock_retry_handler):
        mock_retry_handler.replay_dlq_event.return_value = True

        result = await service.retry_event("event-123")

        assert result.success is True
        assert result.event_id == "event-123"
        assert "successfully" in result.message

    @pytest.mark.asyncio
    async def test_retry_event_failure(self, service, mock_retry_handler):
        mock_retry_handler.replay_dlq_event.return_value = False

        result = await service.retry_event("event-123")

        assert result.success is False
        assert "Failed" in result.message

    @pytest.mark.asyncio
    async def test_retry_all(self, service, mock_retry_handler):
        mock_retry_handler.replay_all_dlq.return_value = {
            "succeeded": 5,
            "failed": 2,
            "total": 7,
        }

        result = await service.retry_all()

        assert result.succeeded == 5
        assert result.failed == 2
        assert result.total == 7

    @pytest.mark.asyncio
    async def test_retry_all_by_date(self, service, mock_retry_handler):
        mock_retry_handler.replay_all_dlq.return_value = {
            "succeeded": 3,
            "failed": 0,
            "total": 3,
        }

        result = await service.retry_all(date="2026-03-25")

        mock_retry_handler.replay_all_dlq.assert_called_once()
        assert result.total == 3
