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
    set_agent,
    set_blast_client,
    set_chat_handler,
    set_claude_configured,
    set_metrics,
    set_redis_health,
    set_report_generator,
    set_service_status,
)
from src.blast import BlastClient
from src.config import settings
from src.copilot import ChatHandler, ClaudeClient, MolecularBiologyAgent, ReportGenerator
from src.events import EventBusConsumer, EventBusPublisher
from src.models import AnalysisResult, ServiceMetrics, ServiceStatus

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
        self._consumer: EventBusConsumer | None = None
        self._consumer_task: asyncio.Task | None = None
        self._claude_client: ClaudeClient | None = None
        self._blast_client: BlastClient | None = None
        self._chat_handler: ChatHandler | None = None
        self._report_generator: ReportGenerator | None = None
        self._agent: MolecularBiologyAgent | None = None
        self._metrics = ServiceMetrics()
        self._shutdown_event = asyncio.Event()
        # Cache for analysis results (trace_id -> AnalysisResult)
        self._analysis_cache: dict[str, AnalysisResult] = {}

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

        # Initialize Copilot components
        self._claude_client = ClaudeClient(settings)
        self._chat_handler = ChatHandler(self._claude_client, settings)
        self._report_generator = ReportGenerator(self._claude_client, settings)

        # Set up API handlers
        set_chat_handler(self._chat_handler)
        set_report_generator(self._report_generator)

        claude_configured = self._claude_client.is_configured
        set_claude_configured(claude_configured)
        if claude_configured:
            logger.info("claude_api_configured", model=settings.claude_model)
        else:
            logger.warning("claude_api_not_configured")

        # Initialize BLAST client
        self._blast_client = BlastClient(settings)
        set_blast_client(self._blast_client)
        if self._blast_client.is_configured:
            logger.info("blast_client_configured", email=settings.blast_email)
        else:
            logger.warning("blast_client_not_configured")

        # Initialize Molecular Biology Agent
        self._agent = MolecularBiologyAgent(
            settings=settings,
            trace_provider=self._get_trace_analysis,
            blast_handler=self._handle_blast_tool,
        )
        set_agent(self._agent)
        logger.info(
            "agent_initialized",
            available=self._agent.is_available,
        )

        # Start consumer if eventbus enabled
        if settings.eventbus_enabled:
            self._consumer = EventBusConsumer(self._redis, settings)
            self._consumer.register_handler("trace.processed", self._handle_trace_processed)
            self._consumer.register_handler("alignment.completed", self._handle_alignment_completed)
            self._consumer_task = asyncio.create_task(self._consumer.start())
            logger.info("eventbus_consumer_started")

        set_service_status(ServiceStatus.RUNNING)
        logger.info(
            "ai_service_started",
            api_port=settings.api_port,
            eventbus_enabled=settings.eventbus_enabled,
            copilot_available=claude_configured,
            blast_available=self._blast_client.is_configured,
            agent_available=self._agent.is_available,
        )

    async def stop(self) -> None:
        """Stop the AI service gracefully."""
        logger.info("ai_service_stopping")
        set_service_status(ServiceStatus.STOPPING)

        # Stop consumer
        if self._consumer:
            await self._consumer.stop()
        if self._consumer_task:
            self._consumer_task.cancel()
            try:
                await self._consumer_task
            except asyncio.CancelledError:
                pass

        # Close BLAST client
        if self._blast_client:
            await self._blast_client.close()

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

    async def _handle_trace_processed(self, event: dict) -> None:
        """Handle TraceProcessed events from geneflow-datalake."""
        trace_id = event.get("data", {}).get("traceId", "unknown")
        logger.info("trace_processed_received", trace_id=trace_id)

        # TODO: Implement full analysis pipeline
        # For now, just log the event

    async def _handle_alignment_completed(self, event: dict) -> None:
        """Handle AlignmentCompleted events from geneflow-analysis."""
        trace_id = event.get("data", {}).get("traceId", "unknown")
        logger.info("alignment_completed_received", trace_id=trace_id)

        # TODO: Implement analysis on alignment results

    def _get_trace_analysis(self, trace_id: str) -> AnalysisResult | None:
        """
        Get analysis result for a trace.

        This is the trace provider for the agent.
        Currently uses local cache, but could be extended to
        fetch from Datalake or other sources.
        """
        return self._analysis_cache.get(trace_id)

    def cache_analysis(self, trace_id: str, result: AnalysisResult) -> None:
        """Cache an analysis result for agent access."""
        self._analysis_cache[trace_id] = result
        logger.debug("analysis_cached", trace_id=trace_id)

    async def _handle_blast_tool(self, params: dict) -> dict:
        """
        Handle BLAST search requests from the agent.

        This connects the agent's search_blast tool to the BLAST client.
        """
        if not self._blast_client or not self._blast_client.is_configured:
            return {"error": "BLAST no configurado"}

        try:
            sequence = params.get("sequence", "")
            program = params.get("program", "blastn")
            database = params.get("database", "nt")

            result = await self._blast_client.search(
                sequence=sequence,
                program=program,
                database=database,
                timeout_seconds=120,  # Shorter timeout for tool use
            )

            # Format results for agent
            hits = []
            for hit in result.hits[:5]:  # Top 5 hits
                hits.append(
                    {
                        "accession": hit.accession,
                        "description": hit.description,
                        "organism": hit.organism,
                        "identity": hit.identity,
                        "eValue": hit.eValue,
                    }
                )

            return {
                "rid": result.rid,
                "hitCount": result.hitCount,
                "queryLength": result.queryLength,
                "hits": hits,
            }

        except Exception as e:
            logger.error("blast_tool_error", error=str(e))
            return {"error": str(e)}


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
