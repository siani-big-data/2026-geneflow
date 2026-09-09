"""Branch coverage tests for src/lifecycle.py."""

import asyncio
from unittest.mock import AsyncMock, MagicMock, patch

import pytest

from src.bootstrap import ApplicationComponents
from src.lifecycle import ApplicationLifecycle


def _make_components():
    settings = MagicMock()
    settings.storage_provider = "local"
    settings.redis_url = "redis://localhost:6379"
    settings.api_host = "0.0.0.0"
    settings.api_port = 8081

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


class TestShutdownEarlyReturns:
    """Cover lines 58-59 and lines 51-53 (cancel tasks)."""

    @pytest.mark.asyncio
    async def test_shutdown_gather_handles_cancelled_error(self):
        """Cover lines 58-59: asyncio.gather raises CancelledError -> swallowed."""
        components = _make_components()
        lifecycle = ApplicationLifecycle(components)

        async def cancelled_coro():
            raise asyncio.CancelledError("boom")

        # Create tasks that are already cancelled
        lifecycle._consumer_task = asyncio.create_task(cancelled_coro())
        lifecycle._api_task = asyncio.create_task(cancelled_coro())

        # Drain
        await asyncio.sleep(0.01)

        async def fake_gather(*args, **kwargs):
            raise asyncio.CancelledError("gather cancelled")

        with patch("asyncio.gather", new=fake_gather):
            # Should not propagate
            await lifecycle.shutdown()


class TestWaitForShutdownBranches:
    """Cover lines 92, 101-102, 108-114 in _wait_for_shutdown and signal handlers."""

    @pytest.mark.asyncio
    async def test_wait_for_shutdown_windows_path(self):
        """Cover line 92: win32 -> gather consumer/api tasks."""
        components = _make_components()
        lifecycle = ApplicationLifecycle(components)

        async def quick():
            await asyncio.sleep(0.01)

        lifecycle._consumer_task = asyncio.create_task(quick())
        lifecycle._api_task = asyncio.create_task(quick())

        with patch("src.lifecycle.sys") as mock_sys:
            mock_sys.platform = "win32"
            await lifecycle._wait_for_shutdown()

    @pytest.mark.asyncio
    async def test_wait_for_shutdown_unix_cancels_pending(self):
        """Cover lines 101-102 and 113-114: Unix path with pending cancellation."""
        components = _make_components()
        lifecycle = ApplicationLifecycle(components)

        # Use slow tasks so shutdown event is what completes
        lifecycle._consumer_task = asyncio.create_task(asyncio.sleep(10))
        lifecycle._api_task = asyncio.create_task(asyncio.sleep(10))

        async def set_event_soon():
            await asyncio.sleep(0.05)
            lifecycle._shutdown_event.set()

        asyncio.create_task(set_event_soon())

        with patch("src.lifecycle.sys") as mock_sys:
            mock_sys.platform = "linux"
            await asyncio.wait_for(lifecycle._wait_for_shutdown(), timeout=1.0)

        # Cleanup
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


class TestSignalHandlersBranches:
    """Cover lines 108-109 / 111-114: signal handler setup."""

    @pytest.mark.asyncio
    async def test_setup_signal_handlers_unix_path(self):
        """Cover lines 108-109: Unix path adds signal handlers via loop."""
        components = _make_components()
        lifecycle = ApplicationLifecycle(components)

        mock_loop = MagicMock()
        with (
            patch("src.lifecycle.sys") as mock_sys,
            patch("asyncio.get_running_loop", return_value=mock_loop),
        ):
            mock_sys.platform = "linux"
            lifecycle._setup_signal_handlers()
            # Should have called add_signal_handler twice (SIGTERM, SIGINT)
            assert mock_loop.add_signal_handler.call_count == 2

    def test_setup_signal_handlers_windows_path(self):
        """Cover lines 113-114: win32 path uses signal.signal."""
        components = _make_components()
        lifecycle = ApplicationLifecycle(components)

        with patch("src.lifecycle.sys") as mock_sys, patch("signal.signal") as mock_signal:
            mock_sys.platform = "win32"
            lifecycle._setup_signal_handlers()
            mock_signal.assert_called_once()

    @pytest.mark.asyncio
    async def test_signal_handler_sets_shutdown_event(self):
        """Cover the inner signal_handler closure setting the shutdown event."""
        components = _make_components()
        lifecycle = ApplicationLifecycle(components)

        captured = {}

        def fake_add_signal_handler(sig, handler):
            # Save handler so we can invoke it
            captured[sig] = handler

        mock_loop = MagicMock()
        mock_loop.add_signal_handler.side_effect = fake_add_signal_handler

        with (
            patch("src.lifecycle.sys") as mock_sys,
            patch("asyncio.get_running_loop", return_value=mock_loop),
        ):
            mock_sys.platform = "linux"
            lifecycle._setup_signal_handlers()

        # Invoke captured handler -> should set the event
        assert not lifecycle._shutdown_event.is_set()
        # Call any captured handler
        list(captured.values())[0]()
        assert lifecycle._shutdown_event.is_set()
