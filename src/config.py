"""Settings configuration for GeneFlow AI service."""


from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Application settings with AI_ prefix."""

    model_config = SettingsConfigDict(env_prefix="AI_", env_file=".env")

    # Redis
    redis_url: str = "redis://localhost:6379"
    redis_consumer_group: str = "ai-consumers"
    redis_consumer_name: str = "ai-1"
    redis_block_ms: int = 5000

    # API
    api_host: str = "0.0.0.0"
    api_port: int = 8090
    api_key: str = ""

    # CORS
    cors_origins: list[str] = ["*"]
    cors_allow_credentials: bool = True
    cors_allow_methods: list[str] = ["*"]
    cors_allow_headers: list[str] = ["*"]

    # Claude API (Copilot)
    claude_api_key: str = ""
    claude_model: str = "claude-sonnet-4-20250514"
    claude_max_tokens: int = 4096

    # NCBI BLAST
    blast_email: str = ""
    blast_api_key: str = ""
    blast_base_url: str = "https://blast.ncbi.nlm.nih.gov/Blast.cgi"

    # Event Bus
    eventbus_enabled: bool = True
    eventbus_stream_prefix: str = "geneflow:events"
    eventbus_max_stream_length: int = 100000
    subscribed_categories: list[str] = ["traces", "alignments"]

    # Analysis
    analysis_timeout_seconds: int = 300
    max_sequence_length: int = 100000

    # Logging
    log_level: str = "INFO"


settings = Settings()
