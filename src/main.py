"""Entry point for GeneFlow AI service."""

import asyncio
import signal
import sys
from contextlib import asynccontextmanager

import structlog
import uvicorn
from redis.asyncio import Redis

from src.api import (
    app,
    set_claude_configured,
    set_metrics,
    set_redis_health,
    set_service_status,
)
from src.config import settings
from src.events import EventBusPublisher
from src.models import ServiceMetrics, ServiceStatus

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


class AIService:
    """Main AI service that coordinates all components."""

    def __init__(self):
        self._redis: Redis | None = None
        self._publisher: EventBusPublisher | None = None
        self._metrics = ServiceMetrics()
        self._shutdown_event = asyncio.Event()

    async def start(self) -> None:
        """Start the AI service."""
        logger.info("ai_service_starting")
        set_service_status(ServiceStatus.STARTING)

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

        # Check Claude configuration
        claude_configured = bool(settings.claude_api_key)
        set_claude_configured(claude_configured)
        if claude_configured:
            logger.info("claude_api_configured", model=settings.claude_model)
        else:
            logger.warning("claude_api_not_configured")

        set_service_status(ServiceStatus.RUNNING)
        logger.info(
            "ai_service_started",
            api_port=settings.api_port,
            eventbus_enabled=settings.eventbus_enabled,
        )

    async def stop(self) -> None:
        """Stop the AI service gracefully."""
        logger.info("ai_service_stopping")
        set_service_status(ServiceStatus.STOPPING)

        # Close Redis connection
        if self._redis:
            await self._redis.close()
            set_redis_health(False)

        set_service_status(ServiceStatus.STOPPED)
        logger.info(
            "ai_service_stopped",
            analyses_completed=self._metrics.analysesCompleted,
            analyses_failed=self._metrics.analysesFailed,
        )

    async def wait_for_shutdown(self) -> None:
        """Wait for shutdown signal."""
        await self._shutdown_event.wait()

    def signal_shutdown(self) -> None:
        """Signal shutdown."""
        self._shutdown_event.set()

    def update_metrics(self) -> None:
        """Update metrics in API."""
        set_metrics(self._metrics.to_dict())


# Global service instance
service = AIService()


def handle_signal(sig: signal.Signals) -> None:
    """Handle shutdown signals."""
    logger.info("shutdown_signal_received", signal=sig.name)
    service.signal_shutdown()


@asynccontextmanager
async def lifespan(app):
    """FastAPI lifespan manager."""
    # Start service
    await service.start()

    # Setup signal handlers
    loop = asyncio.get_event_loop()
    for sig in (signal.SIGTERM, signal.SIGINT):
        try:
            loop.add_signal_handler(sig, lambda s=sig: handle_signal(s))
        except NotImplementedError:
            # Windows doesn't support add_signal_handler
            signal.signal(sig, lambda s, f: handle_signal(signal.Signals(s)))

    yield

    # Stop service
    await service.stop()


# Update app with lifespan
app.router.lifespan_context = lifespan


async def main() -> None:
    """Main entry point."""
    logger.info(
        "geneflow_ai_starting",
        redis_url=settings.redis_url,
        api_port=settings.api_port,
        eventbus_enabled=settings.eventbus_enabled,
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
