"""Tests for application bootstrap module."""

import tempfile
from pathlib import Path
from unittest.mock import MagicMock, patch

import pytest

from src.bootstrap import (
    ApplicationComponents,
    bootstrap,
    create_api,
    create_consumer,
    create_mounter_engine,
    create_storage,
)
from src.config import Settings


@pytest.fixture
def temp_dir():
    with tempfile.TemporaryDirectory() as tmpdir:
        yield Path(tmpdir)


@pytest.fixture
def test_settings(temp_dir: Path) -> Settings:
    return Settings(
        redis_url="redis://localhost:6379",
        storage_provider="local",
        local_storage_path=str(temp_dir / "datalake"),
        wal_path=str(temp_dir / "wal"),
        dlq_path=str(temp_dir / "dlq"),
        api_port=8888,
        api_key="test-key",
        _env_file=None,
    )


class TestApplicationComponents:
    """Tests for ApplicationComponents container."""

    def test_stores_all_components(self):
        settings = MagicMock()
        storage = MagicMock()
        mounter_engine = MagicMock()
        consumer = MagicMock()
        api = MagicMock()

        components = ApplicationComponents(
            settings=settings,
            storage=storage,
            mounter_engine=mounter_engine,
            consumer=consumer,
            api=api,
        )

        assert components.settings == settings
        assert components.storage == storage
        assert components.mounter_engine == mounter_engine
        assert components.consumer == consumer
        assert components.api == api


class TestCreateStorage:
    """Tests for create_storage function."""

    def test_creates_local_storage(self, test_settings):
        storage = create_storage(test_settings)

        assert storage is not None
        assert hasattr(storage, "read_events")
        assert hasattr(storage, "append_events_batch")


class TestCreateMounterEngine:
    """Tests for create_mounter_engine function."""

    def test_creates_mounter_engine(self, test_settings):
        with patch("src.bootstrap.setup_mounters") as mock_setup:
            mock_engine = MagicMock()
            mock_setup.return_value = mock_engine

            engine = create_mounter_engine(test_settings)

            mock_setup.assert_called_once()
            assert engine == mock_engine


class TestCreateConsumer:
    """Tests for create_consumer function."""

    def test_creates_consumer(self, test_settings):
        storage = MagicMock()
        mounter_engine = MagicMock()

        consumer = create_consumer(test_settings, storage, mounter_engine)

        assert consumer is not None
        assert hasattr(consumer, "start")
        assert hasattr(consumer, "stop")

    def test_consumer_has_retry_handler(self, test_settings):
        storage = MagicMock()
        mounter_engine = MagicMock()

        consumer = create_consumer(test_settings, storage, mounter_engine)

        assert hasattr(consumer, "retry_handler")


class TestCreateApi:
    """Tests for create_api function."""

    def test_creates_api(self, test_settings):
        storage = MagicMock()
        consumer = MagicMock()
        consumer.retry_handler = MagicMock()
        consumer.metrics = {"events_processed": 100}

        api = create_api(test_settings, storage, consumer)

        assert api is not None
        assert hasattr(api, "app")

    def test_sets_consumer_metrics_callback(self, test_settings):
        storage = MagicMock()
        consumer = MagicMock()
        consumer.retry_handler = MagicMock()
        consumer.metrics = {"events_processed": 50}

        api = create_api(test_settings, storage, consumer)

        metrics = api._get_consumer_metrics()
        assert metrics == {"events_processed": 50}


class TestBootstrap:
    """Tests for bootstrap function."""

    def test_creates_all_components(self, test_settings):
        with patch("src.bootstrap.setup_mounters") as mock_setup:
            mock_setup.return_value = MagicMock()

            components = bootstrap(test_settings)

            assert isinstance(components, ApplicationComponents)
            assert components.settings == test_settings
            assert components.storage is not None
            assert components.consumer is not None
            assert components.api is not None

    def test_wires_components_together(self, test_settings):
        with patch("src.bootstrap.setup_mounters") as mock_setup:
            mock_engine = MagicMock()
            mock_setup.return_value = mock_engine

            components = bootstrap(test_settings)

            assert components.api._consumer_metrics_callback is not None
            metrics_callback = components.api._consumer_metrics_callback
            assert metrics_callback() == components.consumer.metrics


class TestBootstrapIntegration:
    """Integration tests for bootstrap."""

    def test_all_components_have_correct_types(self, test_settings):
        with patch("src.bootstrap.setup_mounters") as mock_setup:
            mock_setup.return_value = MagicMock()

            components = bootstrap(test_settings)

            from src.api import DatalakeAPI
            from src.consumer import DatalakeConsumer

            assert isinstance(components.api, DatalakeAPI)
            assert isinstance(components.consumer, DatalakeConsumer)

    def test_storage_matches_settings(self, test_settings):
        with patch("src.bootstrap.setup_mounters") as mock_setup:
            mock_setup.return_value = MagicMock()

            components = bootstrap(test_settings)

            from src.storage.local import LocalStorageProvider

            assert isinstance(components.storage, LocalStorageProvider)
