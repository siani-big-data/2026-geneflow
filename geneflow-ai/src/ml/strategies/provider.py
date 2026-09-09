"""Model provider that manages strategy selection.

The provider allows:
1. Registering multiple strategies per model type
2. Selecting strategy via configuration
3. Fallback to heuristic if preferred strategy unavailable
"""

from typing import Optional

import structlog

from .base import ModelStrategy, StrategyType

logger = structlog.get_logger()

# Global provider instance
_provider: Optional["ModelProvider"] = None


class ModelProvider:
    """Manages model strategies and selection.

    Usage:
        provider = ModelProvider()
        provider.register("artifact_detector", HeuristicArtifactStrategy())
        provider.register("mutation_impact", SIFTStrategy())

        # Get strategy (respects preference order)
        strategy = provider.get("artifact_detector")
        result = await strategy.execute(signal_a=[...], signal_t=[...], ...)
    """

    def __init__(self, preferred_type: StrategyType = StrategyType.HEURISTIC):
        self._strategies: dict[str, dict[StrategyType, ModelStrategy]] = {}
        self._preferred_type = preferred_type

    @property
    def preferred_type(self) -> StrategyType:
        """Get the preferred strategy type."""
        return self._preferred_type

    @preferred_type.setter
    def preferred_type(self, value: StrategyType) -> None:
        """Set the preferred strategy type."""
        self._preferred_type = value
        logger.info("strategy_preference_changed", preferred=value.value)

    def register(self, model_key: str, strategy: ModelStrategy) -> None:
        """Register a strategy for a model type.

        Args:
            model_key: Unique identifier for the model (e.g., "quality_predictor")
            strategy: The strategy implementation
        """
        if model_key not in self._strategies:
            self._strategies[model_key] = {}

        self._strategies[model_key][strategy.strategy_type] = strategy
        logger.debug(
            "strategy_registered",
            model=model_key,
            strategy=strategy.strategy_type.value,
            name=strategy.model_name,
        )

    def get(
        self,
        model_key: str,
        strategy_type: Optional[StrategyType] = None,
    ) -> Optional[ModelStrategy]:
        """Get a strategy for a model.

        Args:
            model_key: The model identifier
            strategy_type: Specific strategy type (or use preferred)

        Returns:
            The strategy, or None if not found
        """
        if model_key not in self._strategies:
            logger.warning("model_not_found", model=model_key)
            return None

        strategies = self._strategies[model_key]
        target_type = strategy_type or self._preferred_type

        # Try preferred type first
        if target_type in strategies:
            strategy = strategies[target_type]
            if strategy.is_available:
                return strategy
            logger.warning(
                "strategy_not_available",
                model=model_key,
                strategy=target_type.value,
            )

        # Fallback order: CUSTOM -> MARKET -> HEURISTIC
        fallback_order = [StrategyType.CUSTOM, StrategyType.MARKET, StrategyType.HEURISTIC]
        for fallback_type in fallback_order:
            if fallback_type in strategies and strategies[fallback_type].is_available:
                logger.info(
                    "strategy_fallback",
                    model=model_key,
                    requested=target_type.value,
                    using=fallback_type.value,
                )
                return strategies[fallback_type]

        return None

    def list_models(self) -> list[str]:
        """List all registered model keys."""
        return list(self._strategies.keys())

    def list_strategies(self, model_key: str) -> list[dict]:
        """List all strategies for a model."""
        if model_key not in self._strategies:
            return []

        return [
            {
                "type": strategy.strategy_type.value,
                "name": strategy.model_name,
                "version": strategy.model_version,
                "available": strategy.is_available,
            }
            for strategy in self._strategies[model_key].values()
        ]

    def get_status(self) -> dict:
        """Get provider status."""
        return {
            "preferredType": self._preferred_type.value,
            "registeredModels": len(self._strategies),
            "models": {
                key: [s.strategy_type.value for s in strategies.values()]
                for key, strategies in self._strategies.items()
            },
        }


def get_provider() -> ModelProvider:
    """Get the global model provider instance."""
    global _provider
    if _provider is None:
        _provider = ModelProvider()
    return _provider


def set_provider(provider: ModelProvider) -> None:
    """Set the global model provider instance."""
    global _provider
    _provider = provider
