"""Base handler for PostgreSQL event processing."""

from abc import ABC


class BaseHandler(ABC):
    """Base class for PostgreSQL event handlers."""

    def __init__(self, connection):
        self._connection = connection
        self._event_mappings: dict[str, str] = {}

    async def handle(self, event_type: str, payload: dict) -> None:
        """Handle an event based on its type."""
        method_name = self._event_mappings.get(event_type)
        if method_name:
            method = getattr(self, method_name, None)
            if method:
                await method(payload)
