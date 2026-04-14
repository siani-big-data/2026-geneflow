"""Application lifecycle management - startup and shutdown."""

import asyncio
import signal
import sys

import structlog
import uvicorn

from src.bootstrap import ApplicationComponents

logger = structlog.get_logger()


class ApplicationLifecycle:
    """Manages application startup and shutdown."""

    def __init__(self, components: ApplicationComponents):
        self.components = components
        self._shutdown_event = asyncio.Event()
        self._consumer_task: asyncio.Task | None = None
        self._api_task: asyncio.Task | None = None

    async def startup(self) -> None:
        """Start all application services."""
        logger.info(
            "application_starting",
            storage=self.components.settings.storage_provider,
            redis=self.components.settings.redis_url,
        )

        await self.components.mounter_engine.start()

        self._consumer_task = asyncio.create_task(self.components.consumer.start())
        self._api_task = asyncio.create_task(self._run_api())

        logger.info(
            "application_started",
            api_url=f"http://{self.components.settings.api_host}:{self.components.settings.api_port}",
        )

    async def shutdown(self) -> None:
        """Gracefully shutdown all services."""
        logger.info("application_stopping")

        await self.components.consumer.stop()
        await self.components.mounter_engine.stop()

        if self._consumer_task:
            self._consumer_task.cancel()
        if self._api_task:
            self._api_task.cancel()

        tasks = [t for t in [self._consumer_task, self._api_task] if t]
        if tasks:
            try:
                await asyncio.gather(*tasks, return_exceptions=True)
            except asyncio.CancelledError:
                pass

        logger.info("application_stopped")

    async def run(self) -> None:
        """Run the application until shutdown signal."""
        self._setup_signal_handlers()

        await self.startup()

        try:
            await self._wait_for_shutdown()
        except asyncio.CancelledError:
            pass
        finally:
            await self.shutdown()

    async def _run_api(self) -> None:
        """Run the FastAPI server."""
        config = uvicorn.Config(
            self.components.api.app,
            host=self.components.settings.api_host,
            port=self.components.settings.api_port,
            log_level="info",
        )
        server = uvicorn.Server(config)
        await server.serve()

    async def _wait_for_shutdown(self) -> None:
        """Wait for shutdown signal or task completion."""
        if sys.platform == "win32":
            await asyncio.gather(self._consumer_task, self._api_task)
        else:
            done, pending = await asyncio.wait(
                [
                    asyncio.create_task(self._shutdown_event.wait()),
                    self._consumer_task,
                    self._api_task,
                ],
                return_when=asyncio.FIRST_COMPLETED,
            )

            for task in pending:
                task.cancel()

    def _setup_signal_handlers(self) -> None:
        """Setup signal handlers for graceful shutdown."""
        def signal_handler(sig):
            logger.info("shutdown_signal_received", signal=sig)
            self._shutdown_event.set()

        if sys.platform != "win32":
            loop = asyncio.get_running_loop()
            for sig in (signal.SIGTERM, signal.SIGINT):
                loop.add_signal_handler(sig, lambda s=sig: signal_handler(s))
        else:
            signal.signal(signal.SIGINT, lambda s, f: signal_handler(s))
