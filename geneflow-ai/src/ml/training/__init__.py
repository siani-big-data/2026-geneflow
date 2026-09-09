"""Training utilities for ML models."""

from .callbacks import (
    DetailedProgressLogger,
    EarlyStopping,
    ModelCheckpoint,
    ProgressLogger,
    TrainingCallback,
)
from .figures import (
    PaperFigures,
    generate_latex_table,
)
from .losses import (
    FocalLoss,
    HierarchicalFocalLoss,
    LabelSmoothingCrossEntropy,
)
from .metrics import MetricsTracker
from .report import (
    ModelInfo,
    TrainingReport,
    analyze_model,
    compute_accuracy_regression,
    compute_regression_metrics_detailed,
    generate_training_plots,
)
from .results import (
    ClassMetrics,
    DatasetInfo,
    EpochMetrics,
    HardwareInfo,
    Predictions,
    ResultsReader,
    ResultsWriter,
    TrainingResult,
    compute_classification_result,
)
from .trainer import Trainer, TrainingConfig

__all__ = [
    # Trainer
    "Trainer",
    "TrainingConfig",
    # Callbacks
    "EarlyStopping",
    "ModelCheckpoint",
    "TrainingCallback",
    "ProgressLogger",
    "DetailedProgressLogger",
    # Losses
    "FocalLoss",
    "HierarchicalFocalLoss",
    "LabelSmoothingCrossEntropy",
    # Metrics
    "MetricsTracker",
    # Report (legacy)
    "TrainingReport",
    "ModelInfo",
    "analyze_model",
    "generate_training_plots",
    "compute_accuracy_regression",
    "compute_regression_metrics_detailed",
    # Results (new unified system)
    "TrainingResult",
    "DatasetInfo",
    "HardwareInfo",
    "ClassMetrics",
    "EpochMetrics",
    "Predictions",
    "ResultsWriter",
    "ResultsReader",
    "compute_classification_result",
    # Figures
    "PaperFigures",
    "generate_latex_table",
]
