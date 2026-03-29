"""Tests for market strategies (stubs that check availability)."""

import pytest

from src.ml.strategies.base import StrategyType
from src.ml.strategies.market.quality import PhredQualityStrategy, TracyQualityStrategy
from src.ml.strategies.market.variants import DeepVariantStrategy, GATKVariantStrategy


class TestTracyQualityStrategy:
    """Tests for Tracy wrapper strategy."""

    @pytest.fixture
    def strategy(self):
        return TracyQualityStrategy()

    def test_strategy_type(self, strategy):
        assert strategy.strategy_type == StrategyType.MARKET
        assert strategy.model_name == "tracy"

    def test_is_available_property(self, strategy):
        # Should return bool based on binary detection
        assert isinstance(strategy.is_available, bool)

    @pytest.mark.asyncio
    async def test_execute_not_available(self, strategy):
        # When tracy not installed, should return error gracefully
        if not strategy.is_available:
            result = await strategy.execute(trace_file="test.ab1")
            assert result.confidence == 0.0
            assert "error" in result.data or "status" in result.data


class TestPhredQualityStrategy:
    """Tests for Phred wrapper strategy."""

    @pytest.fixture
    def strategy(self):
        return PhredQualityStrategy()

    def test_strategy_type(self, strategy):
        assert strategy.strategy_type == StrategyType.MARKET
        assert strategy.model_name == "phred"

    def test_is_available_property(self, strategy):
        assert isinstance(strategy.is_available, bool)

    @pytest.mark.asyncio
    async def test_execute_not_available(self, strategy):
        if not strategy.is_available:
            result = await strategy.execute()
            assert result.confidence == 0.0


class TestDeepVariantStrategy:
    """Tests for DeepVariant wrapper strategy."""

    @pytest.fixture
    def strategy(self):
        return DeepVariantStrategy()

    def test_strategy_type(self, strategy):
        assert strategy.strategy_type == StrategyType.MARKET
        assert strategy.model_name == "deepvariant"

    def test_checks_docker(self, strategy):
        # is_available should check for docker
        available = strategy.is_available
        assert isinstance(available, bool)

    @pytest.mark.asyncio
    async def test_execute_not_available(self, strategy):
        if not strategy.is_available:
            result = await strategy.execute(bam_file="test.bam")
            assert "error" in result.data or "status" in result.data


class TestGATKVariantStrategy:
    """Tests for GATK wrapper strategy."""

    @pytest.fixture
    def strategy(self):
        return GATKVariantStrategy()

    def test_strategy_type(self, strategy):
        assert strategy.strategy_type == StrategyType.MARKET
        assert strategy.model_name == "gatk_haplotypecaller"

    @pytest.mark.asyncio
    async def test_execute_not_available(self, strategy):
        if not strategy.is_available:
            result = await strategy.execute()
            assert result.confidence == 0.0
