"""Tests for bootstrap module."""

import pytest

from src.bootstrap import (
    ApplicationComponents,
    bootstrap,
    create_api,
    create_publisher,
    create_redis,
    create_storage,
    create_workers,
)
from src.config import Settings


@pytest.fixture
def test_settings(temp_dir) -> Settings:
    """Create test settings."""
    return Settings(
        redis_url="redis://localhost:6379",
        storage_provider="local",
        local_storage_path=str(temp_dir / "data"),
        trace_worker_enabled=True,
        alignment_worker_enabled=True,
        analysis_worker_enabled=True,
        eventbus_enabled=False,
    )


class TestCreateRedis:
    """Tests for create_redis factory."""

    def test_creates_redis_client(self, test_settings):
        """Test Redis client creation."""
        redis = create_redis(test_settings)

        assert redis is not None
        # Client is not connected until ping() is called


class TestCreatePublisher:
    """Tests for create_publisher factory."""

    def test_creates_publisher(self, test_settings):
        """Test EventBusPublisher creation."""
        redis = create_redis(test_settings)
        publisher = create_publisher(redis, test_settings)

        assert publisher is not None


class TestCreateStorage:
    """Tests for create_storage factory."""

    def test_creates_local_storage(self, test_settings):
        """Test local storage creation."""
        storage = create_storage(test_settings)

        assert storage is not None

    def test_creates_http_storage(self, test_settings):
        """Test HTTP storage creation."""
        test_settings.storage_provider = "http"
        storage = create_storage(test_settings)

        assert storage is not None

    def test_invalid_storage_raises(self, test_settings):
        """Test invalid storage provider raises error."""
        test_settings.storage_provider = "invalid"

        with pytest.raises(ValueError, match="Unknown storage provider"):
            create_storage(test_settings)


class TestCreateWorkers:
    """Tests for create_workers factory."""

    def test_creates_all_workers(self, test_settings):
        """Test all workers created when enabled."""
        redis = create_redis(test_settings)
        publisher = create_publisher(redis, test_settings)

        workers = create_workers(redis, publisher, test_settings)

        assert len(workers) == 4
        assert "trace" in workers
        assert "alignment" in workers
        assert "analysis" in workers
        assert "phylogeny" in workers

    def test_creates_no_workers_when_disabled(self, test_settings):
        """Test no workers created when disabled."""
        test_settings.trace_worker_enabled = False
        test_settings.alignment_worker_enabled = False
        test_settings.analysis_worker_enabled = False
        test_settings.phylogeny_worker_enabled = False

        redis = create_redis(test_settings)
        publisher = create_publisher(redis, test_settings)

        workers = create_workers(redis, publisher, test_settings)

        assert len(workers) == 0

    def test_creates_partial_workers(self, test_settings):
        """Test partial worker creation."""
        test_settings.alignment_worker_enabled = False

        redis = create_redis(test_settings)
        publisher = create_publisher(redis, test_settings)

        workers = create_workers(redis, publisher, test_settings)

        assert len(workers) == 3
        assert "trace" in workers
        assert "alignment" not in workers
        assert "analysis" in workers
        assert "phylogeny" in workers


class TestCreateApi:
    """Tests for create_api factory."""

    def test_creates_api(self, test_settings):
        """Test API creation."""
        api = create_api(test_settings)

        assert api is not None
        assert api.app is not None


class TestBootstrap:
    """Tests for bootstrap function."""

    def test_bootstrap_creates_components(self, test_settings):
        """Test bootstrap creates all components."""
        components = bootstrap(test_settings)

        assert isinstance(components, ApplicationComponents)
        assert components.settings is test_settings
        assert components.redis is not None
        assert components.publisher is not None
        assert components.storage is not None
        assert components.workers is not None
        assert components.api is not None

    def test_bootstrap_registers_workers_with_api(self, test_settings):
        """Test bootstrap registers workers with API."""
        components = bootstrap(test_settings)

        assert len(components.api.workers) == 4

    def test_bootstrap_default_settings(self, monkeypatch, temp_dir):
        """Test bootstrap with default settings."""
        monkeypatch.setenv("WORKER_STORAGE_PROVIDER", "local")
        monkeypatch.setenv("WORKER_LOCAL_STORAGE_PATH", str(temp_dir / "data"))

        components = bootstrap()

        assert components.settings is not None
