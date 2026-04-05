"""Unified training results system for reproducible ML experiments.

Provides a standardized format for storing training results, predictions,
and generating publication-ready figures and statistics.
"""

import json
import pickle
from dataclasses import dataclass, field, asdict
from datetime import datetime
from pathlib import Path
from typing import Any, Literal

import numpy as np


@dataclass
class HardwareInfo:
    """Hardware and environment information."""

    backend: str  # "sklearn_cpu", "xgboost_gpu", "pytorch_gpu", etc.
    device: str | None = None  # "cpu", "cuda:0", etc.
    gpu_name: str | None = None
    gpu_memory_mb: float | None = None
    cpu_count: int | None = None

    def to_dict(self) -> dict:
        return {k: v for k, v in asdict(self).items() if v is not None}


@dataclass
class DatasetInfo:
    """Dataset information."""

    name: str
    task: Literal["classification", "regression", "multilabel"]
    train_samples: int
    val_samples: int
    test_samples: int = 0
    n_features: int = 0

    # Classification specific
    n_classes: int = 0
    class_names: list[str] = field(default_factory=list)
    class_distribution: dict[str, int] = field(default_factory=dict)

    # Feature info
    featuREDACTED: list[str] = field(default_factory=list)

    def to_dict(self) -> dict:
        d = asdict(self)
        # Remove empty fields
        return {k: v for k, v in d.items() if v or isinstance(v, (int, float))}


@dataclass
class ClassMetrics:
    """Per-class metrics for classification."""

    name: str
    precision: float
    recall: float
    f1: float
    support: int

    # Optional advanced metrics
    auc_roc: float | None = None
    auc_pr: float | None = None

    def to_dict(self) -> dict:
        return {k: v for k, v in asdict(self).items() if v is not None}


@dataclass
class Predictions:
    """Stored predictions for later analysis."""

    y_true: np.ndarray
    y_pred: np.ndarray
    y_proba: np.ndarray | None = None  # For classification

    def save(self, path: Path) -> None:
        """Save predictions to npz file."""
        data = {"y_true": self.y_true, "y_pred": self.y_pred}
        if self.y_proba is not None:
            data["y_proba"] = self.y_proba
        np.savez_compressed(path, **data)

    @classmethod
    def load(cls, path: Path) -> "Predictions":
        """Load predictions from npz file."""
        data = np.load(path)
        return cls(
            y_true=data["y_true"],
            y_pred=data["y_pred"],
            y_proba=data.get("y_proba"),
        )


@dataclass
class EpochMetrics:
    """Metrics for a single epoch (neural network training)."""

    epoch: int
    train_loss: float
    val_loss: float | None = None
    train_accuracy: float | None = None
    val_accuracy: float | None = None
    train_f1: float | None = None
    val_f1: float | None = None
    learning_rate: float | None = None

    # Additional metrics as dict
    extra: dict = field(default_factory=dict)

    def to_dict(self) -> dict:
        d = {
            "epoch": self.epoch,
            "train_loss": self.train_loss,
        }
        for k in ["val_loss", "train_accuracy", "val_accuracy",
                  "train_f1", "val_f1", "learning_rate"]:
            v = getattr(self, k)
            if v is not None:
                d[k] = v
        d.update(self.extra)
        return d


@dataclass
class TrainingResult:
    """Complete training result for any ML model.

    This is the main class that stores all information about a training run,
    including configuration, metrics, predictions, and metadata needed for
    reproducibility and paper generation.
    """

    # Identifiers
    run_id: str
    model_name: str
    model_type: str  # "RandomForest", "XGBoost", "CNN", "Transformer", etc.

    # Dataset info
    dataset: DatasetInfo

    # Hardware info
    hardware: HardwareInfo

    # Hyperparameters (model-specific)
    hyperparameters: dict = field(default_factory=dict)

    # Training time
    training_time_sec: float = 0.0

    # Global metrics
    train_metrics: dict = field(default_factory=dict)
    val_metrics: dict = field(default_factory=dict)
    test_metrics: dict = field(default_factory=dict)

    # Per-class metrics (classification)
    per_class_metrics: list[ClassMetrics] = field(default_factory=list)

    # Epoch history (for neural networks)
    history: list[EpochMetrics] = field(default_factory=list)

    # Feature importance (for tree-based models)
    featuREDACTED: dict[str, float] = field(default_factory=dict)

    # Confusion matrix (classification)
    confusion_matrix: np.ndarray | None = None

    # Timestamps
    timestamp: str = field(default_factory=lambda: datetime.now().isoformat())

    # Extra metadata
    metadata: dict = field(default_factory=dict)

    def to_dict(self) -> dict:
        """Convert to dictionary for JSON serialization."""
        d = {
            "run_id": self.run_id,
            "model_name": self.model_name,
            "model_type": self.model_type,
            "timestamp": self.timestamp,
            "dataset": self.dataset.to_dict(),
            "hardware": self.hardware.to_dict(),
            "hyperparameters": self.hyperparameters,
            "training_time_sec": round(self.training_time_sec, 2),
            "train_metrics": self.train_metrics,
            "val_metrics": self.val_metrics,
        }

        if self.test_metrics:
            d["test_metrics"] = self.test_metrics

        if self.per_class_metrics:
            d["per_class_metrics"] = [m.to_dict() for m in self.per_class_metrics]

        if self.history:
            d["history"] = [h.to_dict() for h in self.history]

        if self.featuREDACTED:
            # Sort by importance and include top 50
            sorted_fi = sorted(self.featuREDACTED.items(), key=lambda x: -x[1])
            d["featuREDACTED"] = dict(sorted_fi[:50])

        if self.confusion_matrix is not None:
            d["confusion_matrix"] = self.confusion_matrix.tolist()

        if self.metadata:
            d["metadata"] = self.metadata

        return d

    @classmethod
    def from_dict(cls, d: dict) -> "TrainingResult":
        """Create from dictionary."""
        # Parse nested objects
        dataset = DatasetInfo(**d["dataset"])
        hardware = HardwareInfo(**d["hardware"])

        per_class = []
        if "per_class_metrics" in d:
            per_class = [ClassMetrics(**m) for m in d["per_class_metrics"]]

        history = []
        if "history" in d:
            history = [EpochMetrics(**h) for h in d["history"]]

        cm = None
        if "confusion_matrix" in d:
            cm = np.array(d["confusion_matrix"])

        return cls(
            run_id=d["run_id"],
            model_name=d["model_name"],
            model_type=d["model_type"],
            timestamp=d.get("timestamp", ""),
            dataset=dataset,
            hardware=hardware,
            hyperparameters=d.get("hyperparameters", {}),
            training_time_sec=d.get("training_time_sec", 0),
            train_metrics=d.get("train_metrics", {}),
            val_metrics=d.get("val_metrics", {}),
            test_metrics=d.get("test_metrics", {}),
            per_class_metrics=per_class,
            history=history,
            featuREDACTED=d.get("featuREDACTED", {}),
            confusion_matrix=cm,
            metadata=d.get("metadata", {}),
        )


class ResultsWriter:
    """Write training results to standardized directory structure."""

    def __init__(self, base_dir: str | Path = "checkpoints"):
        self.base_dir = Path(base_dir)

    def save(
        self,
        result: TrainingResult,
        model: Any = None,
        train_predictions: Predictions | None = None,
        val_predictions: Predictions | None = None,
        test_predictions: Predictions | None = None,
    ) -> Path:
        """Save complete training results.

        Creates directory structure:
            {base_dir}/{model_type}_{run_id}/
                ├── result.json         # Main results file
                ├── model.pkl           # Serialized model
                ├── predictions/
                │   ├── train.npz
                │   ├── val.npz
                │   └── test.npz
                └── figures/            # Created by PaperFigures
        """
        # Create output directory
        output_dir = self.base_dir / f"{result.model_type.lower()}_{result.run_id}"
        output_dir.mkdir(parents=True, exist_ok=True)

        # Save main results JSON
        result_path = output_dir / "result.json"
        with open(result_path, "w") as f:
            json.dump(result.to_dict(), f, indent=2)

        # Save model
        if model is not None:
            model_path = output_dir / "model.pkl"
            with open(model_path, "wb") as f:
                pickle.dump(model, f)

        # Save predictions
        if any([train_predictions, val_predictions, test_predictions]):
            pred_dir = output_dir / "predictions"
            pred_dir.mkdir(exist_ok=True)

            if train_predictions:
                train_predictions.save(pred_dir / "train.npz")
            if val_predictions:
                val_predictions.save(pred_dir / "val.npz")
            if test_predictions:
                test_predictions.save(pred_dir / "test.npz")

        print(f"Results saved to: {output_dir}")
        return output_dir


class ResultsReader:
    """Read and analyze training results."""

    def __init__(self, base_dir: str | Path = "checkpoints"):
        self.base_dir = Path(base_dir)

    def load(self, run_dir: str | Path) -> TrainingResult:
        """Load results from a run directory."""
        run_dir = Path(run_dir)
        if not run_dir.is_absolute():
            run_dir = self.base_dir / run_dir

        result_path = run_dir / "result.json"
        with open(result_path) as f:
            return TrainingResult.from_dict(json.load(f))

    def load_predictions(
        self,
        run_dir: str | Path,
        split: str = "val"
    ) -> Predictions:
        """Load predictions for a specific split."""
        run_dir = Path(run_dir)
        if not run_dir.is_absolute():
            run_dir = self.base_dir / run_dir

        pred_path = run_dir / "predictions" / f"{split}.npz"
        return Predictions.load(pred_path)

    def load_model(self, run_dir: str | Path) -> Any:
        """Load the trained model."""
        run_dir = Path(run_dir)
        if not run_dir.is_absolute():
            run_dir = self.base_dir / run_dir

        model_path = run_dir / "model.pkl"
        with open(model_path, "rb") as f:
            return pickle.load(f)

    def list_runs(self, model_type: str | None = None) -> list[Path]:
        """List all run directories, optionally filtered by model type."""
        runs = []
        for d in self.base_dir.iterdir():
            if d.is_dir() and (d / "result.json").exists():
                if model_type is None or d.name.startswith(model_type.lower()):
                    runs.append(d)
        return sorted(runs, key=lambda x: x.stat().st_mtime, reverse=True)

    def compaREDACTED(self, run_dirs: list[str | Path]) -> dict:
        """Compare multiple runs side by side."""
        results = {}
        for run_dir in run_dirs:
            result = self.load(run_dir)
            results[result.run_id] = {
                "model": result.model_name,
                "val_accuracy": result.val_metrics.get("accuracy"),
                "val_f1": result.val_metrics.get("f1_macro"),
                "train_time": result.training_time_sec,
                "n_params": result.hyperparameters.get("n_estimators") or
                           result.metadata.get("total_parameters"),
            }
        return results


def compute_classification_result(
    y_true: np.ndarray,
    y_pred: np.ndarray,
    y_proba: np.ndarray | None,
    class_names: list[str],
) -> tuple[dict, list[ClassMetrics], np.ndarray]:
    """Compute classification metrics from predictions.

    Returns:
        metrics: Global metrics dict
        per_class: List of ClassMetrics
        cm: Confusion matrix
    """
    from sklearn.metrics import (
        accuracy_score,
        f1_score,
        precision_score,
        recall_score,
        confusion_matrix,
        classification_report,
    )

    metrics = {
        "accuracy": float(accuracy_score(y_true, y_pred)),
        "f1_macro": float(f1_score(y_true, y_pred, average="macro", zero_division=0)),
        "f1_weighted": float(f1_score(y_true, y_pred, average="weighted", zero_division=0)),
        "precision_macro": float(precision_score(y_true, y_pred, average="macro", zero_division=0)),
        "recall_macro": float(recall_score(y_true, y_pred, average="macro", zero_division=0)),
    }

    # Top-k accuracy if probabilities available
    if y_proba is not None and len(class_names) > 3:
        from sklearn.metrics import top_k_accuracy_score
        for k in [3, 5]:
            if len(class_names) > k:
                metrics[f"top_{k}_accuracy"] = float(
                    top_k_accuracy_score(y_true, y_proba, k=k)
                )

    # Per-class metrics
    report = classification_report(
        y_true, y_pred,
        target_names=class_names,
        output_dict=True,
        zero_division=0,
    )

    per_class = []
    for name in class_names:
        if name in report:
            per_class.append(ClassMetrics(
                name=name,
                precision=report[name]["precision"],
                recall=report[name]["recall"],
                f1=report[name]["f1-score"],
                support=int(report[name]["support"]),
            ))

    # Confusion matrix
    cm = confusion_matrix(y_true, y_pred)

    return metrics, per_class, cm
