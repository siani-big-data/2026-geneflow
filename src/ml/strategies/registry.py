"""Strategy registry - initializes and registers all available strategies.

Usage:
    from src.ml.strategies import get_provider, StrategyType

    # Initialize with all strategies
    init_strategies()

    # Get provider and use strategies
    provider = get_provider()
    provider.preferred_type = StrategyType.MARKET  # Try market models first

    strategy = provider.get("quality_predictor")
    result = await strategy.execute(quality_scores=[30, 35, 40])
"""

import structlog

from .base import StrategyType

# Import heuristic strategies
from .heuristic import (
    HeuristicArtifactStrategy,
    HeuristicClusterStrategy,
    HeuristicDiversityStrategy,
    HeuristicGeneFinderStrategy,
    HeuristicHeterozygoteStrategy,
    HeuristicMotifStrategy,
    HeuristicMutationImpactStrategy,
    HeuristicQualityStrategy,
    HeuristicRNAStructureStrategy,
    HeuristicSNPStrategy,
    HeuristicTrimStrategy,
)

# Import market strategies
from .market import (
    DeepVariantStrategy,
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

    # Register Quality strategies
    provider.register("quality_predictor", HeuristicQualityStrategy())
    provider.register("quality_predictor", TracyQualityStrategy())

    provider.register("auto_trimmer", HeuristicTrimStrategy())

    provider.register("artifact_detector", HeuristicArtifactStrategy())

    # Register Variant strategies
    provider.register("snp_caller", HeuristicSNPStrategy())
    provider.register("snp_caller", DeepVariantStrategy())

    provider.register("heterozygote_detector", HeuristicHeterozygoteStrategy())

    # Register Annotation strategies
    provider.register("gene_finder", HeuristicGeneFinderStrategy())

    provider.register("motif_scanner", HeuristicMotifStrategy())

    # Register Phylo strategies
    provider.register("sequence_clusterer", HeuristicClusterStrategy())

    provider.register("diversity_calculator", HeuristicDiversityStrategy())

    # Register Functional strategies
    provider.register("mutation_impact", HeuristicMutationImpactStrategy())
    provider.register("mutation_impact", SIFTStrategy())

    provider.register("rna_structure", HeuristicRNAStructureStrategy())
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

    QUALITY_PREDICTOR = "quality_predictor"
    AUTO_TRIMMER = "auto_trimmer"
    ARTIFACT_DETECTOR = "artifact_detector"
    SNP_CALLER = "snp_caller"
    HETEROZYGOTE_DETECTOR = "heterozygote_detector"
    GENE_FINDER = "gene_finder"
    MOTIF_SCANNER = "motif_scanner"
    SEQUENCE_CLUSTERER = "sequence_clusterer"
    DIVERSITY_CALCULATOR = "diversity_calculator"
    MUTATION_IMPACT = "mutation_impact"
    RNA_STRUCTURE = "rna_structure"
