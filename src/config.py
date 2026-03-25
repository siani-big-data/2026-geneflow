from typing import Literal
from pydantic_settings import BaseSettings

class Settings(BaseSettings):

    # Redis config
    redis_url: str = "redis://redis:6379"
    redis_consumer_group: str = "geneflow-datalake-consumers"
    redis_consumer_name: str = "geneflow-datalake-1"
    redis_block_ms: int = 5000
    redis_stream_prefix: str = "geneflow-datalake:events"
    batch_size: int = 50

    # Storage
    storage_provider: Literal["local", "supabase", "cloudflare"] = "local"
    local_storage_path: str = './data/datalake'
    supabase_url: str = ''
    supabase_key: str = ''
    supabase_bucket: str = 'geneflow-datalake'

    # Buffer
    buffer_max_size: int = 1000
    buffer_flush_interval: float = 5.0
    wal_path: str = "./data/wal"

    # Retry
    retry_max_attempts: int = 5
    retry_base_delay: float = 1.0
    retry_max_delay: float = 300.0
    dlq_path: str = "./data/dlq"

    # Deduplication

    dedup_ttl_hours: int = 24
    dedup_max_size: int = 100000

    # API
    api_host: str = "0.0.0.0"
    api_port: int = 8080
    api_key: str = ""  # If empty, no auth required

    class Config:
        env_prefix = "DATALAKE_"
        env_file = ".env"
