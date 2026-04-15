"""Tests for API endpoints."""

from datetime import datetime
from pathlib import Path

import pytest
from httpx import ASGITransport, AsyncClient

from src.api import DatalakeAPI
from src.config import Settings
from src.retry import RetryHandler
from src.storage.local import LocalStorageProvider


class TestHealthEndpoint:
    @pytest.fixture
    def api_client(self, tmp_path: Path):
        settings = Settings(
            storage_provider="local",
            local_storage_path=str(tmp_path / "datalake"),
            dlq_path=str(tmp_path / "dlq"),
            api_key="test-key",
            _env_file=None,
        )

        storage = LocalStorageProvider(base_path=str(tmp_path / "datalake"))
        retry_handler = RetryHandler(dlq_path=str(tmp_path / "dlq"))

        api = DatalakeAPI(
            storage=storage,
            settings=settings,
            retry_handler=retry_handler,
        )

        return api

    @pytest.mark.asyncio
    async def test_health_no_auth_required(self, api_client):
        transport = ASGITransport(app=api_client.app)
        async with AsyncClient(transport=transport, base_url="http://test") as client:
            response = await client.get("/health")

        assert response.status_code == 200
        assert response.json()["status"] == "healthy"

    @pytest.mark.asyncio
    async def test_health_returns_storage_status(self, api_client):
        transport = ASGITransport(app=api_client.app)
        async with AsyncClient(transport=transport, base_url="http://test") as client:
            response = await client.get("/health")

        assert response.json()["storage_healthy"] is True


class TestAuthMiddleware:
    @pytest.fixture
    def api_client(self, tmp_path: Path):
        settings = Settings(
            storage_provider="local",
            local_storage_path=str(tmp_path / "datalake"),
            dlq_path=str(tmp_path / "dlq"),
            api_key="test-key",
            _env_file=None,
        )

        storage = LocalStorageProvider(base_path=str(tmp_path / "datalake"))
        retry_handler = RetryHandler(dlq_path=str(tmp_path / "dlq"))

        api = DatalakeAPI(
            storage=storage,
            settings=settings,
            retry_handler=retry_handler,
        )

        return api

    @pytest.mark.asyncio
    async def test_protected_endpoint_without_key_returns_401(self, api_client):
        transport = ASGITransport(app=api_client.app)
        async with AsyncClient(transport=transport, base_url="http://test") as client:
            response = await client.get("/categories")

        assert response.status_code == 401
        data = response.json()
        assert data["error"] == "unauthorized"
        assert "Invalid or missing API key" in data["message"]
        assert "correlation_id" in data

    @pytest.mark.asyncio
    async def test_protected_endpoint_with_wrong_key_returns_401(self, api_client):
        transport = ASGITransport(app=api_client.app)
        async with AsyncClient(transport=transport, base_url="http://test") as client:
            response = await client.get("/categories", headers={"X-API-Key": "wrong-key"})

        assert response.status_code == 401

    @pytest.mark.asyncio
    async def test_protected_endpoint_with_correct_key_returns_200(self, api_client):
        transport = ASGITransport(app=api_client.app)
        async with AsyncClient(transport=transport, base_url="http://test") as client:
            response = await client.get("/categories", headers={"X-API-Key": "test-key"})

        assert response.status_code == 200


class TestCategoriesEndpoints:
    @pytest.fixture
    def api_with_storage(self, tmp_path: Path):
        settings = Settings(
            storage_provider="local",
            local_storage_path=str(tmp_path / "datalake"),
            dlq_path=str(tmp_path / "dlq"),
            api_key="test-key",
            _env_file=None,
        )

        storage = LocalStorageProvider(base_path=str(tmp_path / "datalake"))
        retry_handler = RetryHandler(dlq_path=str(tmp_path / "dlq"))

        api = DatalakeAPI(
            storage=storage,
            settings=settings,
            retry_handler=retry_handler,
        )

        return api, storage

    @pytest.mark.asyncio
    async def test_list_categories_empty(self, api_with_storage):
        api, _ = api_with_storage
        transport = ASGITransport(app=api.app)
        async with AsyncClient(transport=transport, base_url="http://test") as client:
            response = await client.get("/categories", headers={"X-API-Key": "test-key"})

        assert response.status_code == 200
        assert response.json()["categories"] == []

    @pytest.mark.asyncio
    async def test_list_categories_with_data(self, api_with_storage):
        api, storage = api_with_storage

        await storage.append_events_batch("users", datetime(2026, 3, 25), ['{"test": 1}'])
        await storage.append_events_batch("traces", datetime(2026, 3, 25), ['{"test": 2}'])

        transport = ASGITransport(app=api.app)
        async with AsyncClient(transport=transport, base_url="http://test") as client:
            response = await client.get("/categories", headers={"X-API-Key": "test-key"})

        assert response.status_code == 200
        categories = response.json()["categories"]
        assert "users" in categories
        assert "traces" in categories

    @pytest.mark.asyncio
    async def test_category_stats(self, api_with_storage):
        api, storage = api_with_storage

        await storage.append_events_batch("users", datetime(2026, 3, 25), ['{"e": 1}', '{"e": 2}'])

        transport = ASGITransport(app=api.app)
        async with AsyncClient(transport=transport, base_url="http://test") as client:
            response = await client.get("/categories/users/stats", headers={"X-API-Key": "test-key"})

        assert response.status_code == 200
        data = response.json()
        assert data["category"] == "users"
        assert data["event_count"] == 2
        assert data["file_count"] == 1

    @pytest.mark.asyncio
    async def test_category_dates(self, api_with_storage):
        api, storage = api_with_storage

        await storage.append_events_batch("users", datetime(2026, 3, 24), ['{"e": 1}'])
        await storage.append_events_batch("users", datetime(2026, 3, 25), ['{"e": 2}'])

        transport = ASGITransport(app=api.app)
        async with AsyncClient(transport=transport, base_url="http://test") as client:
            response = await client.get("/categories/users/dates", headers={"X-API-Key": "test-key"})

        assert response.status_code == 200
        data = response.json()
        assert data["count"] == 2
        assert "2026-03-24" in data["dates"]
        assert "2026-03-25" in data["dates"]


class TestEventsEndpoint:
    @pytest.fixture
    def api_with_storage(self, tmp_path: Path):
        settings = Settings(
            storage_provider="local",
            local_storage_path=str(tmp_path / "datalake"),
            dlq_path=str(tmp_path / "dlq"),
            api_key="test-key",
            _env_file=None,
        )

        storage = LocalStorageProvider(base_path=str(tmp_path / "datalake"))
        retry_handler = RetryHandler(dlq_path=str(tmp_path / "dlq"))

        api = DatalakeAPI(
            storage=storage,
            settings=settings,
            retry_handler=retry_handler,
        )

        return api, storage

    @pytest.mark.asyncio
    async def test_get_events_by_date(self, api_with_storage):
        api, storage = api_with_storage

        await storage.append_events_batch(
            "users",
            datetime(2026, 3, 25),
            ['{"eventId": "1", "type": "UserRegistered"}'],
        )

        transport = ASGITransport(app=api.app)
        async with AsyncClient(transport=transport, base_url="http://test") as client:
            response = await client.get(
                "/events/users?date=2026-03-25",
                headers={"X-API-Key": "test-key"},
            )

        assert response.status_code == 200
        data = response.json()
        assert data["count"] == 1
        assert data["events"][0]["eventId"] == "1"

    @pytest.mark.asyncio
    async def test_get_events_filter_by_type(self, api_with_storage):
        api, storage = api_with_storage

        await storage.append_events_batch(
            "users",
            datetime(2026, 3, 25),
            [
                '{"eventId": "1", "type": "UserRegistered"}',
                '{"eventId": "2", "type": "UserUpdated"}',
            ],
        )

        transport = ASGITransport(app=api.app)
        async with AsyncClient(transport=transport, base_url="http://test") as client:
            response = await client.get(
                "/events/users?date=2026-03-25&event_type=UserRegistered",
                headers={"X-API-Key": "test-key"},
            )

        assert response.status_code == 200
        data = response.json()
        assert data["count"] == 1
        assert data["events"][0]["type"] == "UserRegistered"

    @pytest.mark.asyncio
    async def test_get_events_pagination(self, api_with_storage):
        api, storage = api_with_storage

        events = [f'{{"eventId": "{i}"}}' for i in range(10)]
        await storage.append_events_batch("users", datetime(2026, 3, 25), events)

        transport = ASGITransport(app=api.app)
        async with AsyncClient(transport=transport, base_url="http://test") as client:
            response = await client.get(
                "/events/users?date=2026-03-25&limit=3&offset=2",
                headers={"X-API-Key": "test-key"},
            )

        assert response.status_code == 200
        data = response.json()
        assert data["count"] == 10
        assert len(data["events"]) == 3
        assert data["events"][0]["eventId"] == "2"


class TestReplayEndpoint:
    @pytest.fixture
    def api_with_storage(self, tmp_path: Path):
        settings = Settings(
            storage_provider="local",
            local_storage_path=str(tmp_path / "datalake"),
            dlq_path=str(tmp_path / "dlq"),
            api_key="test-key",
            _env_file=None,
        )

        storage = LocalStorageProvider(base_path=str(tmp_path / "datalake"))
        retry_handler = RetryHandler(dlq_path=str(tmp_path / "dlq"))

        api = DatalakeAPI(
            storage=storage,
            settings=settings,
            retry_handler=retry_handler,
        )

        return api, storage

    @pytest.mark.asyncio
    async def test_replay_all(self, api_with_storage):
        api, storage = api_with_storage

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

        transport = ASGITransport(app=api.app)
        async with AsyncClient(transport=transport, base_url="http://test") as client:
            response = await client.get("/replay/users", headers={"X-API-Key": "test-key"})

        assert response.status_code == 200
        data = response.json()
        assert data["count"] == 2
        assert data["first_date"] == "2026-03-24"
        assert data["last_date"] == "2026-03-25"

    @pytest.mark.asyncio
    async def test_replay_empty_category(self, api_with_storage):
        api, _ = api_with_storage

        transport = ASGITransport(app=api.app)
        async with AsyncClient(transport=transport, base_url="http://test") as client:
            response = await client.get("/replay/nonexistent", headers={"X-API-Key": "test-key"})

        assert response.status_code == 200
        data = response.json()
        assert data["count"] == 0
        assert data["events"] == []


class TestDLQEndpoints:
    @pytest.fixture
    def api_client(self, tmp_path: Path):
        settings = Settings(
            storage_provider="local",
            local_storage_path=str(tmp_path / "datalake"),
            dlq_path=str(tmp_path / "dlq"),
            api_key="test-key",
            _env_file=None,
        )

        storage = LocalStorageProvider(base_path=str(tmp_path / "datalake"))
        retry_handler = RetryHandler(dlq_path=str(tmp_path / "dlq"))

        api = DatalakeAPI(
            storage=storage,
            settings=settings,
            retry_handler=retry_handler,
        )

        return api

    @pytest.mark.asyncio
    async def test_get_dlq_empty(self, api_client):
        transport = ASGITransport(app=api_client.app)
        async with AsyncClient(transport=transport, base_url="http://test") as client:
            response = await client.get("/dlq", headers={"X-API-Key": "test-key"})

        assert response.status_code == 200
        assert response.json()["count"] == 0

    @pytest.mark.asyncio
    async def test_get_all_dlq_empty(self, api_client):
        transport = ASGITransport(app=api_client.app)
        async with AsyncClient(transport=transport, base_url="http://test") as client:
            response = await client.get("/dlq/all", headers={"X-API-Key": "test-key"})

        assert response.status_code == 200
        assert response.json()["count"] == 0
