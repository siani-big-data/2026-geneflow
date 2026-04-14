"""Tests for API authentication."""

from unittest.mock import MagicMock

import pytest
from fastapi import Depends, FastAPI, HTTPException
from fastapi.testclient import TestClient

from src.api.auth import get_api_key_dependency
from src.api.auth.api_key import ApiKeyAuthenticator
from src.config import Settings


class TestApiKeyAuthenticator:
    """Tests for ApiKeyAuthenticator class."""

    @pytest.mark.asyncio
    async def test_verify_with_correct_key(self):
        authenticator = ApiKeyAuthenticator("secret-key")

        result = await authenticator.verify(x_api_key="secret-key")

        assert result is None

    @pytest.mark.asyncio
    async def test_verify_with_wrong_key_raises(self):
        authenticator = ApiKeyAuthenticator("secret-key")

        with pytest.raises(HTTPException) as exc_info:
            await authenticator.verify(x_api_key="wrong-key")

        assert exc_info.value.status_code == 401
        assert "Invalid or missing API key" in exc_info.value.detail

    @pytest.mark.asyncio
    async def test_verify_with_missing_key_raises(self):
        authenticator = ApiKeyAuthenticator("secret-key")

        with pytest.raises(HTTPException) as exc_info:
            await authenticator.verify(x_api_key=None)

        assert exc_info.value.status_code == 401

    @pytest.mark.asyncio
    async def test_verify_without_configured_key_allows_all(self):
        authenticator = ApiKeyAuthenticator(None)

        result = await authenticator.verify(x_api_key=None)

        assert result is None

    @pytest.mark.asyncio
    async def test_verify_empty_string_key_allows_all(self):
        authenticator = ApiKeyAuthenticator("")

        result = await authenticator.verify(x_api_key="any-key")

        assert result is None


class TestGetApiKeyDependency:
    """Tests for get_api_key_dependency function."""

    def test_creates_dependency_function(self):
        settings = MagicMock(spec=Settings)
        settings.api_key = "test-key"

        dependency = get_api_key_dependency(settings)

        assert callable(dependency)

    @pytest.mark.asyncio
    async def test_dependency_verifies_key(self):
        settings = MagicMock(spec=Settings)
        settings.api_key = "test-key"

        dependency = get_api_key_dependency(settings)

        result = await dependency(x_api_key="test-key")
        assert result is None

    @pytest.mark.asyncio
    async def test_dependency_rejects_wrong_key(self):
        settings = MagicMock(spec=Settings)
        settings.api_key = "test-key"

        dependency = get_api_key_dependency(settings)

        with pytest.raises(HTTPException) as exc_info:
            await dependency(x_api_key="wrong-key")

        assert exc_info.value.status_code == 401


class TestApiKeyIntegration:
    """Integration tests for API key authentication."""

    @pytest.fixture
    def protected_app(self):
        app = FastAPI()
        authenticator = ApiKeyAuthenticator("integration-test-key")

        @app.get("/public")
        async def public_endpoint():
            return {"message": "public"}

        @app.get("/protected")
        async def protected_endpoint(_=Depends(authenticator.verify)):
            return {"message": "secret"}

        return app

    def test_public_endpoint_no_auth(self, protected_app):
        client = TestClient(protected_app)

        response = client.get("/public")

        assert response.status_code == 200
        assert response.json() == {"message": "public"}

    def test_protected_endpoint_with_valid_key(self, protected_app):
        client = TestClient(protected_app)

        response = client.get(
            "/protected", headers={"X-API-Key": "integration-test-key"}
        )

        assert response.status_code == 200
        assert response.json() == {"message": "secret"}

    def test_protected_endpoint_with_invalid_key(self, protected_app):
        client = TestClient(protected_app)

        response = client.get("/protected", headers={"X-API-Key": "wrong-key"})

        assert response.status_code == 401

    def test_protected_endpoint_without_key(self, protected_app):
        client = TestClient(protected_app)

        response = client.get("/protected")

        assert response.status_code == 401

    def test_protected_endpoint_empty_key(self, protected_app):
        client = TestClient(protected_app)

        response = client.get("/protected", headers={"X-API-Key": ""})

        assert response.status_code == 401

    def test_case_sensitive_header(self, protected_app):
        client = TestClient(protected_app)

        response = client.get(
            "/protected", headers={"x-api-key": "integration-test-key"}
        )

        assert response.status_code == 200


class TestNoAuthConfig:
    """Tests for when no API key is configured."""

    @pytest.fixture
    def open_app(self):
        app = FastAPI()
        authenticator = ApiKeyAuthenticator(None)

        @app.get("/endpoint")
        async def endpoint(_=Depends(authenticator.verify)):
            return {"message": "open"}

        return app

    def test_allows_requests_without_key(self, open_app):
        client = TestClient(open_app)

        response = client.get("/endpoint")

        assert response.status_code == 200

    def test_allows_requests_with_any_key(self, open_app):
        client = TestClient(open_app)

        response = client.get("/endpoint", headers={"X-API-Key": "random-key"})

        assert response.status_code == 200
