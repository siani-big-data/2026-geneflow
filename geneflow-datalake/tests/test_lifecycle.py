"""Tests for application lifecycle module."""

import asyncio
import sys
from unittest.mock import AsyncMock, MagicMock, patch

import pytest

from src.bootstrap import ApplicationComponents
from src.lifecycle import ApplicationLifecycle


@pytest.fixture
def mock_components():
    """Create mock application components."""
    settings = MagicMock()
    settings.storage_provider = "local"
    settings.redis_url = "redis://localhost:6379"
    settings.api_host = "0.0.0.0"
    settings.api_port = 8080

    storage = MagicMock()
    mounter_engine = MagicMock()
    mounter_engine.start = AsyncMock()
    mounter_engine.stop = AsyncMock()

    consumer = MagicMock()
    consumer.start = AsyncMock()
    consumer.stop = AsyncMock()

    api = MagicMock()
    api.app = MagicMock()

    return ApplicationComponents(
        settings=settings,
        storage=storage,
        mounter_engine=mounter_engine,
        consumer=consumer,
        api=api,
    )


class TestApplicationLifecycle:
    """Tests for ApplicationLifecycle class."""

    def test_initialization(self, mock_components):
        lifecycle = ApplicationLifecycle(mock_components)

        assert lifecycle.components == mock_components
        assert lifecycle._shutdown_event is not None
        assert lifecycle._consumer_task is None
        assert lifecycle._api_task is None

    @pytest.mark.asyncio
    async def test_startup_starts_mounter_engine(self, mock_components):
        lifecycle = ApplicationLifecycle(mock_components)

        with patch.object(lifecycle, "_run_api", new_callable=AsyncMock):
            await lifecycle.startup()

            mock_components.mounter_engine.start.assert_called_once()

    @pytest.mark.asyncio
    async def test_startup_creates_consumer_task(self, mock_components):
        lifecycle = ApplicationLifecycle(mock_components)

        with patch.object(lifecycle, "_run_api", new_callable=AsyncMock):
            await lifecycle.startup()

            assert lifecycle._consumer_task is not None
            lifecycle._consumer_task.cancel()

    @pytest.mark.asyncio
    async def test_startup_creates_api_task(self, mock_components):
        lifecycle = ApplicationLifecycle(mock_components)

        with patch.object(lifecycle, "_run_api", new_callable=AsyncMock):
            await lifecycle.startup()

            assert lifecycle._api_task is not None
            lifecycle._api_task.cancel()

    @pytest.mark.asyncio
    async def test_shutdown_stops_consumer(self, mock_components):
        lifecycle = ApplicationLifecycle(mock_components)
        lifecycle._consumer_task = asyncio.create_task(asyncio.sleep(10))
        lifecycle._api_task = asyncio.create_task(asyncio.sleep(10))

        await lifecycle.shutdown()

        mock_components.consumer.stop.assert_called_once()

    @pytest.mark.asyncio
    async def test_shutdown_stops_mounter_engine(self, mock_components):
        lifecycle = ApplicationLifecycle(mock_components)
        lifecycle._consumer_task = asyncio.create_task(asyncio.sleep(10))
        lifecycle._api_task = asyncio.create_task(asyncio.sleep(10))

        await lifecycle.shutdown()

        mock_components.mounter_engine.stop.assert_called_once()

    @pytest.mark.asyncio
    async def test_shutdown_cancels_tasks(self, mock_components):
        lifecycle = ApplicationLifecycle(mock_components)
        lifecycle._consumer_task = asyncio.create_task(asyncio.sleep(100))
        lifecycle._api_task = asyncio.create_task(asyncio.sleep(100))

        await lifecycle.shutdown()

        assert lifecycle._consumer_task.cancelled()
        assert lifecycle._api_task.cancelled()

    @pytest.mark.asyncio
    async def test_shutdown_handles_no_tasks(self, mock_components):
        lifecycle = ApplicationLifecycle(mock_components)

        await lifecycle.shutdown()

    @pytest.mark.asyncio
    async def test_run_calls_startup_and_shutdown(self, mock_components):
        lifecycle = ApplicationLifecycle(mock_components)

        with patch.object(lifecycle, "startup", new_callable=AsyncMock) as mock_startup:
            with patch.object(lifecycle, "shutdown", new_callable=AsyncMock) as mock_shutdown:
                with patch.object(lifecycle, "_wait_for_shutdown", new_callable=AsyncMock):
                    with patch.object(lifecycle, "_setup_signal_handlers"):
                        await lifecycle.run()

                        mock_startup.assert_called_once()
                        mock_shutdown.assert_called_once()

    @pytest.mark.asyncio
    async def test_run_handles_cancelled_error(self, mock_components):
        lifecycle = ApplicationLifecycle(mock_components)

        async def raise_cancelled():
            raise asyncio.CancelledError()

        with patch.object(lifecycle, "startup", new_callable=AsyncMock):
            with patch.object(lifecycle, "shutdown", new_callable=AsyncMock) as mock_shutdown:
                with patch.object(lifecycle, "_wait_for_shutdown", side_effect=raise_cancelled):
                    with patch.object(lifecycle, "_setup_signal_handlers"):
                        await lifecycle.run()

                        mock_shutdown.assert_called_once()


class TestRunApi:
    """Tests for _run_api method."""

    @pytest.mark.asyncio
    async def test_creates_uvicorn_config(self, mock_components):
        lifecycle = ApplicationLifecycle(mock_components)

        with patch("src.lifecycle.uvicorn") as mock_uvicorn:
            mock_server = MagicMock()
            mock_server.serve = AsyncMock()
            mock_uvicorn.Server.return_value = mock_server

            await lifecycle._run_api()

            mock_uvicorn.Config.assert_called_once()
            config_call = mock_uvicorn.Config.call_args
            assert config_call[1]["host"] == "0.0.0.0"
            assert config_call[1]["port"] == 8080


class TestSignalHandlers:
    """Tests for signal handler setup."""

    def test_setup_signal_handlers_sets_shutdown_event(self, mock_components):
        lifecycle = ApplicationLifecycle(mock_components)

        if sys.platform != "win32":
            with patch("asyncio.get_running_loop") as mock_loop:
                mock_loop.return_value = MagicMock()
                lifecycle._setup_signal_handlers()
                assert mock_loop.return_value.add_signal_handler.called
        else:
            with patch("signal.signal") as mock_signal:
                lifecycle._setup_signal_handlers()
                mock_signal.assert_called()

    @pytest.mark.asyncio
    async def test_shutdown_event_can_be_set(self, mock_components):
        lifecycle = ApplicationLifecycle(mock_components)

        assert not lifecycle._shutdown_event.is_set()

        lifecycle._shutdown_event.set()

        assert lifecycle._shutdown_event.is_set()


class TestWaitForShutdown:
    """Tests for _wait_for_shutdown method."""

    @pytest.mark.asyncio
    @pytest.mark.skipif(sys.platform == "win32", reason="Unix-specific test")
    async def test_waits_for_shutdown_event_unix(self, mock_components):
        lifecycle = ApplicationLifecycle(mock_components)
        lifecycle._consumer_task = asyncio.create_task(asyncio.sleep(100))
        lifecycle._api_task = asyncio.create_task(asyncio.sleep(100))

        async def set_shutdown():
            await asyncio.sleep(0.1)
            lifecycle._shutdown_event.set()

        asyncio.create_task(set_shutdown())

        await asyncio.wait_for(lifecycle._wait_for_shutdown(), timeout=1.0)

        lifecycle._consumer_task.cancel()
        lifecycle._api_task.cancel()
        try:
            await lifecycle._consumer_task
        except asyncio.CancelledError:
            pass
        try:
            await lifecycle._api_task
        except asyncio.CancelledError:
            pass

    @pytest.mark.asyncio
    @pytest.mark.skipif(sys.platform != "win32", reason="Windows-specific test")
    async def test_waits_for_tasks_windows(self, mock_components):
        lifecycle = ApplicationLifecycle(mock_components)

        async def quick_task():
            await asyncio.sleep(0.1)

        lifecycle._consumer_task = asyncio.create_task(quick_task())
        lifecycle._api_task = asyncio.create_task(quick_task())

        await asyncio.wait_for(lifecycle._wait_for_shutdown(), timeout=1.0)
