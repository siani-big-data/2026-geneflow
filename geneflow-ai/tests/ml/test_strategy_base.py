"""Tests for strategy base classes and registry."""

import pytest

from src.ml.strategies.base import ModelStrategy, StrategyResult, StrategyType


class TestStrategyType:
    """Tests for StrategyType enum."""

    def test_enum_values(self):
        assert StrategyType.HEURISTIC.value == "heuristic"
        assert StrategyType.MARKET.value == "market"
        assert StrategyType.CUSTOM.value == "custom"

    def test_string_enum(self):
        assert str(StrategyType.HEURISTIC) == "StrategyType.HEURISTIC"
        assert StrategyType.HEURISTIC == "heuristic"


class TestStrategyResult:
    """Tests for StrategyResult dataclass."""

    def test_create_result(self):
        result = StrategyResult(
            data={"key": "value"},
            confidence=0.95,
            strategy_used=StrategyType.HEURISTIC,
            model_name="test_model",
            model_version="1.0.0",
        )

        assert result.data == {"key": "value"}
        assert result.confidence == 0.95
        assert result.strategy_used == StrategyType.HEURISTIC
        assert result.model_name == "test_model"
        assert result.model_version == "1.0.0"
        assert result.metadata is None

    def test_result_with_metadata(self):
        result = StrategyResult(
            data={},
            confidence=0.5,
            strategy_used=StrategyType.MARKET,
            model_name="test",
            model_version="1.0",
            metadata={"extra": "info"},
        )

        assert result.metadata == {"extra": "info"}


class TestModelStrategy:
    """Tests for ModelStrategy abstract base class."""

    def test_cannot_instantiate(self):
        with pytest.raises(TypeError):
            ModelStrategy()

    def test_default_is_available(self):
        class ConcreteStrategy(ModelStrategy):
            strategy_type = StrategyType.HEURISTIC
            model_name = "concrete"
            model_version = "1.0.0"

            async def execute(self, **kwargs):
                return StrategyResult(
                    data={},
                    confidence=1.0,
                    strategy_used=self.strategy_type,
                    model_name=self.model_name,
                    model_version=self.model_version,
                )

        strategy = ConcreteStrategy()
        assert strategy.is_available is True

    def test_repr(self):
        class ConcreteStrategy(ModelStrategy):
            strategy_type = StrategyType.HEURISTIC
            model_name = "test"
            model_version = "1.0"

            async def execute(self, **kwargs):
                pass

        strategy = ConcreteStrategy()
        assert "ConcreteStrategy" in repr(strategy)
        assert "heuristic" in repr(strategy)
