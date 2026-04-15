"""Application bootstrap - component creation and wiring."""

from typing import TYPE_CHECKING

from redis.asyncio import Redis

from src.api import AnalysisAPI
from src.config import Settings
from src.events.publisher import EventBusPublisher
from src.storage.factory import StorageFactory
from src.workers import AlignmentWorker, AnalysisWorker, TraceWorker

if TYPE_CHECKING:
    from src.storage.base import BaseStorageProvider
    from src.workers.base import BaseWorker


class ApplicationComponents:
    """Container for all application components."""

    def __init__(
        self,
        settings: Settings,
        redis: Redis,
        publisher: EventBusPublisher,
        storage: "BaseStorageProvider",
        workers: dict[str, "BaseWorker"],
        api: AnalysisAPI,
    ):
        self.settings = settings
        self.redis = redis
        self.publisher = publisher
        self.storage = storage
        self.workers = workers
        self.api = api


def create_redis(settings: Settings) -> Redis:
    """Create Redis client from settings.

    Args:
        settings: Application settings.

    Returns:
        Redis client instance (not yet connected).
    """
    return Redis.from_url(
        settings.redis_url,
        decode_responses=False,
    )


def create_publisher(redis: Redis, settings: Settings) -> EventBusPublisher:
    """Create event bus publisher.

    Args:
        redis: Redis client.
        settings: Application settings.

    Returns:
        EventBusPublisher instance.
    """
    return EventBusPublisher(redis, settings)


def create_storage(settings: Settings) -> "BaseStorageProvider":
    """Create storage provider based on settings.

    Args:
        settings: Application settings.

    Returns:
        Storage provider instance.
    """
    return StorageFactory.create(settings)


def create_workers(
    redis: Redis,
    publisher: EventBusPublisher,
    settings: Settings,
) -> dict[str, "BaseWorker"]:
    """Create enabled workers based on settings.

    Args:
        redis: Redis client.
        publisher: Event bus publisher.
        settings: Application settings.

    Returns:
        Dictionary of worker name to worker instance.
    """
    workers: dict[str, "BaseWorker"] = {}

    if settings.trace_worker_enabled:
        workers["trace"] = TraceWorker(redis, publisher, settings)

    if settings.alignment_worker_enabled:
        workers["alignment"] = AlignmentWorker(redis, publisher, settings)

    if settings.analysis_worker_enabled:
        workers["analysis"] = AnalysisWorker(redis, publisher, settings)

    return workers


def create_api(settings: Settings) -> AnalysisAPI:
    """Create the Analysis API.

    Args:
        settings: Application settings.

    Returns:
        AnalysisAPI instance.
    """
    return AnalysisAPI(settings)


def bootstrap(settings: Settings | None = None) -> ApplicationComponents:
    """Bootstrap all application components.

    Creates and wires together all components needed to run the application.
    Note: Redis connection is not established here - call redis.ping() to connect.

    Args:
        settings: Optional settings instance. If None, creates from environment.

    Returns:
        ApplicationComponents containing all wired components.
    """
    if settings is None:
        settings = Settings()

    redis = create_redis(settings)
    publisher = create_publisher(redis, settings)
    storage = create_storage(settings)
    workers = create_workers(redis, publisher, settings)
    api = create_api(settings)

    # Register workers with API for health monitoring
    api.register_workers(workers)

    return ApplicationComponents(
        settings=settings,
        redis=redis,
        publisher=publisher,
        storage=storage,
        workers=workers,
        api=api,
    )
