"""Strategy registry - initializes and registers all available strategies.

This module only registers strategies that provide value beyond geneflow-analysis:
- ArtifactDetector: Pattern recognition in chromatograms (ML candidate)
- MotifScanner: Biological regulatory motifs
- MutationImpact: Functional impact prediction (ML candidate)

For other analyses (ORF detection, trimming, SNP calling, diversity metrics),
use geneflow-analysis which has more complete algorithmic implementations.

Usage:
    from src.ml.strategies import get_provider, StrategyType

    # Initialize with all strategies
    init_strategies()

    # Get provider and use strategies
    provider = get_provider()
    strategy = provider.get("artifact_detector")
    result = await strategy.execute(signal_a=[...], signal_t=[...], ...)
"""

import structlog

from .base import StrategyType

# Import heuristic strategies (only those that add value)
from .heuristic import (
    HeuristicArtifactStrategy,
    HeuristicMotifStrategy,
    HeuristicMutationImpactStrategy,
)

# Import market strategies (external tools)
from .market import (
    SIFTStrategy,
    TracyQualityStrategy,
    ViennaRNAStrategy,
)
from .provider import ModelProvider, get_provider

logger = structlog.get_logger()


def init_strategies(
    provider: ModelProvider | None = None,
    preferred_type: StrategyType = StrategyType.HEURISTIC,
) -> ModelProvider:
    """Initialize all strategies and register them with the provider.

    Args:
        provider: Optional provider instance (uses global if not provided)
        preferred_type: Default strategy type preference

    Returns:
        Configured ModelProvider
    """
    if provider is None:
        provider = get_provider()

    provider.preferred_type = preferred_type

    # Quality strategies - only artifact detection (ML candidate)
    provider.register("artifact_detector", HeuristicArtifactStrategy())
    provider.register("quality_predictor", TracyQualityStrategy())  # External tool

    # Annotation strategies - biological motifs
    provider.register("motif_scanner", HeuristicMotifStrategy())

    # Functional strategies - mutation impact (ML candidate)
    provider.register("mutation_impact", HeuristicMutationImpactStrategy())
    provider.register("mutation_impact", SIFTStrategy())  # External tool

    # RNA structure - external tool only (ViennaRNA is superior to Nussinov)
    provider.register("rna_structure", ViennaRNAStrategy())

    logger.info(
        "strategies_initialized",
        total_models=len(provider.list_models()),
        preferred_type=preferred_type.value,
    )

    return provider


# Model key constants for easy reference
class ModelKeys:
    """Constants for model keys used in the registry."""

    ARTIFACT_DETECTOR = "artifact_detector"
    QUALITY_PREDICTOR = "quality_predictor"
    MOTIF_SCANNER = "motif_scanner"
    MUTATION_IMPACT = "mutation_impact"
    RNA_STRUCTURE = "rna_structure"
