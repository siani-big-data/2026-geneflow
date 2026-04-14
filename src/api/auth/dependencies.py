from typing import Callable

from src.api.auth.api_key import ApiKeyAuthenticator
from src.config import Settings


def get_api_key_dependency(settings: Settings) -> Callable:
    """Create FastAPI dependency for API key verification."""
    authenticator = ApiKeyAuthenticator(settings.api_key)
    return authenticator.verify
