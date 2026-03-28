"""Entry point for GeneFlow Analysis Worker."""

import asyncio
import signal
import sys
from contextlib import asynccontextmanager

import structlog
import uvicorn
from redis.asyncio import Redis

from src.api import app, register_workers, set_redis_health
from src.config import settings
from src.events.events import WorkerStarted, WorkerStopped
from src.events.publisher import EventBusPublisher
from src.workers import AlignmentWorker, AnalysisWorker, TraceWorker

# Configure structlog
structlog.configure(
    processors=[
        structlog.stdlib.filter_by_level,
        structlog.stdlib.add_logger_name,
        structlog.stdlib.add_log_level,
        structlog.stdlib.PositionalArgumentsFormatter(),
        structlog.processors.TimeStamper(fmt="iso"),
        structlog.processors.StackInfoRenderer(),
        structlog.processors.format_exc_info,
        structlog.processors.UnicodeDecoder(),
        structlog.processors.JSONRenderer(),
    ],
    wrapper_class=structlog.stdlib.BoundLogger,
    context_class=dict,
    logger_factory=structlog.stdlib.LoggerFactory(),
    cache_logger_on_first_use=True,
)

logger = structlog.get_logger()


class WorkerManager:
    """Manages worker lifecycle."""

    def __init__(self):
        self._redis: Redis | None = None
        self._publisher: EventBusPublisher | None = None
        self._workers: dict = {}
        self._tasks: list[asyncio.Task] = []
        self._shutdown_event = asyncio.Event()

    async def start(self) -> None:
        """Start all workers."""
        logger.info("worker_manager_starting")

        # Connect to Redis
        self._redis = Redis.from_url(
            settings.redis_url,
            decode_responses=False,
        )

        # Test connection
        try:
            await self._redis.ping()
            set_redis_health(True)
            logger.info("redis_connected", url=settings.redis_url)
        except Exception as e:
            logger.error("redis_connection_failed", error=str(e))
            set_redis_health(False)
            raise

        # Create publisher
        self._publisher = EventBusPublisher(self._redis, settings)

        # Create workers
        enabled_workers = []

        if settings.trace_worker_enabled:
            worker = TraceWorker(self._redis, self._publisher, settings)
            self._workers["trace"] = worker
            enabled_workers.append("trace")

        if settings.alignment_worker_enabled:
            worker = AlignmentWorker(self._redis, self._publisher, settings)
            self._workers["alignment"] = worker
            enabled_workers.append("alignment")

        if settings.analysis_worker_enabled:
            worker = AnalysisWorker(self._redis, self._publisher, settings)
            self._workers["analysis"] = worker
            enabled_workers.append("analysis")

        # Register workers for health monitoring
        register_workers(self._workers)

        # Publish start event
        await self._publisher.publish(
            WorkerStarted(
                workerName="geneflow-analysis",
                workerId=settings.redis_consumer_name,
                enabledWorkers=enabled_workers,
            )
        )

        # Start worker tasks
        for name, worker in self._workers.items():
            task = asyncio.create_task(
                worker.start(),
                name=f"worker-{name}",
            )
            self._tasks.append(task)

        logger.info(
            "workers_started",
            count=len(self._workers),
            workers=list(self._workers.keys()),
        )

    async def stop(self) -> None:
        """Stop all workers gracefully."""
        logger.info("worker_manager_stopping")

        # Calculate total jobs processed
        total_jobs = sum(w.metrics.jobsProcessed for w in self._workers.values())

        # Stop workers
        for worker in self._workers.values():
            await worker.stop()

        # Cancel tasks
        for task in self._tasks:
            task.cancel()

        # Wait for tasks to complete
        if self._tasks:
            await asyncio.gather(*self._tasks, return_exceptions=True)

        # Publish stop event
        if self._publisher:
            await self._publisher.publish(
                WorkerStopped(
                    workerName="geneflow-analysis",
                    workerId=settings.redis_consumer_name,
                    reason="shutdown",
                    jobsProcessed=total_jobs,
                )
            )

        # Close Redis connection
        if self._redis:
            await self._redis.close()
            set_redis_health(False)

        logger.info(
            "worker_manager_stopped",
            total_jobs_processed=total_jobs,
        )

    async def wait_for_shutdown(self) -> None:
        """Wait for shutdown signal."""
        await self._shutdown_event.wait()

    def signal_shutdown(self) -> None:
        """Signal shutdown."""
        self._shutdown_event.set()


# Global manager instance
manager = WorkerManager()


def handle_signal(sig: signal.Signals) -> None:
    """Handle shutdown signals."""
    logger.info("shutdown_signal_received", signal=sig.name)
    manager.signal_shutdown()


@asynccontextmanager
async def lifespan(app):
    """FastAPI lifespan manager."""
    # Start workers
    await manager.start()

    # Setup signal handlers
    loop = asyncio.get_event_loop()
    for sig in (signal.SIGTERM, signal.SIGINT):
        try:
            loop.add_signal_handler(sig, lambda s=sig: handle_signal(s))
        except NotImplementedError:
            # Windows doesn't support add_signal_handler
            signal.signal(sig, lambda s, f: handle_signal(signal.Signals(s)))

    yield

    # Stop workers
    await manager.stop()


# Update app with lifespan
app.router.lifespan_context = lifespan


async def main() -> None:
    """Main entry point."""
    logger.info(
        "geneflow_analysis_starting",
        redis_url=settings.redis_url,
        api_port=settings.api_port,
        trace_worker=settings.trace_worker_enabled,
        alignment_worker=settings.alignment_worker_enabled,
        analysis_worker=settings.analysis_worker_enabled,
    )

    # Run uvicorn with the app
    config = uvicorn.Config(
        app,
        host=settings.api_host,
        port=settings.api_port,
        log_level=settings.log_level.lower(),
    )
    server = uvicorn.Server(config)
    await server.serve()


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        logger.info("shutdown_keyboard_interrupt")
        sys.exit(0)
