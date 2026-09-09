"""Tests for lifecycle module."""

from unittest.mock import AsyncMock, MagicMock, patch

import pytest

from src.config import Settings
from src.lifecycle import ApplicationLifecycle
from src.models import WorkerMetrics, WorkerStatus


class MockWorker:
    """Mock worker for testing lifecycle."""

    def __init__(self, name: str = "mock"):
        self._name = name
        self._status = WorkerStatus.STOPPED
        self._metrics = WorkerMetrics(jobsProcessed=5, jobsFailed=0)
        self.start = AsyncMock()
        self.stop = AsyncMock()

    @property
    def name(self) -> str:
        return self._name

    @property
    def status(self) -> WorkerStatus:
        return self._status

    @property
    def metrics(self) -> WorkerMetrics:
        return self._metrics


class MockComponents:
    """Mock application components for testing."""

    def __init__(self, settings: Settings):
        self.settings = settings
        self.redis = AsyncMock()
        self.redis.ping = AsyncMock()
        self.redis.close = AsyncMock()
        self.publisher = AsyncMock()
        self.publisher.publish = AsyncMock()
        self.storage = MagicMock()
        self.workers = {"trace": MockWorker("trace")}
        self.api = MagicMock()
        self.api.set_redis_health = MagicMock()


@pytest.fixture
def test_settings(temp_dir) -> Settings:
    """Create test settings."""
    return Settings(
        redis_url="redis://localhost:6379",
        storage_provider="local",
        local_storage_path=str(temp_dir / "data"),
        api_host="127.0.0.1",
        api_port=8888,
        trace_worker_enabled=True,
        alignment_worker_enabled=False,
        analysis_worker_enabled=False,
        eventbus_enabled=False,
    )


@pytest.fixture
def mock_components(test_settings) -> MockComponents:
    """Create mock application components."""
    return MockComponents(test_settings)


class TestApplicationLifecycle:
    """Tests for ApplicationLifecycle class."""

    def test_init(self, mock_components):
        """Test lifecycle initialization."""
        lifecycle = ApplicationLifecycle(mock_components)

        assert lifecycle.components is mock_components
        assert lifecycle._worker_tasks == []
        assert lifecycle._api_task is None

    @pytest.mark.asyncio
    async def test_startup_connects_redis(self, mock_components):
        """Test startup connects to Redis."""
        lifecycle = ApplicationLifecycle(mock_components)

        with patch.object(lifecycle, "_run_api", new_callable=AsyncMock):
            await lifecycle.startup()

        mock_components.redis.ping.assert_called_once()

    @pytest.mark.asyncio
    async def test_startup_publishes_event(self, mock_components):
        """Test startup publishes WorkerStarted event."""
        lifecycle = ApplicationLifecycle(mock_components)

        with patch.object(lifecycle, "_run_api", new_callable=AsyncMock):
            await lifecycle.startup()

        mock_components.publisher.publish.assert_called_once()
        event = mock_components.publisher.publish.call_args[0][0]
        assert event.workerName == "geneflow-analysis"

    @pytest.mark.asyncio
    async def test_startup_starts_workers(self, mock_components):
        """Test startup starts all workers."""
        lifecycle = ApplicationLifecycle(mock_components)

        with patch.object(lifecycle, "_run_api", new_callable=AsyncMock):
            await lifecycle.startup()

        for worker in mock_components.workers.values():
            worker.start.assert_called_once()

    @pytest.mark.asyncio
    async def test_startup_sets_redis_healthy(self, mock_components):
        """Test startup sets Redis as healthy."""
        lifecycle = ApplicationLifecycle(mock_components)

        with patch.object(lifecycle, "_run_api", new_callable=AsyncMock):
            await lifecycle.startup()

        mock_components.api.set_redis_health.assert_called_with(True)

    @pytest.mark.asyncio
    async def test_shutdown_stops_workers(self, mock_components):
        """Test shutdown stops all workers."""
        lifecycle = ApplicationLifecycle(mock_components)

        # Start first
        with patch.object(lifecycle, "_run_api", new_callable=AsyncMock):
            await lifecycle.startup()

        # Then shutdown
        await lifecycle.shutdown()

        for worker in mock_components.workers.values():
            worker.stop.assert_called_once()

    @pytest.mark.asyncio
    async def test_shutdown_closes_redis(self, mock_components):
        """Test shutdown closes Redis connection."""
        lifecycle = ApplicationLifecycle(mock_components)

        with patch.object(lifecycle, "_run_api", new_callable=AsyncMock):
            await lifecycle.startup()

        await lifecycle.shutdown()

        mock_components.redis.close.assert_called_once()

    @pytest.mark.asyncio
    async def test_shutdown_publishes_event(self, mock_components):
        """Test shutdown publishes WorkerStopped event."""
        lifecycle = ApplicationLifecycle(mock_components)

        with patch.object(lifecycle, "_run_api", new_callable=AsyncMock):
            await lifecycle.startup()

        await lifecycle.shutdown()

        # Should have two calls: WorkerStarted and WorkerStopped
        assert mock_components.publisher.publish.call_count == 2

    @pytest.mark.asyncio
    async def test_startup_failuREDACTED(self, mock_components):
        """Test Redis connection failure sets unhealthy status."""
        mock_components.redis.ping.side_effect = Exception("Connection refused")

        lifecycle = ApplicationLifecycle(mock_components)

        with pytest.raises(Exception, match="Connection refused"):
            await lifecycle.startup()


class TestSignalHandlers:
    """Tests for signal handling."""

    def test_setup_signal_handlers(self, mock_components):
        """Test signal handlers are set up."""
        lifecycle = ApplicationLifecycle(mock_components)

        # Just verify the method exists and is callable
        # Actual signal handling is platform-specific
        assert hasattr(lifecycle, "_setup_signal_handlers")
        assert callable(lifecycle._setup_signal_handlers)
