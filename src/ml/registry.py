"""
Central model registry for GeneFlow AI.

Registra y gestiona los modelos ML locales que aportan valor
sobre los análisis algorítmicos de geneflow-analysis.

Modelos incluidos:
- ArtifactDetector: Detección de artefactos en cromatogramas (ML pattern recognition)
- MotifScanner: Motivos biológicos predefinidos (TATA, Kozak, splice sites)
- MutationImpactPredictor: Predicción de impacto funcional de variantes
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
    # Quality models - ML-based artifact detection
    from .quality import ArtifactDetector

    registry.register(ArtifactDetector())

    # Annotation models - biological motif scanning
    from .annotation import MotifScanner

    registry.register(MotifScanner())

    # Functional models - mutation impact prediction
    from .functional import MutationImpactPredictor

    registry.register(MutationImpactPredictor())

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
