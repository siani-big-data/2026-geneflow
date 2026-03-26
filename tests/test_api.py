from datetime import datetime
from pathlib import Path

import pytest
from fastapi.testclient import TestClient

from src.api import DatalakeAPI
from src.config import Settings
from src.retry import RetryHandler
from src.storage.local import LocalStorageProvider


@pytest.fixture
def api_with_data(temp_dir: Path) -> tuple[TestClient, LocalStorageProvider]:
    settings = Settings(
        storage_provider="local",
        local_storage_path=str(temp_dir / "datalake"),
        dlq_path=str(temp_dir / "dlq"),
        api_key="test-key",
    )

    storage = LocalStorageProvider(base_path=str(temp_dir / "datalake"))
    retry_handler = RetryHandler(dlq_path=str(temp_dir / "dlq"))

    api = DatalakeAPI(
        storage=storage,
        settings=settings,
        retry_handler=retry_handler,
    )

    return TestClient(api.app), storage


class TestHealthEndpoint:
    def test_health_no_auth_required(self, api_with_data):
        client, _ = api_with_data

        response = client.get("/health")

        assert response.status_code == 200
        assert response.json()["status"] == "healthy"

    def test_health_returns_storage_status(self, api_with_data):
        client, _ = api_with_data

        response = client.get("/health")

        assert response.json()["storage_healthy"] is True


class TestAuthMiddleware:
    def test_protected_endpoint_without_key_returns_401(self, api_with_data):
        client, _ = api_with_data

        response = client.get("/categories")

        assert response.status_code == 401
        data = response.json()
        assert data["error"] == "unauthorized"
        assert "Invalid or missing API key" in data["message"]
        assert "correlation_id" in data

    def test_protected_endpoint_with_wrong_key_returns_401(self, api_with_data):
        client, _ = api_with_data

        response = client.get("/categories", headers={"X-API-Key": "wrong-key"})

        assert response.status_code == 401

    def test_protected_endpoint_with_correct_key_returns_200(self, api_with_data):
        client, _ = api_with_data

        response = client.get("/categories", headers={"X-API-Key": "test-key"})

        assert response.status_code == 200


class TestCategoriesEndpoints:
    def test_list_categories_empty(self, api_with_data):
        client, _ = api_with_data

        response = client.get("/categories", headers={"X-API-Key": "test-key"})

        assert response.status_code == 200
        assert response.json()["categories"] == []

    @pytest.mark.asyncio
    async def test_list_categories_with_data(self, api_with_data):
        client, storage = api_with_data

        await storage.append_events_batch("users", datetime(2026, 3, 25), ['{"test": 1}'])
        await storage.append_events_batch("traces", datetime(2026, 3, 25), ['{"test": 2}'])

        response = client.get("/categories", headers={"X-API-Key": "test-key"})

        assert response.status_code == 200
        categories = response.json()["categories"]
        assert "users" in categories
        assert "traces" in categories

    @pytest.mark.asyncio
    async def test_category_stats(self, api_with_data):
        client, storage = api_with_data

        await storage.append_events_batch("users", datetime(2026, 3, 25), ['{"e": 1}', '{"e": 2}'])

        response = client.get("/categories/users/stats", headers={"X-API-Key": "test-key"})

        assert response.status_code == 200
        data = response.json()
        assert data["category"] == "users"
        assert data["event_count"] == 2
        assert data["file_count"] == 1

    @pytest.mark.asyncio
    async def test_category_dates(self, api_with_data):
        client, storage = api_with_data

        await storage.append_events_batch("users", datetime(2026, 3, 24), ['{"e": 1}'])
        await storage.append_events_batch("users", datetime(2026, 3, 25), ['{"e": 2}'])

        response = client.get("/categories/users/dates", headers={"X-API-Key": "test-key"})

        assert response.status_code == 200
        data = response.json()
        assert data["count"] == 2
        assert "2026-03-24" in data["dates"]
        assert "2026-03-25" in data["dates"]


class TestEventsEndpoint:
    @pytest.mark.asyncio
    async def test_get_events_by_date(self, api_with_data):
        client, storage = api_with_data

        await storage.append_events_batch(
            "users",
            datetime(2026, 3, 25),
            ['{"eventId": "1", "type": "UserRegistered"}'],
        )

        response = client.get(
            "/events/users?date=2026-03-25",
            headers={"X-API-Key": "test-key"},
        )

        assert response.status_code == 200
        data = response.json()
        assert data["count"] == 1
        assert data["events"][0]["eventId"] == "1"

    @pytest.mark.asyncio
    async def test_get_events_filter_by_type(self, api_with_data):
        client, storage = api_with_data

        await storage.append_events_batch(
            "users",
            datetime(2026, 3, 25),
            [
                '{"eventId": "1", "type": "UserRegistered"}',
                '{"eventId": "2", "type": "UserUpdated"}',
            ],
        )

        response = client.get(
            "/events/users?date=2026-03-25&event_type=UserRegistered",
            headers={"X-API-Key": "test-key"},
        )

        assert response.status_code == 200
        data = response.json()
        assert data["count"] == 1
        assert data["events"][0]["type"] == "UserRegistered"

    @pytest.mark.asyncio
    async def test_get_events_pagination(self, api_with_data):
        client, storage = api_with_data

        events = [f'{{"eventId": "{i}"}}' for i in range(10)]
        await storage.append_events_batch("users", datetime(2026, 3, 25), events)

        response = client.get(
            "/events/users?date=2026-03-25&limit=3&offset=2",
            headers={"X-API-Key": "test-key"},
        )

        assert response.status_code == 200
        data = response.json()
        assert data["count"] == 10  # Total count
        assert len(data["events"]) == 3  # Paginated
        assert data["events"][0]["eventId"] == "2"  # Offset applied


class TestReplayEndpoint:
    @pytest.mark.asyncio
    async def test_replay_all(self, api_with_data):
        client, storage = api_with_data

        await storage.append_events_batch(
            "users",
            datetime(2026, 3, 24),
            ['{"eventId": "1", "timestamp": "2026-03-24T10:00:00"}'],
        )
        await storage.append_events_batch(
            "users",
            datetime(2026, 3, 25),
            ['{"eventId": "2", "timestamp": "2026-03-25T10:00:00"}'],
        )

        response = client.get("/replay/users", headers={"X-API-Key": "test-key"})

        assert response.status_code == 200
        data = response.json()
        assert data["count"] == 2
        assert data["first_date"] == "2026-03-24"
        assert data["last_date"] == "2026-03-25"

    @pytest.mark.asyncio
    async def test_replay_empty_category(self, api_with_data):
        client, _ = api_with_data

        response = client.get("/replay/nonexistent", headers={"X-API-Key": "test-key"})

        assert response.status_code == 200
        data = response.json()
        assert data["count"] == 0
        assert data["events"] == []


class TestDLQEndpoints:
    def test_get_dlq_empty(self, api_with_data):
        client, _ = api_with_data

        response = client.get("/dlq", headers={"X-API-Key": "test-key"})

        assert response.status_code == 200
        assert response.json()["count"] == 0

    def test_get_all_dlq_empty(self, api_with_data):
        client, _ = api_with_data

        response = client.get("/dlq/all", headers={"X-API-Key": "test-key"})

        assert response.status_code == 200
        assert response.json()["count"] == 0
