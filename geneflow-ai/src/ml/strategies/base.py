"""Base strategy interface for ML models.

Strategy Pattern: Allows swapping model implementations at runtime.
- Heuristic: Fast, rule-based (current implementation)
- Market: Pre-trained models from the market (SIFT, ViennaRNA, etc.)
- Custom: Future trained models that replace market ones
"""

from abc import ABC, abstractmethod
from dataclasses import dataclass
from enum import Enum
from typing import Any


class StrategyType(str, Enum):
    """Available strategy types."""

    HEURISTIC = "heuristic"  # Rule-based, fast, for testing
    MARKET = "market"  # Pre-trained models from market
    CUSTOM = "custom"  # Future custom-trained models


@dataclass
class StrategyResult:
    """Result from a strategy execution."""

    data: Any
    confidence: float
    strategy_used: StrategyType
    model_name: str
    model_version: str
    metadata: dict | None = None


class ModelStrategy(ABC):
    """Abstract base class for model strategies.

    Each strategy implements the same interface but with different
    underlying implementations (heuristic, market model, custom model).
    """

    @property
    @abstractmethod
    def strategy_type(self) -> StrategyType:
        """Return the type of this strategy."""
        pass

    @property
    @abstractmethod
    def model_name(self) -> str:
        """Return the name of the underlying model."""
        pass

    @property
    @abstractmethod
    def model_version(self) -> str:
        """Return the version of the underlying model."""
        pass

    @property
    def is_available(self) -> bool:
        """Check if this strategy is available (dependencies installed)."""
        return True

    @abstractmethod
    async def execute(self, **kwargs) -> StrategyResult:
        """Execute the strategy with given inputs."""
        pass

    def __repr__(self) -> str:
        return f"{self.__class__.__name__}({self.strategy_type.value})"
