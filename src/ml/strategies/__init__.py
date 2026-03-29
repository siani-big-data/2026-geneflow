"""Model strategies for swappable implementations.

Strategy Pattern for ML models:
- HEURISTIC: Fast rule-based implementations (always available)
- MARKET: Pre-trained models from market (Tracy, SIFT, ViennaRNA, etc.)
- CUSTOM: Future custom-trained models

Usage:
    from src.ml.strategies import init_strategies, get_provider, StrategyType, ModelKeys

    # Initialize all strategies
    init_strategies(preferred_type=StrategyType.MARKET)

    # Get and use a strategy
    provider = get_provider()
    strategy = provider.get(ModelKeys.QUALITY_PREDICTOR)
    result = await strategy.execute(quality_scores=[30, 35, 40])
"""

from .base import ModelStrategy, StrategyResult, StrategyType
from .provider import ModelProvider, get_provider, set_provider
from .registry import ModelKeys, init_strategies

__all__ = [
    # Core
    "ModelStrategy",
    "StrategyResult",
    "StrategyType",
    # Provider
    "ModelProvider",
    "get_provider",
    "set_provider",
    # Registry
    "init_strategies",
    "ModelKeys",
]
