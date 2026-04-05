"""Custom ML models for GeneFlow.

Available models:
    - QualityPredictor: Predict Phred quality scores from signals
    - QualityClassifier: Classify quality into bins (Q10-Q50+)
    - TaxonomyClassifier: Hierarchical taxonomy classification
    - HeterozygoteClassifier: Detect heterozygous positions
    - TrimmingPredictor: Predict optimal trim points
"""

from .base import BaseModel, ModelConfig
from .quality import (
    QualityLoss,
    QualityPredictor,
    QualityPredictorConfig,
    EnhancedQualityPredictor,
    EnhancedQualityConfig,
    EnhancedQualityLoss,
)
from .quality.classifier import (
    QualityClassifier,
    QualityClassifierConfig,
    QualityClassifierCNN,
    QualityClassifierCNNConfig,
)
from .taxonomy import (
    TaxonomyClassifier,
    TaxonomyConfig,
    TAXONOMY_LEVELS,
)
from .heterozygote import (
    HeterozygoteClassifier,
    HeterozygoteConfig,
)
from .trimming import (
    TrimmingPredictor,
    TrimmingConfig,
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
