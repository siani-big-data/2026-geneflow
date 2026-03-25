"""
GeneFlow Datalake - Entry Point

Consumes ALL events from Redis and persists to JSONL storage.
Exposes REST API for queries and replay.
"""

import asyncio
import signal
import sys

import structlog
import uvicorn

from src.api import DatalakeAPI
from src.config import Settings
from src.consumer import DatalakeConsumer
from src.storage import get_storage_provider

# Configure structured logging
structlog.configure(
    processors=[
        structlog.stdlib.add_log_level,
        structlog.processors.TimeStamper(fmt="iso"),
        structlog.processors.StackInfoRenderer(),
        structlog.processors.format_exc_info,
        structlog.processors.UnicodeDecoder(),
        structlog.processors.JSONRenderer(),
    ],
    wrapper_class=structlog.BoundLogger,
    context_class=dict,
    logger_factory=structlog.PrintLoggerFactory(),
    cache_logger_on_first_use=True,
)

logger = structlog.get_logger()


async def run_api(api: DatalakeAPI, host: str, port: int) -> None:
    """Run the FastAPI server."""
    config = uvicorn.Config(api.app, host=host, port=port, log_level="info")
    server = uvicorn.Server(config)
    await server.serve()


async def main() -> None:
    """Main entry point."""
    logger.info("datalake_starting")

    # Load settings
    settings = Settings()
    logger.info(
        "config_loaded",
        storage=settings.storage_provider,
        redis=settings.redis_url,
    )

    # Create storage provider
    storage = get_storage_provider(
        provider=settings.storage_provider,
        local_storage_path=settings.local_storage_path,
        supabase_url=settings.supabase_url,
        supabase_key=settings.supabase_key,
        supabase_bucket=settings.supabase_bucket,
    )

    # Create consumer
    consumer = DatalakeConsumer(settings=settings, storage=storage)

    # Create API
    api = DatalakeAPI(
        storage=storage,
        settings=settings,
        retry_handler=consumer.retry_handler,
    )
    api.set_consumer_metrics_callback(lambda: consumer.metrics)

    # Setup shutdown
    shutdown_event = asyncio.Event()

    def signal_handler(sig):
        logger.info("shutdown_signal_received", signal=sig)
        shutdown_event.set()

    # Register signal handlers
    if sys.platform != "win32":
        loop = asyncio.get_running_loop()
        for sig in (signal.SIGTERM, signal.SIGINT):
            loop.add_signal_handler(sig, lambda s=sig: signal_handler(s))
    else:
        # Windows: handle SIGINT via exception
        signal.signal(signal.SIGINT, lambda s, f: signal_handler(s))

    # Start services
    consumer_task = asyncio.create_task(consumer.start())
    api_task = asyncio.create_task(run_api(api, settings.api_host, settings.api_port))

    logger.info(
        "services_started",
        api_url=f"http://{settings.api_host}:{settings.api_port}",
    )

    # Wait for shutdown signal or task completion
    try:
        if sys.platform == "win32":
            # Windows: wait for tasks, Ctrl+C raises KeyboardInterrupt
            await asyncio.gather(consumer_task, api_task)
        else:
            # Unix: wait for shutdown event
            done, pending = await asyncio.wait(
                [
                    asyncio.create_task(shutdown_event.wait()),
                    consumer_task,
                    api_task,
                ],
                return_when=asyncio.FIRST_COMPLETED,
            )

            # Cancel pending tasks
            for task in pending:
                task.cancel()

    except asyncio.CancelledError:
        pass

    # Graceful shutdown
    logger.info("graceful_shutdown_starting")
    await consumer.stop()

    # Cancel remaining tasks
    consumer_task.cancel()
    api_task.cancel()

    try:
        await asyncio.gather(consumer_task, api_task, return_exceptions=True)
    except asyncio.CancelledError:
        pass

    logger.info("datalake_stopped")


def run() -> None:
    """Run the service (entry point for pyproject.toml)."""
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        logger.info("keyboard_interrupt")
    except Exception as e:
        logger.error("fatal_error", error=str(e))
        sys.exit(1)


if __name__ == "__main__":
    run()
