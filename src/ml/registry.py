"""
Central model registry for GeneFlow AI.

Registra y gestiona todos los modelos ML locales.
"""

from pathlib import Path
from typing import Optional

import structlog

from .base import ModelRegistry

logger = structlog.get_logger()

# Singleton registry
_registry: Optional[ModelRegistry] = None


def get_registry() -> ModelRegistry:
    """Get the global model registry."""
    global _registry
    if _registry is None:
        _registry = ModelRegistry()
        _register_all_models(_registry)
    return _registry


def _register_all_models(registry: ModelRegistry) -> None:
    """Register all available models."""
    # Quality models
    from .quality import ArtifactDetector, AutoTrimmer, QualityPredictor

    registry.register(AutoTrimmer())
    registry.register(ArtifactDetector())
    registry.register(QualityPredictor())

    # Variant models
    from .variants import HeterozygoteDetector, SNPCaller

    registry.register(SNPCaller())
    registry.register(HeterozygoteDetector())

    # Annotation models
    from .annotation import GeneFinder, MotifScanner

    registry.register(GeneFinder())
    registry.register(MotifScanner())

    # Phylo models
    from .phylo import DiversityCalculator, SequenceClusterer

    registry.register(SequenceClusterer())
    registry.register(DiversityCalculator())

    # Functional models
    from .functional import MutationImpactPredictor, RNAStructurePredictor

    registry.register(MutationImpactPredictor())
    registry.register(RNAStructurePredictor())

    logger.info(
        "models_registered",
        total=registry.total_count,
    )


def load_all_models(models_dir: Optional[Path] = None) -> dict[str, bool]:
    """
    Load all registered models.

    Args:
        models_dir: Directory containing model files

    Returns:
        Dict of model_name -> load_success
    """
    registry = get_registry()
    results = registry.load_all(models_dir)

    logger.info(
        "models_loaded",
        loaded=registry.loaded_count,
        total=registry.total_count,
    )

    return results
