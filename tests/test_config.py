import os

import pytest

from src.config import Settings


def test_default_values(monkeypatch):
    # Clear env vars that might be set from .env file
    monkeypatch.delenv("DATALAKE_REDIS_URL", raising=False)
    monkeypatch.delenv("DATALAKE_STORAGE_PROVIDER", raising=False)
    monkeypatch.delenv("DATALAKE_BUFFER_MAX_SIZE", raising=False)
    monkeypatch.delenv("DATALAKE_API_PORT", raising=False)
    monkeypatch.delenv("DATALAKE_API_KEY", raising=False)

    settings = Settings(_env_file=None)
    assert settings.redis_url == "redis://redis:6379"
    assert settings.storage_provider == "local"
    assert settings.buffer_max_size == 1000
    assert settings.api_port == 8080


def test_env_override(monkeypatch):
    monkeypatch.setenv("DATALAKE_REDIS_URL", "redis://custom:6380")
    monkeypatch.setenv("DATALAKE_API_PORT", "9000")

    settings = Settings()
    assert settings.redis_url == "redis://custom:6380"
    assert settings.api_port == 9000


def test_api_key_default_empty(monkeypatch):
    # Clear env vars that might be set from .env file
    monkeypatch.delenv("DATALAKE_API_KEY", raising=False)

    settings = Settings(_env_file=None)
    assert settings.api_key == ""


def test_api_key_from_env(monkeypatch):
    monkeypatch.setenv("DATALAKE_API_KEY", "my-secret-key")

    settings = Settings()
    assert settings.api_key == "my-secret-key"
