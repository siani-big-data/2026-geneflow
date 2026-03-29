"""Tests for heuristic phylogenetic strategies."""

import pytest

from src.ml.strategies.base import StrategyType
from src.ml.strategies.heuristic.phylo import (
    HeuristicClusterStrategy,
    HeuristicDiversityStrategy,
)


class TestHeuristicClusterStrategy:
    """Tests for sequence clustering strategy."""

    @pytest.fixture
    def strategy(self):
        return HeuristicClusterStrategy()

    @pytest.mark.asyncio
    async def test_cluster_similar_sequences(self, strategy):
        sequences = [
            "ATGCGATCGATCG",
            "ATGCGATCGATCG",  # Identical
            "ATGCGATCGATCG",  # Identical
            "TTTTTTTTTTTT",  # Different
        ]
        result = await strategy.execute(sequences=sequences, threshold=0.9)

        assert result.strategy_used == StrategyType.HEURISTIC
        assert result.data["totalClusters"] == 2

    @pytest.mark.asyncio
    async def test_single_cluster(self, strategy):
        sequences = [
            "ATGCGATCGATCG",
            "ATGCGATCGATCG",
            "ATGCGATCGATCG",
        ]
        result = await strategy.execute(sequences=sequences, threshold=0.9)

        assert result.data["totalClusters"] == 1
        assert result.data["clusterSizes"][0] == 3

    @pytest.mark.asyncio
    async def test_all_different(self, strategy):
        sequences = [
            "AAAAAAAAAA",
            "TTTTTTTTTT",
            "CCCCCCCCCC",
            "GGGGGGGGGG",
        ]
        result = await strategy.execute(sequences=sequences, threshold=0.9)

        assert result.data["totalClusters"] == 4

    @pytest.mark.asyncio
    async def test_empty_sequences(self, strategy):
        result = await strategy.execute(sequences=[])

        assert result.data["totalClusters"] == 0
        assert result.confidence == 0.0

    @pytest.mark.asyncio
    async def test_kmer_parameter(self, strategy):
        sequences = ["ATGCGATCGATCGATCG", "ATGCGATCGATCGATCG"]
        result = await strategy.execute(sequences=sequences, k=4)

        assert result.data["totalClusters"] == 1


class TestHeuristicDiversityStrategy:
    """Tests for genetic diversity strategy."""

    @pytest.fixture
    def strategy(self):
        return HeuristicDiversityStrategy()

    @pytest.mark.asyncio
    async def test_diversity_metrics(self, strategy):
        sequences = [
            "ATGCGATCGA",
            "ATGCTATCGA",  # 1 difference
            "ATGCAATCGA",  # 1 difference
        ]
        result = await strategy.execute(sequences=sequences)

        assert result.strategy_used == StrategyType.HEURISTIC
        assert "nucleotideDiversity" in result.data
        assert "thetaWatterson" in result.data
        assert "tajimasD" in result.data
        assert "haplotypeDiversity" in result.data
        assert "segregatingSites" in result.data

    @pytest.mark.asyncio
    async def test_identical_sequences(self, strategy):
        sequences = ["ATGCGATCGA", "ATGCGATCGA", "ATGCGATCGA"]
        result = await strategy.execute(sequences=sequences)

        assert result.data["nucleotideDiversity"] == 0
        assert result.data["segregatingSites"] == 0

    @pytest.mark.asyncio
    async def test_single_sequence_error(self, strategy):
        result = await strategy.execute(sequences=["ATGC"])

        assert "error" in result.data
        assert result.confidence == 0.0

    @pytest.mark.asyncio
    async def test_haplotype_count(self, strategy):
        sequences = [
            "ATGC",
            "ATGC",
            "TTGC",
            "TTGC",
        ]
        result = await strategy.execute(sequences=sequences)

        assert result.data["numHaplotypes"] == 2
        assert result.data["numSequences"] == 4

    @pytest.mark.asyncio
    async def test_segregating_sites(self, strategy):
        sequences = [
            "ATGC",
            "TTGC",  # A->T at pos 0
            "ATAC",  # G->A at pos 2
        ]
        result = await strategy.execute(sequences=sequences)

        assert result.data["segregatingSites"] == 2
