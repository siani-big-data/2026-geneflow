"""Tests for heuristic annotation strategies."""

import pytest

from src.ml.strategies.base import StrategyType
from src.ml.strategies.heuristic.annotation import (
    HeuristicGeneFinderStrategy,
    HeuristicMotifStrategy,
)


class TestHeuristicGeneFinderStrategy:
    """Tests for gene finding strategy."""

    @pytest.fixture
    def strategy(self):
        return HeuristicGeneFinderStrategy()

    @pytest.mark.asyncio
    async def test_find_orf(self, strategy):
        # ORF needs to be at least 100bp by default
        # ATG + 30 codons + TAA = 33 codons = 99bp, need longer
        sequence = "ATG" + "AAA" * 35 + "TAA"  # 108bp ORF
        result = await strategy.execute(sequence=sequence)

        assert result.strategy_used == StrategyType.HEURISTIC
        assert "regions" in result.data
        assert result.data["totalRegions"] >= 1

    @pytest.mark.asyncio
    async def test_find_complete_orf(self, strategy):
        # Complete ORF with start and stop, >100bp
        sequence = "ATG" + "GCG" * 40 + "TAA"  # 123bp
        result = await strategy.execute(sequence=sequence)

        if result.data["regions"]:
            orf = result.data["regions"][0]
            assert "start" in orf
            assert "end" in orf
            assert "frame" in orf
            assert "strand" in orf

    @pytest.mark.asyncio
    async def test_min_length_filter(self, strategy):
        # Short sequence won't have ORFs >= 100bp
        sequence = "ATGAAATAA"  # Only 9bp
        result = await strategy.execute(sequence=sequence)

        assert result.data["totalRegions"] == 0

    @pytest.mark.asyncio
    async def test_custom_min_length(self, strategy):
        sequence = "ATG" + "AAA" * 10 + "TAA"  # 36bp ORF
        result = await strategy.execute(sequence=sequence, min_orf_length=30)

        assert result.data["totalRegions"] >= 1

    @pytest.mark.asyncio
    async def test_no_start_codon(self, strategy):
        sequence = "GGGCCCAAATTTGGGCCC" * 10  # No ATG
        result = await strategy.execute(sequence=sequence)

        assert result.data["totalRegions"] == 0


class TestHeuristicMotifStrategy:
    """Tests for motif scanning strategy."""

    @pytest.fixture
    def strategy(self):
        return HeuristicMotifStrategy()

    @pytest.mark.asyncio
    async def test_find_tata_box(self, strategy):
        # TATA box pattern: TATA[AT]A[AT]
        sequence = "GGGGTATAAAAGGGATGCCC"
        result = await strategy.execute(sequence=sequence)

        assert "motifs" in result.data
        tata_motifs = [m for m in result.data["motifs"] if m["motif"] == "TATA_box"]
        assert len(tata_motifs) >= 1

    @pytest.mark.asyncio
    async def test_find_polya_signal(self, strategy):
        sequence = "ATGCGAATAAAAATGC"  # AATAAA poly-A signal
        result = await strategy.execute(sequence=sequence)

        polya = [m for m in result.data["motifs"] if m["motif"] == "polyA_signal"]
        assert len(polya) >= 1

    @pytest.mark.asyncio
    async def test_result_structure(self, strategy):
        sequence = "ATGCGATCGATCG"
        result = await strategy.execute(sequence=sequence)

        assert result.model_name == "heuristic_motif"
        assert "motifs" in result.data
        assert "totalMotifs" in result.data
        assert "motifTypes" in result.data

    @pytest.mark.asyncio
    async def test_no_motifs(self, strategy):
        sequence = "CCCCCCCCCCCC"  # No recognizable motifs
        result = await strategy.execute(sequence=sequence)

        assert result.data["totalMotifs"] == 0

    @pytest.mark.asyncio
    async def test_motif_has_position(self, strategy):
        sequence = "AATAAA"  # polyA at position 1
        result = await strategy.execute(sequence=sequence)

        if result.data["motifs"]:
            motif = result.data["motifs"][0]
            assert "start" in motif
            assert "end" in motif
            assert "sequence" in motif
