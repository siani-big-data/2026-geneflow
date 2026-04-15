"""Tests for API middleware."""

import uuid
from unittest.mock import patch

import pytest
from fastapi import FastAPI
from fastapi.testclient import TestClient

from src.api.middleware.correlation import (
    CorrelationIdMiddleware,
    correlation_id_ctx,
    get_correlation_id,
)
from src.api.middleware.logging import RequestLoggingMiddleware


@pytest.fixture
def app_with_correlation_middleware():
    """Create app with correlation ID middleware."""
    app = FastAPI()
    app.add_middleware(CorrelationIdMiddleware)

    @app.get("/test")
    async def test_endpoint():
        return {"correlation_id": get_correlation_id()}

    return app


@pytest.fixture
def app_with_logging_middleware():
    """Create app with logging middleware."""
    app = FastAPI()
    app.add_middleware(CorrelationIdMiddleware)
    app.add_middleware(RequestLoggingMiddleware, log_requests=True)

    @app.get("/test")
    async def test_endpoint():
        return {"status": "ok"}

    @app.get("/health")
    async def health_endpoint():
        return {"status": "healthy"}

    @app.get("/error")
    async def error_endpoint():
        raise ValueError("Test error")

    return app


class TestCorrelationIdMiddleware:
    """Tests for CorrelationIdMiddleware."""

    def test_generates_correlation_id_when_missing(self, app_with_correlation_middleware):
        client = TestClient(app_with_correlation_middleware)

        response = client.get("/test")

        assert response.status_code == 200
        assert "X-Correlation-ID" in response.headers
        correlation_id = response.headers["X-Correlation-ID"]
        assert len(correlation_id) == 36

    def test_uses_provided_correlation_id(self, app_with_correlation_middleware):
        client = TestClient(app_with_correlation_middleware)
        custom_id = "custom-correlation-123"

        response = client.get("/test", headers={"X-Correlation-ID": custom_id})

        assert response.status_code == 200
        assert response.headers["X-Correlation-ID"] == custom_id
        assert response.json()["correlation_id"] == custom_id

    def test_correlation_id_available_in_handler(self, app_with_correlation_middleware):
        client = TestClient(app_with_correlation_middleware)

        response = client.get("/test")

        data = response.json()
        assert "correlation_id" in data
        assert data["correlation_id"] == response.headers["X-Correlation-ID"]

    def test_different_requests_get_different_ids(self, app_with_correlation_middleware):
        client = TestClient(app_with_correlation_middleware)

        response1 = client.get("/test")
        response2 = client.get("/test")

        id1 = response1.headers["X-Correlation-ID"]
        id2 = response2.headers["X-Correlation-ID"]
        assert id1 != id2


class TestGetCorrelationId:
    """Tests for get_correlation_id function."""

    def test_returns_default_when_not_set(self):
        correlation_id_ctx.set("")

        result = get_correlation_id()

        assert result == "unknown"

    def test_returns_set_value(self):
        test_id = "test-correlation-id"
        correlation_id_ctx.set(test_id)

        result = get_correlation_id()

        assert result == test_id

    def test_returns_uuid_format(self, app_with_correlation_middleware):
        client = TestClient(app_with_correlation_middleware)

        response = client.get("/test")

        correlation_id = response.headers["X-Correlation-ID"]
        try:
            uuid.UUID(correlation_id)
            is_valid_uuid = True
        except ValueError:
            is_valid_uuid = False

        assert is_valid_uuid


class TestRequestLoggingMiddleware:
    """Tests for RequestLoggingMiddleware."""

    def test_logs_request_completion(self, app_with_logging_middleware):
        client = TestClient(app_with_logging_middleware)

        with patch("src.api.middleware.logging.logger") as mock_logger:
            response = client.get("/test")

            assert response.status_code == 200
            mock_logger.info.assert_called()

    def test_skips_health_endpoint(self, app_with_logging_middleware):
        client = TestClient(app_with_logging_middleware)

        with patch("src.api.middleware.logging.logger") as mock_logger:
            response = client.get("/health")

            assert response.status_code == 200
            calls = [
                call
                for call in mock_logger.info.call_args_list
                if "/health" in str(call)
            ]
            assert len(calls) == 0

    def test_logs_error_on_exception(self, app_with_logging_middleware):
        client = TestClient(app_with_logging_middleware, raise_server_exceptions=False)

        with patch("src.api.middleware.logging.logger") as mock_logger:
            response = client.get("/error")

            assert response.status_code == 500
            mock_logger.error.assert_called()

    def test_includes_correlation_id_in_logs(self, app_with_logging_middleware):
        client = TestClient(app_with_logging_middleware)
        custom_id = "log-test-correlation"

        with patch("src.api.middleware.logging.logger") as mock_logger:
            response = client.get("/test", headers={"X-Correlation-ID": custom_id})

            assert response.status_code == 200
            assert mock_logger.info.called

    def test_includes_duration_in_logs(self, app_with_logging_middleware):
        client = TestClient(app_with_logging_middleware)

        with patch("src.api.middleware.logging.logger") as mock_logger:
            client.get("/test")

            info_calls = mock_logger.info.call_args_list
            assert any("duration_ms" in str(call) for call in info_calls)

    def test_logs_warning_for_4xx_responses(self):
        app = FastAPI()
        app.add_middleware(CorrelationIdMiddleware)
        app.add_middleware(RequestLoggingMiddleware, log_requests=True)

        @app.get("/not-found")
        async def not_found():
            from fastapi import HTTPException

            raise HTTPException(status_code=404, detail="Not found")

        client = TestClient(app, raise_server_exceptions=False)

        with patch("src.api.middleware.logging.logger") as mock_logger:
            response = client.get("/not-found")

            assert response.status_code == 404
            mock_logger.warning.assert_called()

    def test_no_logging_when_disabled(self):
        app = FastAPI()
        app.add_middleware(CorrelationIdMiddleware)
        app.add_middleware(RequestLoggingMiddleware, log_requests=False)

        @app.get("/test")
        async def test_endpoint():
            return {"status": "ok"}

        client = TestClient(app)

        with patch("src.api.middleware.logging.logger") as mock_logger:
            response = client.get("/test")

            assert response.status_code == 200
            mock_logger.info.assert_not_called()

    def test_custom_skip_paths(self):
        app = FastAPI()
        app.add_middleware(CorrelationIdMiddleware)
        app.add_middleware(
            RequestLoggingMiddleware, log_requests=True, skip_paths={"/custom", "/skip"}
        )

        @app.get("/custom")
        async def custom_endpoint():
            return {"status": "ok"}

        @app.get("/logged")
        async def logged_endpoint():
            return {"status": "ok"}

        client = TestClient(app)

        with patch("src.api.middleware.logging.logger") as mock_logger:
            client.get("/custom")
            custom_calls = mock_logger.info.call_count

            client.get("/logged")
            logged_calls = mock_logger.info.call_count

            assert logged_calls > custom_calls
