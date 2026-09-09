"""Exponential backoff calculation."""

from datetime import datetime, timedelta

from src.constants import RETRY_BASE_DELAY_SECONDS, RETRY_MAX_DELAY_SECONDS


class ExponentialBackoff:
    """Calculates retry delays with exponential backoff."""

    def __init__(
        self,
        base_delay: float = RETRY_BASE_DELAY_SECONDS,
        max_delay: float = RETRY_MAX_DELAY_SECONDS,
    ):
        self.base_delay = base_delay
        self.max_delay = max_delay

    def calculate_delay(self, retry_count: int) -> float:
        """Calculate delay in seconds for given retry count."""
        return min(self.base_delay * (2**retry_count), self.max_delay)

    def next_retry_at(self, retry_count: int) -> datetime:
        """Calculate next retry datetime."""
        delay = self.calculate_delay(retry_count)
        return datetime.utcnow() + timedelta(seconds=delay)
