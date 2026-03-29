"""Tests for heuristic quality strategies."""

import pytest

from src.ml.strategies.base import StrategyType
from src.ml.strategies.heuristic.quality import (
    HeuristicArtifactStrategy,
    HeuristicQualityStrategy,
    HeuristicTrimStrategy,
)


class TestHeuristicQualityStrategy:
    """Tests for quality prediction strategy."""

    @pytest.fixture
    def strategy(self):
        return HeuristicQualityStrategy()

    @pytest.mark.asyncio
    async def test_execute_with_quality_scores(self, strategy):
        scores = [30, 35, 40, 38, 42, 45, 40, 38, 35, 30]
        result = await strategy.execute(quality_scores=scores)

        assert result.strategy_used == StrategyType.HEURISTIC
        assert result.model_name == "heuristic_quality"
        assert result.confidence > 0
        assert "q20Percentage" in result.data
        assert "q30Percentage" in result.data
        assert "meanQuality" in result.data
        assert result.data["q20Percentage"] == 100.0  # All >= 20

    @pytest.mark.asyncio
    async def test_execute_without_scores(self, strategy):
        result = await strategy.execute(sequence="ATGC")

        assert result.confidence == 0.5
        assert result.data["q20Percentage"] == 85.0
        assert result.data["meanQuality"] == 25.0

    @pytest.mark.asyncio
    async def test_q30_calculation(self, strategy):
        scores = [25, 25, 35, 35, 40, 40]  # 4/6 >= 30
        result = await strategy.execute(quality_scores=scores)

        expected_q30 = (4 / 6) * 100
        assert abs(result.data["q30Percentage"] - expected_q30) < 0.1


class TestHeuristicTrimStrategy:
    """Tests for auto-trimming strategy."""

    @pytest.fixture
    def strategy(self):
        return HeuristicTrimStrategy()

    @pytest.mark.asyncio
    async def test_trim_low_quality_ends(self, strategy):
        # Low quality at start and end
        scores = [5, 8, 10, 12, 35, 40, 42, 40, 38, 35, 10, 8, 5]
        result = await strategy.execute(quality_scores=scores)

        assert result.data["trimStart"] > 0
        assert result.data["trimEnd"] < len(scores)
        assert result.data["basesRemoved"] > 0

    @pytest.mark.asyncio
    async def test_no_trim_high_quality(self, strategy):
        scores = [40, 42, 45, 43, 40, 42, 45, 43, 40, 42]
        result = await strategy.execute(quality_scores=scores)

        assert result.data["trimStart"] == 0
        # Allow small variance in trimming algorithm
        assert result.data["trimmedLength"] >= len(scores) - 2

    @pytest.mark.asyncio
    async def test_trim_result_structure(self, strategy):
        scores = [10] * 5 + [40] * 20 + [10] * 5
        result = await strategy.execute(quality_scores=scores)

        assert "trimStart" in result.data
        assert "trimEnd" in result.data
        assert "trimmedLength" in result.data
        assert "basesRemoved" in result.data
        assert result.strategy_used == StrategyType.HEURISTIC


class TestHeuristicArtifactStrategy:
    """Tests for artifact detection strategy."""

    @pytest.fixture
    def strategy(self):
        return HeuristicArtifactStrategy()

    @pytest.mark.asyncio
    async def test_detect_spike(self, strategy):
        # Normal signals with one spike
        signal_a = [500, 600, 5000, 500, 600]  # Spike at position 2
        signal_t = [100, 100, 100, 100, 100]
        signal_c = [100, 100, 100, 100, 100]
        signal_g = [100, 100, 100, 100, 100]

        result = await strategy.execute(
            signal_a=signal_a,
            signal_t=signal_t,
            signal_c=signal_c,
            signal_g=signal_g,
        )

        assert result.data["totalArtifacts"] >= 1
        artifacts = result.data["artifacts"]
        spike = next((a for a in artifacts if a["type"] == "spike"), None)
        assert spike is not None
        assert spike["position"] == 2

    @pytest.mark.asyncio
    async def test_detect_low_signal(self, strategy):
        signal_a = [500, 20, 500]  # Low signal at position 1
        signal_t = [100, 10, 100]
        signal_c = [100, 15, 100]
        signal_g = [100, 5, 100]

        result = await strategy.execute(
            signal_a=signal_a,
            signal_t=signal_t,
            signal_c=signal_c,
            signal_g=signal_g,
        )

        low_signals = [a for a in result.data["artifacts"] if a["type"] == "low_signal"]
        assert len(low_signals) >= 1

    @pytest.mark.asyncio
    async def test_clean_signal(self, strategy):
        signal = [500, 600, 550, 580, 520]
        result = await strategy.execute(
            signal_a=signal,
            signal_t=[100] * 5,
            signal_c=[100] * 5,
            signal_g=[100] * 5,
        )

        assert result.data["totalArtifacts"] == 0
        assert result.data["artifactRate"] == 0
