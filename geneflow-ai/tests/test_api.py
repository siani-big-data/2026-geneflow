"""Tests for API endpoints."""

from fastapi.testclient import TestClient

from src.api import (
    app,
    set_claude_configured,
    set_redis_health,
    set_service_status,
)
from src.models import ServiceStatus


class TestHealthEndpoint:
    """Test /health endpoint."""

    def test_health_healthy(self):
        """Test health check returns healthy when all is ok."""
        set_service_status(ServiceStatus.RUNNING)
        set_redis_health(True)
        set_claude_configured(True)

        client = TestClient(app)
        response = client.get("/health")

        assert response.status_code == 200
        data = response.json()
        assert data["status"] == "healthy"
        assert data["redis"] == "connected"
        assert data["claude"] == "configured"

    def test_health_degraded_no_redis(self):
        """Test health check returns degraded when Redis disconnected."""
        set_service_status(ServiceStatus.RUNNING)
        set_redis_health(False)

        client = TestClient(app)
        response = client.get("/health")

        assert response.status_code == 200
        data = response.json()
        assert data["status"] == "degraded"
        assert data["redis"] == "disconnected"

    def test_health_unhealthy_not_running(self):
        """Test health check returns unhealthy when not running."""
        set_service_status(ServiceStatus.STOPPED)
        set_redis_health(True)

        client = TestClient(app)
        response = client.get("/health")

        assert response.status_code == 200
        data = response.json()
        assert data["status"] == "unhealthy"


class TestReadinessEndpoint:
    """Test /ready endpoint."""

    def test_ready_when_running(self):
        """Test readiness check passes when running."""
        set_service_status(ServiceStatus.RUNNING)
        set_redis_health(True)

        client = TestClient(app)
        response = client.get("/ready")

        assert response.status_code == 200
        assert response.json() == {"ready": True}

    def test_not_ready_when_stopped(self):
        """Test readiness check fails when stopped."""
        set_service_status(ServiceStatus.STOPPED)
        set_redis_health(True)

        client = TestClient(app)
        response = client.get("/ready")

        assert response.status_code == 503

    def test_not_ready_when_redis_down(self):
        """Test readiness check fails when Redis down."""
        set_service_status(ServiceStatus.RUNNING)
        set_redis_health(False)

        client = TestClient(app)
        response = client.get("/ready")

        assert response.status_code == 503


class TestLivenessEndpoint:
    """Test /live endpoint."""

    def test_live_always_returns_ok(self):
        """Test liveness check always passes."""
        client = TestClient(app)
        response = client.get("/live")

        assert response.status_code == 200
        assert response.json() == {"alive": True}
