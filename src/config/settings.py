from typing import Literal

from pydantic_settings import BaseSettings

from src.config.constants import (
    BUFFER_DEFAULT_MAX_SIZE,
    BUFFER_FLUSH_INTERVAL_SECONDS,
    RETRY_MAX_ATTEMPTS,
    RETRY_BASE_DELAY_SECONDS,
    RETRY_MAX_DELAY_SECONDS,
    DEDUP_TTL_HOURS,
    DEDUP_MAX_SIZE,
    REDIS_BLOCK_MS,
    REDIS_BATCH_SIZE,
)


class Settings(BaseSettings):
    """Datalake configuration loaded from environment variables with DATALAKE_ prefix."""

    redis_url: str = "redis://redis:6379"
    redis_consumer_group: str = "geneflow-datalake-consumers"
    redis_consumer_name: str = "geneflow-datalake-1"
    redis_block_ms: int = REDIS_BLOCK_MS
    redis_stream_prefix: str = "geneflow-datalake:events"
    batch_size: int = REDIS_BATCH_SIZE

    storage_provider: Literal["local", "supabase", "minio"] = "local"
    local_storage_path: str = "./data/datalake"

    minio_endpoint: str = ""
    minio_access_key: str = ""
    minio_secret_key: str = ""
    minio_bucket: str = "geneflow-datalake"
    minio_secure: bool = True

    buffer_max_size: int = BUFFER_DEFAULT_MAX_SIZE
    buffer_flush_interval: float = BUFFER_FLUSH_INTERVAL_SECONDS
    wal_path: str = "./data/wal"

    retry_max_attempts: int = RETRY_MAX_ATTEMPTS
    retry_base_delay: float = RETRY_BASE_DELAY_SECONDS
    retry_max_delay: float = RETRY_MAX_DELAY_SECONDS
    dlq_path: str = "./data/dlq"

    dedup_ttl_hours: int = DEDUP_TTL_HOURS
    dedup_max_size: int = DEDUP_MAX_SIZE

    api_host: str = "0.0.0.0"
    api_port: int = 8080
    api_key: str = ""

    cors_origins: list[str] = ["*"]
    cors_allow_credentials: bool = True
    cors_allow_methods: list[str] = ["*"]
    cors_allow_headers: list[str] = ["*"]

    log_level: str = "INFO"
    log_requests: bool = True

    postgres_dsn: str = ""
    postgres_enabled: bool = False

    qdrant_url: str = "http://localhost:6333"
    qdrant_api_key: str = ""
    qdrant_enabled: bool = False

    model_config = {
        "env_prefix": "DATALAKE_",
        "env_file": ".env",
    }
