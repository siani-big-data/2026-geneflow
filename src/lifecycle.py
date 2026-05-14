"""Application lifecycle management - startup and shutdown."""

import asyncio
import signal
import sys

import structlog
import uvicorn

from src.bootstrap import ApplicationComponents
from src.events.events import WorkerStarted, WorkerStopped

logger = structlog.get_logger()


class ApplicationLifecycle:
    """Manages application startup and shutdown."""

    def __init__(self, components: ApplicationComponents):
        self.components = components
        self._shutdown_event = asyncio.Event()
        self._worker_tasks: list[asyncio.Task] = []
        self._api_task: asyncio.Task | None = None

    async def startup(self) -> None:
        """Start all application services."""
        logger.info(
            "application_starting",
            storage=self.components.settings.storage_provider,
            redis=self.components.settings.redis_url,
        )

        try:
            await self.components.redis.ping()
            self.components.api.set_redis_health(True)
            logger.info("redis_connected", url=self.components.settings.redis_url)
        except Exception as e:
            logger.error("redis_connection_failed", error=str(e))
            self.components.api.set_redis_health(False)
            raise

        enabled_workers = list(self.components.workers.keys())
        await self.components.publisher.publish(
            WorkerStarted(
                workerName="geneflow-analysis",
                workerId=self.components.settings.redis_consumer_name,
                enabledWorkers=enabled_workers,
            )
        )

        for name, worker in self.components.workers.items():
            task = asyncio.create_task(
                worker.start(),
                name=f"worker-{name}",
            )
            self._worker_tasks.append(task)

        self._api_task = asyncio.create_task(self._run_api())

        logger.info(
            "application_started",
            api_url=f"http://{self.components.settings.api_host}:{self.components.settings.api_port}",
            workers=enabled_workers,
        )

    async def shutdown(self) -> None:
        """Gracefully shutdown all services."""
        logger.info("application_stopping")

        total_jobs = sum(w.metrics.jobsProcessed for w in self.components.workers.values())

        for worker in self.components.workers.values():
            await worker.stop()

        for task in self._worker_tasks:
            task.cancel()

        if self._api_task:
            self._api_task.cancel()

        all_tasks = self._worker_tasks + ([self._api_task] if self._api_task else [])
        if all_tasks:
            try:
                await asyncio.gather(*all_tasks, return_exceptions=True)
            except asyncio.CancelledError:
                pass

        try:
            await self.components.publisher.publish(
                WorkerStopped(
                    workerName="geneflow-analysis",
                    workerId=self.components.settings.redis_consumer_name,
                    reason="shutdown",
                    jobsProcessed=total_jobs,
                )
            )
        except Exception as e:
            logger.warning("failed_to_publish_stop_event", error=str(e))

        await self.components.redis.close()
        self.components.api.set_redis_health(False)

        logger.info(
            "application_stopped",
            total_jobs_processed=total_jobs,
        )

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
            log_level=self.components.settings.log_level.lower(),
        )
        server = uvicorn.Server(config)
        await server.serve()

    async def _wait_for_shutdown(self) -> None:
        """Wait for shutdown signal or task completion."""
        if sys.platform == "win32":
            all_tasks = self._worker_tasks + ([self._api_task] if self._api_task else [])
            await asyncio.gather(*all_tasks)
        else:
            done, pending = await asyncio.wait(
                [
                    asyncio.create_task(self._shutdown_event.wait()),
                    *self._worker_tasks,
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
