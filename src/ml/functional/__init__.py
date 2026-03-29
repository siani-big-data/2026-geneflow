"""Functional prediction models."""

from .mutation_impact import MutationImpactPredictor
from .rna_structure import RNAStructurePredictor

__all__ = ["MutationImpactPredictor", "RNAStructurePredictor"]
