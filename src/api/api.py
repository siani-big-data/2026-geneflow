"""Datalake API - Backward compatible wrapper."""

from src.api.app import create_app
from src.config import Settings
from src.retry import RetryHandler
from src.storage import StorageProvider


class DatalakeAPI:
    """REST API for the Datalake.

    This class provides backward compatibility with the original API.
    Internally delegates to the app factory.
    """

    def __init__(
        self,
        storage: StorageProvider,
        settings: Settings,
        retry_handler: RetryHandler,
    ):
        self.storage = storage
        self.settings = settings
        self.retry_handler = retry_handler
        self._consumer_metrics_callback = None

        self.app = create_app(
            storage=storage,
            settings=settings,
            retry_handler=retry_handler,
            get_consumer_metrics=lambda: self._get_consumer_metrics(),
        )

    def _get_consumer_metrics(self) -> dict | None:
        if self._consumer_metrics_callback:
            return self._consumer_metrics_callback()
        return None

    def set_consumer_metrics_callback(self, callback) -> None:
        """Connect consumer metrics callback."""
        self._consumer_metrics_callback = callback
