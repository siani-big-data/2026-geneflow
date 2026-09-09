"""Custom ML models for GeneFlow.

Available models:
    - QualityPredictor: Predict Phred quality scores from signals
    - QualityClassifier: Classify quality into bins (Q10-Q50+)
    - TaxonomyClassifier: Hierarchical taxonomy classification
    - HeterozygoteClassifier: Detect heterozygous positions
    - TrimmingPredictor: Predict optimal trim points
"""

from .base import BaseModel, ModelConfig
from .heterozygote import (
    HeterozygoteClassifier,
    HeterozygoteConfig,
)
from .quality import (
    EnhancedQualityConfig,
    EnhancedQualityLoss,
    EnhancedQualityPredictor,
    QualityLoss,
    QualityPredictor,
    QualityPredictorConfig,
)
from .quality.classifier import (
    QualityClassifier,
    QualityClassifierCNN,
    QualityClassifierCNNConfig,
    QualityClassifierConfig,
)
from .taxonomy import (
    TAXONOMY_LEVELS,
    TaxonomyClassifier,
    TaxonomyConfig,
)
from .trimming import (
    TrimmingConfig,
    TrimmingPredictor,
)

__all__ = [
    # Base
    "BaseModel",
    "ModelConfig",
    # Quality models
    "QualityPredictor",
    "QualityPredictorConfig",
    "QualityLoss",
    "EnhancedQualityPredictor",
    "EnhancedQualityConfig",
    "EnhancedQualityLoss",
    "QualityClassifier",
    "QualityClassifierConfig",
    "QualityClassifierCNN",
    "QualityClassifierCNNConfig",
    # Taxonomy models
    "TaxonomyClassifier",
    "TaxonomyConfig",
    "TAXONOMY_LEVELS",
    # Heterozygote models
    "HeterozygoteClassifier",
    "HeterozygoteConfig",
    # Trimming models
    "TrimmingPredictor",
    "TrimmingConfig",
]
