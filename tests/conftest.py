"""Shared test fixtures for GeneFlow AI service."""

import tempfile
from pathlib import Path

import pytest
from fastapi.testclient import TestClient

from src.api import app, set_redis_health, set_service_status
from src.config import Settings
from src.models import ServiceStatus


@pytest.fixture
def temp_dir() -> Path:
    """Create a temporary directory for tests."""
    with tempfile.TemporaryDirectory() as tmpdir:
        yield Path(tmpdir)


@pytest.fixture
def settings() -> Settings:
    """Create test settings."""
    return Settings(
        redis_url="redis://localhost:6379",
        api_port=8090,
        log_level="DEBUG",
        eventbus_enabled=False,
        claude_api_key="",
        blast_email="test@example.com",
    )


@pytest.fixture
def api_client() -> TestClient:
    """Create test API client."""
    set_service_status(ServiceStatus.RUNNING)
    set_redis_health(True)
    return TestClient(app)


@pytest.fixture
def sample_sequence() -> str:
    """Sample DNA sequence for testing."""
    return "ATGCGATCGATCGATCGATCGTACGTACGTACGTACGTAGCTAGCTAGCTAGCTAG"


@pytest.fixture
def sample_quality() -> list[int]:
    """Sample quality_enhanced scores for testing."""
    return [30, 35, 40, 38, 42, 45, 40, 38, 35, 30] * 5 + [25, 20, 15, 10, 5]
