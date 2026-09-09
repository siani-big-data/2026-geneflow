"""Base mounter class and interfaces."""

from abc import ABC, abstractmethod
from enum import Enum


class MounterMode(Enum):
    """Modes for running mounters."""

    REPLAY = "replay"
    LIVE = "live"
    REBUILD = "rebuild"


class BaseMounter(ABC):
    """Base class for all mounters."""

    def __init__(self, name: str, categories: list[str]):
        self._name = name
        self._categories = categories
        self._running = False
        self._metrics = {
            "events_processed": 0,
            "events_failed": 0,
        }

    @property
    def name(self) -> str:
        """Get the mounter name."""
        return self._name

    @property
    def categories(self) -> list[str]:
        """Get the categories this mounter handles."""
        return self._categories

    @property
    def running(self) -> bool:
        """Check if the mounter is running."""
        return self._running

    def handles_category(self, category: str) -> bool:
        """Check if this mounter handles a specific category."""
        return category in self._categories

    def get_metrics(self) -> dict:
        """Get mounter metrics."""
        return {
            "name": self._name,
            "running": self._running,
            **self._metrics,
        }

    @abstractmethod
    async def start(self) -> None:
        """Start the mounter."""
        pass

    @abstractmethod
    async def stop(self) -> None:
        """Stop the mounter."""
        pass

    @abstractmethod
    async def handle_event(self, event: dict) -> None:
        """Handle an incoming event."""
        pass

    @abstractmethod
    async def health_check(self) -> bool:
        """Check if the mounter is healthy."""
        pass

    @abstractmethod
    async def rebuild(self) -> None:
        """Rebuild the mounter's state from scratch."""
        pass
