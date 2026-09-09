"""Tests for Health API."""

import pytest
from fastapi.testclient import TestClient

from src.api import create_app
from src.api.routes.health import set_redis_health, set_workers
from src.models import WorkerMetrics, WorkerStatus


class MockWorker:
    """Mock worker for testing."""

    def __init__(self, status: WorkerStatus = WorkerStatus.RUNNING):
        self._status = status
        self._metrics = WorkerMetrics(
            jobsProcessed=10,
            jobsFailed=1,
            averageProcessingTimeMs=50.5,
        )

    @property
    def status(self) -> WorkerStatus:
        return self._status

    @property
    def metrics(self) -> WorkerMetrics:
        return self._metrics


@pytest.fixture
def app():
    """Create test app."""
    return create_app()


@pytest.fixture
def client(app):
    """Create test client."""
    return TestClient(app)


@pytest.fixture(autouse=True)
def reset_state():
    """Reset global state before each test."""
    set_workers({})
    set_redis_health(False)
    yield
    set_workers({})
    set_redis_health(False)


class TestHealthEndpoint:
    """Tests for /health endpoint."""

    def test_health_no_workers(self, client):
        """Test health with no workers registered."""
        response = client.get("/health")

        assert response.status_code == 200
        data = response.json()
        assert data["status"] == "degraded"
        assert data["workers"] == {}

    def test_health_all_workers_running(self, client):
        """Test health with all workers running."""
        set_workers(
            {
                "trace": MockWorker(WorkerStatus.RUNNING),
                "alignment": MockWorker(WorkerStatus.RUNNING),
            }
        )
        set_redis_health(True)

        response = client.get("/health")

        assert response.status_code == 200
        data = response.json()
        assert data["status"] == "healthy"
        assert data["redis"] == "connected"
        assert "trace" in data["workers"]
        assert "alignment" in data["workers"]

    def test_health_some_workers_stopped(self, client):
        """Test health with some workers stopped."""
        set_workers(
            {
                "trace": MockWorker(WorkerStatus.RUNNING),
                "alignment": MockWorker(WorkerStatus.STOPPED),
            }
        )

        response = client.get("/health")

        assert response.status_code == 200
        data = response.json()
        assert data["status"] == "degraded"

    def test_health_all_workers_stopped(self, client):
        """Test health with all workers stopped."""
        set_workers(
            {
                "trace": MockWorker(WorkerStatus.STOPPED),
                "alignment": MockWorker(WorkerStatus.STOPPED),
            }
        )

        response = client.get("/health")

        assert response.status_code == 200
        data = response.json()
        assert data["status"] == "unhealthy"

    def test_health_redis_disconnected(self, client):
        """Test health shows Redis as disconnected."""
        set_redis_health(False)

        response = client.get("/health")

        data = response.json()
        assert data["redis"] == "disconnected"


class TestWorkerHealthEndpoint:
    """Tests for /health/workers/{name} endpoint."""

    def test_worker_health_exists(self, client):
        """Test getting health of existing worker."""
        set_workers(
            {
                "trace": MockWorker(WorkerStatus.RUNNING),
            }
        )

        response = client.get("/health/workers/trace")

        assert response.status_code == 200
        data = response.json()
        assert data["status"] == "running"
        assert data["jobsProcessed"] == 10
        assert data["jobsFailed"] == 1
        assert data["averageProcessingTimeMs"] == 50.5

    def test_worker_health_not_found(self, client):
        """Test getting health of non-existent worker."""
        response = client.get("/health/workers/nonexistent")

        assert response.status_code == 404


class TestReadinessEndpoint:
    """Tests for /ready endpoint."""

    def test_ready_no_workers(self, client):
        """Test readiness with no workers."""
        response = client.get("/ready")

        assert response.status_code == 503

    def test_ready_no_redis(self, client):
        """Test readiness without Redis."""
        set_workers(
            {
                "trace": MockWorker(WorkerStatus.RUNNING),
            }
        )
        set_redis_health(False)

        response = client.get("/ready")

        assert response.status_code == 503

    def test_ready_workers_stopped(self, client):
        """Test readiness with stopped workers."""
        set_workers(
            {
                "trace": MockWorker(WorkerStatus.STOPPED),
            }
        )
        set_redis_health(True)

        response = client.get("/ready")

        assert response.status_code == 503

    def test_ready_success(self, client):
        """Test successful readiness check."""
        set_workers(
            {
                "trace": MockWorker(WorkerStatus.RUNNING),
            }
        )
        set_redis_health(True)

        response = client.get("/ready")

        assert response.status_code == 200
        assert response.json()["ready"] is True


class TestLivenessEndpoint:
    """Tests for /live endpoint."""

    def test_liveness_always_succeeds(self, client):
        """Test that liveness always returns 200."""
        response = client.get("/live")

        assert response.status_code == 200
        assert response.json()["alive"] is True
