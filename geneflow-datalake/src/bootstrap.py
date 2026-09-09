"""Application bootstrap - component creation and wiring."""

from src.api import DatalakeAPI
from src.config import Settings
from src.consumer import DatalakeConsumer
from src.mounters.setup import setup_mounters
from src.storage import get_storage_provider


class ApplicationComponents:
    """Container for all application components."""

    def __init__(
        self,
        settings: Settings,
        storage,
        mounter_engine,
        consumer: DatalakeConsumer,
        api: DatalakeAPI,
    ):
        self.settings = settings
        self.storage = storage
        self.mounter_engine = mounter_engine
        self.consumer = consumer
        self.api = api


def create_storage(settings: Settings):
    """Create storage provider based on settings."""
    return get_storage_provider(
        provider=settings.storage_provider,
        local_storage_path=settings.local_storage_path,
        minio_endpoint=settings.minio_endpoint,
        minio_access_key=settings.minio_access_key,
        minio_secret_key=settings.minio_secret_key,
        minio_bucket=settings.minio_bucket,
        minio_secure=settings.minio_secure,
    )


def create_mounter_engine(settings: Settings):
    """Create mounter engine with all mounters."""
    return setup_mounters(settings, datalake_path=settings.local_storage_path)


def create_consumer(settings: Settings, storage, mounter_engine) -> DatalakeConsumer:
    """Create datalake consumer."""
    return DatalakeConsumer(
        settings=settings,
        storage=storage,
        mounter_engine=mounter_engine,
    )


def create_api(settings: Settings, storage, consumer: DatalakeConsumer) -> DatalakeAPI:
    """Create API with consumer metrics."""
    api = DatalakeAPI(
        storage=storage,
        settings=settings,
        retry_handler=consumer.retry_handler,
    )
    api.set_consumer_metrics_callback(lambda: consumer.metrics)
    return api


def bootstrap(settings: Settings | None = None) -> ApplicationComponents:
    """Bootstrap all application components.

    Creates and wires together all components needed to run the application.
    """
    if settings is None:
        settings = Settings()

    storage = create_storage(settings)
    mounter_engine = create_mounter_engine(settings)
    consumer = create_consumer(settings, storage, mounter_engine)
    api = create_api(settings, storage, consumer)

    return ApplicationComponents(
        settings=settings,
        storage=storage,
        mounter_engine=mounter_engine,
        consumer=consumer,
        api=api,
    )
