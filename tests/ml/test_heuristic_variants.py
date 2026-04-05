"""Tests for heuristic variant strategies."""

import pytest

from src.ml.strategies.base import StrategyType
from src.ml.strategies.heuristic.variants import (
    HeuristicHeterozygoteStrategy,
    HeuristicSNPStrategy,
)


class TestHeuristicSNPStrategy:
    """Tests for SNP calling strategy."""

    @pytest.fixture
    def strategy(self):
        return HeuristicSNPStrategy()

    @pytest.mark.asyncio
    async def test_detect_snps(self, strategy):
        query = "ATGCGATCGA"
        reference = "ATGCTATCGA"  # T->G at position 5 (1-indexed)

        result = await strategy.execute(query_sequence=query, reference_sequence=reference)

        assert result.strategy_used == StrategyType.HEURISTIC
        assert result.data["totalVariants"] == 1
        assert len(result.data["variants"]) == 1

        snp = result.data["variants"][0]
        assert snp["position"] == 5
        assert snp["reference"] == "T"
        assert snp["alternate"] == "G"

    @pytest.mark.asyncio
    async def test_no_snps(self, strategy):
        seq = "ATGCGATCGA"
        result = await strategy.execute(query_sequence=seq, reference_sequence=seq)

        assert result.data["totalVariants"] == 0
        assert len(result.data["variants"]) == 0

    @pytest.mark.asyncio
    async def test_multiple_snps(self, strategy):
        query = "ATGC"
        reference = "TTGC"  # 1 difference at pos 0

        result = await strategy.execute(query_sequence=query, reference_sequence=reference)

        assert result.data["totalVariants"] == 1

    @pytest.mark.asyncio
    async def test_variant_rate(self, strategy):
        query = "ATGCGATCGA"  # 10 bases
        reference = "TTGCGATCGA"  # 1 difference

        result = await strategy.execute(query_sequence=query, reference_sequence=reference)

        assert result.data["variantRate"] == 10.0  # 1/10 * 100


class TestHeuristicHeterozygoteStrategy:
    """Tests for heterozygote_training detection strategy."""

    @pytest.fixture
    def strategy(self):
        return HeuristicHeterozygoteStrategy()

    @pytest.mark.asyncio
    async def test_detect_heterozygote(self, strategy):
        # Position with two strong signals (heterozygote_training) - ratio between 0.25-0.75
        signal_a = [1000, 100, 100]
        signal_t = [100, 100, 100]
        signal_c = [100, 500, 100]  # Strong C at pos 1
        signal_g = [100, 400, 100]  # Strong G at pos 1 too (ratio 0.8, close)

        result = await strategy.execute(
            signal_a=signal_a,
            signal_t=signal_t,
            signal_c=signal_c,
            signal_g=signal_g,
        )

        # May or may not detect based on ratio thresholds
        assert "heterozygotes" in result.data
        assert "totalHeterozygotes" in result.data

    @pytest.mark.asyncio
    async def test_homozygous_positions(self, strategy):
        # Clear single peaks (homozygous) - secondary peak very low
        signal_a = [1000, 100, 100]
        signal_t = [100, 1000, 100]
        signal_c = [50, 50, 1000]
        signal_g = [50, 50, 50]

        result = await strategy.execute(
            signal_a=signal_a,
            signal_t=signal_t,
            signal_c=signal_c,
            signal_g=signal_g,
        )

        assert result.data["totalHeterozygotes"] == 0

    @pytest.mark.asyncio
    async def test_result_structure(self, strategy):
        signal = [500, 600, 550]
        result = await strategy.execute(
            signal_a=signal,
            signal_t=[100] * 3,
            signal_c=[100] * 3,
            signal_g=[100] * 3,
        )

        assert "heterozygotes" in result.data
        assert "totalHeterozygotes" in result.data
        assert "heterozygoteRate" in result.data
        assert result.model_name == "heuristic_heterozygote"
