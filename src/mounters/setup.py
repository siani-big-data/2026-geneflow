"""Setup and registration for mounters."""

import structlog

from src.config import Settings
from src.mounters.engine import MounterEngine
from src.mounters.postgres import PostgresMounter
from src.mounters.qdrant import QdrantMounter
from src.mounters.storage import StorageMounter

logger = structlog.get_logger()


def setup_mounters(settings: Settings, datalake_path: str | None = None) -> MounterEngine:
    """
    Create and configure the mounter engine with all registered mounters.

    Args:
        settings: Application settings
        datalake_path: Path to datalake for replay mode

    Returns:
        Configured MounterEngine with all mounters registered
    """
    engine = MounterEngine(datalake_path=datalake_path)

    if settings.postgres_enabled and settings.postgres_dsn:
        postgres_mounter = PostgresMounter(dsn=settings.postgres_dsn)
        engine.register(postgres_mounter)
        logger.info("postgres_mounter_registered")

    if settings.minio_endpoint:
        storage_mounter = StorageMounter(
            endpoint_url=settings.minio_endpoint,
            access_key=settings.minio_access_key,
            secret_key=settings.minio_secret_key,
            bucket=settings.minio_bucket,
        )
        engine.register(storage_mounter)
        logger.info("storage_mounter_registered")

    if settings.qdrant_enabled:
        qdrant_mounter = QdrantMounter(
            qdrant_url=settings.qdrant_url,
            qdrant_api_key=settings.qdrant_api_key if settings.qdrant_api_key else None,
        )
        engine.register(qdrant_mounter)
        logger.info("qdrant_mounter_registered")

    return engine
