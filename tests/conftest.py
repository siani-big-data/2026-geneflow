"""Shared test fixtures for GeneFlow Analysis Worker."""

import tempfile
from pathlib import Path

import pytest

from src.config import Settings


@pytest.fixture
def temp_dir() -> Path:
    """Create a temporary directory for tests."""
    with tempfile.TemporaryDirectory() as tmpdir:
        yield Path(tmpdir)


@pytest.fixture
def settings(temp_dir: Path) -> Settings:
    """Create test settings."""
    return Settings(
        redis_url="redis://localhost:6379",
        storage_provider="local",
        local_storage_path=str(temp_dir / "data"),
        log_level="DEBUG",
        eventbus_enabled=False,
    )


@pytest.fixture
def sample_sequence() -> str:
    """Sample DNA sequence for testing."""
    return "ATGCGATCGATCGATCGATCGTACGTACGTACGTACGTAGCTAGCTAGCTAGCTAG"


@pytest.fixture
def sample_quality() -> list[int]:
    """Sample quality scores for testing."""
    return [30, 35, 40, 38, 42, 45, 40, 38, 35, 30] * 5 + [25, 20, 15, 10, 5, 3, 2]


@pytest.fixture
def sample_chromatogram_data() -> dict:
    """Sample chromatogram data for testing."""
    return {
        "traceA": [100, 50, 200, 30] * 10,
        "traceC": [50, 200, 30, 100] * 10,
        "traceG": [30, 100, 50, 200] * 10,
        "traceT": [200, 30, 100, 50] * 10,
        "baseCalls": list(range(40)),
        "peakLocations": [i * 10 for i in range(40)],
    }
