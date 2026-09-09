"""Tests for configuration."""

import os

from src.config import Settings


class TestSettings:
    """Test Settings class."""

    def test_default_values(self):
        """Test default configuration values."""
        settings = Settings()

        assert settings.redis_url == "redis://localhost:6379"
        assert settings.api_port == 8090
        assert settings.eventbus_enabled is True
        assert settings.claude_model == "claude-sonnet-4-20250514"

    def test_env_prefix(self):
        """Test that AI_ prefix is used for env vars."""
        os.environ["AI_API_PORT"] = "9000"
        os.environ["AI_LOG_LEVEL"] = "DEBUG"

        try:
            settings = Settings()
            assert settings.api_port == 9000
            assert settings.log_level == "DEBUG"
        finally:
            del os.environ["AI_API_PORT"]
            del os.environ["AI_LOG_LEVEL"]

    def test_cors_defaults(self):
        """Test CORS default configuration."""
        settings = Settings()

        assert settings.cors_origins == ["*"]
        assert settings.cors_allow_credentials is True

    def test_blast_defaults(self):
        """Test BLAST default configuration."""
        settings = Settings()

        assert settings.blast_base_url == "https://blast.ncbi.nlm.nih.gov/Blast.cgi"
        assert settings.blast_email == ""

    def test_subscribed_categories(self):
        """Test default subscribed categories."""
        settings = Settings()

        assert "traces" in settings.subscribed_categories
        assert "alignments" in settings.subscribed_categories
