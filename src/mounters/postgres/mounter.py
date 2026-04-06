"""PostgreSQL mounter for projecting events to relational tables.

This mounter:
1. Initializes database schemas on startup
2. Routes events to domain-specific handlers
3. Projects events into PostgreSQL tables for query optimization
"""

import json

import structlog

from src.mounters.base import BaseMounter
from src.mounters.postgres.connection import PostgresConnection
from src.mounters.postgres.handlers.alignments import AlignmentsHandler
from src.mounters.postgres.handlers.billing import BillingHandler
from src.mounters.postgres.handlers.profiles import ProfilesHandler
from src.mounters.postgres.handlers.studies import StudiesHandler
from src.mounters.postgres.handlers.traces import TracesHandler
from src.mounters.postgres.handlers.users import UsersHandler
from src.mounters.postgres.schemas.alignments import ALIGNMENTS_SCHEMA
from src.mounters.postgres.schemas.billing import BILLING_SCHEMA
from src.mounters.postgres.schemas.profiles import PROFILES_SCHEMA
from src.mounters.postgres.schemas.studies import STUDIES_SCHEMA
from src.mounters.postgres.schemas.traces import TRACES_SCHEMA
from src.mounters.postgres.schemas.users import USERS_SCHEMA

logger = structlog.get_logger()


class PostgresMounter(BaseMounter):
    """Mounter for projecting events to PostgreSQL.

    Handles events from multiple bounded contexts:
    - users (Identity)
    - studies
    - traces
    - alignments
    - billing (plans + subscriptions)
    """

    def __init__(self, dsn: str):
        super().__init__(
            name="postgres",
            categories=["users", "studies", "traces", "alignments", "billing", "profiles"],
        )
        self._dsn = dsn
        self._connection = PostgresConnection(dsn=dsn)
        self._handlers: dict = {}

    async def start(self) -> None:
        """Start the mounter - connect to PostgreSQL and initialize schemas."""
        await self._connection.connect()

        # Initialize schemas
        await self._initialize_schemas()

        # Initialize handlers
        self._handlers = {
            "users": UsersHandler(self._connection),
            "studies": StudiesHandler(self._connection),
            "traces": TracesHandler(self._connection),
            "alignments": AlignmentsHandler(self._connection),
            "billing": BillingHandler(self._connection),
            "profiles": ProfilesHandler(self._connection),
        }

        self._running = True
        logger.info("postgres_mounter_started", categories=self._categories)

    async def stop(self) -> None:
        """Stop the mounter - close PostgreSQL connection."""
        await self._connection.close()
        self._running = False
        self._handlers = {}
        logger.info("postgres_mounter_stopped")

    async def _initialize_schemas(self) -> None:
        """Initialize all database schemas."""
        schemas = [
            ("users", USERS_SCHEMA),
            ("studies", STUDIES_SCHEMA),
            ("traces", TRACES_SCHEMA),
            ("alignments", ALIGNMENTS_SCHEMA),
            ("billing", BILLING_SCHEMA),
            ("profiles", PROFILES_SCHEMA),
        ]

        for name, schema in schemas:
            try:
                # Execute each statement in the schema separately
                statements = [s.strip() for s in schema.split(";") if s.strip()]
                for statement in statements:
                    if statement and not statement.startswith("--"):
                        await self._connection.execute(statement)
                logger.info("schema_initialized", schema=name)
            except Exception as e:
                logger.error("schema_initialization_failed", schema=name, error=str(e))
                raise

    async def handle_event(self, event: dict) -> None:
        """Handle an incoming event by routing to appropriate handler.

        Args:
            event: Event dictionary with structure:
                {
                    "event_id": "uuid",
                    "event_type": "UserRegisteredEvent",
                    "category": "users",
                    "occurred_at": "2024-01-01T00:00:00Z",
                    "data": { ... event payload ... }
                }
        """
        category = event.get("category")
        event_type = event.get("event_type")
        event_id = event.get("event_id")

        handler = self._handlers.get(category)
        if not handler:
            logger.warning(
                "no_handler_for_category",
                category=category,
                event_type=event_type,
            )
            self._metrics["events_skipped"] += 1
            return

        # Get the payload - it might be in 'data' field as JSON string or dict
        payload = event.get("data", {})
        if isinstance(payload, str):
            try:
                payload = json.loads(payload)
            except json.JSONDecodeError:
                logger.error("invalid_event_payload", event_id=event_id)
                self._metrics["events_failed"] += 1
                return

        # Add metadata to payload for handlers
        payload["occurred_at"] = event.get("occurred_at")
        payload["event_id"] = event_id

        try:
            await handler.handle(event_type, payload)
            self._metrics["events_processed"] += 1
            logger.debug(
                "event_processed",
                event_type=event_type,
                category=category,
                event_id=event_id,
            )
        except Exception as e:
            logger.error(
                "event_processing_failed",
                event_type=event_type,
                category=category,
                event_id=event_id,
                error=str(e),
            )
            self._metrics["events_failed"] += 1
            raise

    async def health_check(self) -> bool:
        """Check if PostgreSQL connection is healthy."""
        return await self._connection.health_check()

    async def rebuild(self, categories: list[str] | None = None) -> None:
        """Rebuild projections by truncating and replaying events.

        Args:
            categories: Optional list of categories to rebuild. If None, rebuilds all.
        """
        target_categories = categories or self._categories

        for category in target_categories:
            handler = self._handlers.get(category)
            if handler and hasattr(handler, "truncate"):
                await handler.truncate()
                logger.info("category_truncated", category=category)

        self._metrics["events_processed"] = 0
        self._metrics["events_failed"] = 0
        self._metrics["events_skipped"] = 0

        logger.info("postgres_mounter_rebuild_started", categories=target_categories)
