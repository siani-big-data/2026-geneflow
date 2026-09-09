"""Tests for heuristic functional strategies."""

import pytest

from src.ml.strategies.base import StrategyType
from src.ml.strategies.heuristic.functional import (
    HeuristicMutationImpactStrategy,
    HeuristicRNAStructureStrategy,
)


class TestHeuristicMutationImpactStrategy:
    """Tests for mutation impact prediction strategy."""

    @pytest.fixture
    def strategy(self):
        return HeuristicMutationImpactStrategy()

    @pytest.mark.asyncio
    async def test_synonymous_mutation(self, strategy):
        # TTT and TTC both code for Phe (F)
        result = await strategy.execute(
            reference_codon="TTT",
            alternate_codon="TTC",
            position=100,
        )

        assert result.strategy_used == StrategyType.HEURISTIC
        assert result.data["mutationType"] == "synonymous"
        assert result.data["referenceAA"] == "F"
        assert result.data["alternateAA"] == "F"

    @pytest.mark.asyncio
    async def test_missense_mutation(self, strategy):
        # ATG (Met) -> GTG (Val)
        result = await strategy.execute(
            reference_codon="ATG",
            alternate_codon="GTG",
            position=1,
        )

        assert result.data["mutationType"] == "missense"
        assert result.data["referenceAA"] == "M"
        assert result.data["alternateAA"] == "V"

    @pytest.mark.asyncio
    async def test_nonsense_mutation(self, strategy):
        # TGG (Trp) -> TGA (Stop)
        result = await strategy.execute(
            reference_codon="TGG",
            alternate_codon="TGA",
            position=50,
        )

        assert result.data["mutationType"] == "nonsense"
        assert result.data["alternateAA"] == "*"
        assert result.data["severity"] > 0.9

    @pytest.mark.asyncio
    async def test_result_structure(self, strategy):
        result = await strategy.execute(
            reference_codon="GCT",  # Ala
            alternate_codon="GTT",  # Val
            position=1,
        )

        assert "mutationType" in result.data
        assert "severity" in result.data
        assert "referenceAA" in result.data
        assert "alternateAA" in result.data
        assert "referenceCodon" in result.data
        assert "alternateCodon" in result.data
        assert result.model_name == "heuristic_mutation_impact"


class TestHeuristicRNAStructureStrategy:
    """Tests for RNA structure prediction strategy."""

    @pytest.fixture
    def strategy(self):
        return HeuristicRNAStructureStrategy()

    @pytest.mark.asyncio
    async def test_predict_structure(self, strategy):
        sequence = "GCGCAAAAGCGC"
        result = await strategy.execute(sequence=sequence)

        assert result.strategy_used == StrategyType.HEURISTIC
        assert "structure" in result.data
        assert "freeEnergy" in result.data

    @pytest.mark.asyncio
    async def test_structuREDACTED(self, strategy):
        sequence = "GGGGAAAACCCC"
        result = await strategy.execute(sequence=sequence)

        structure = result.data["structure"]
        # Dot-bracket notation
        assert all(c in ".()[]" for c in structure)
        assert len(structure) == len(sequence)

    @pytest.mark.asyncio
    async def test_base_pairs(self, strategy):
        sequence = "GCGCGCGCGCGC"
        result = await strategy.execute(sequence=sequence)

        assert "basePairs" in result.data
        assert result.data["basePairs"] >= 0

    @pytest.mark.asyncio
    async def test_short_sequence_error(self, strategy):
        sequence = "AAAA"  # Too short
        result = await strategy.execute(sequence=sequence)

        assert "error" in result.data
        assert result.confidence == 0.0

    @pytest.mark.asyncio
    async def test_t_converted_to_u(self, strategy):
        # T should be converted to U for RNA
        sequence = "GCGCTTTTTGCGC"
        result = await strategy.execute(sequence=sequence)

        assert result.data["sequence"].count("T") == 0
        assert "U" in result.data["sequence"]
