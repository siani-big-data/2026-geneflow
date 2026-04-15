"""Settings configuration for GeneFlow Analysis Worker."""

from typing import Literal

from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Application settings with WORKER_ prefix."""

    model_config = SettingsConfigDict(env_prefix="WORKER_", env_file=".env")

    # Redis
    redis_url: str = "redis://localhost:6379"
    redis_consumer_group: str = "geneflow-analysis-consumers"
    redis_consumer_name: str = "analysis-1"
    redis_block_ms: int = 5000

    # Storage
    storage_provider: Literal["local", "http", "supabase"] = "local"
    local_storage_path: str = "./data/analysis"

    # Supabase (optional)
    supabase_url: str = ""
    supabase_key: str = ""
    supabase_bucket: str = "geneflow-traces"

    # API (Health endpoint)
    api_host: str = "0.0.0.0"
    api_port: int = 8080

    # Workers
    trace_worker_enabled: bool = True
    alignment_worker_enabled: bool = True
    analysis_worker_enabled: bool = True

    # Event Bus
    eventbus_enabled: bool = True
    eventbus_stream_prefix: str = "geneflow:events"
    eventbus_max_stream_length: int = 100000

    # Job Streams
    jobs_stream_prefix: str = "geneflow:jobs"

    # Retry
    max_retries: int = 3
    retry_delay_seconds: float = 5.0

    # Temp files
    temp_dir: str = "/tmp/geneflow-analysis"

    # Logging
    log_level: str = "INFO"
