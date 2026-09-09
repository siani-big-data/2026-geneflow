"""Metrics tracking for training."""

from collections import defaultdict
from dataclasses import dataclass, field

import numpy as np


@dataclass
class MetricsTracker:
    """Track and aggregate training metrics."""

    history: dict = field(default_factory=lambda: defaultdict(list))
    current_epoch: dict = field(default_factory=dict)

    def log_epoch(self, phase: str, epoch: int, metrics: dict) -> None:
        """Log metrics for an epoch."""
        for key, value in metrics.items():
            full_key = f"{phase}/{key}"
            self.history[full_key].append((epoch, value))

    def log_step(self, phase: str, step: int, metrics: dict) -> None:
        """Log metrics for a step."""
        for key, value in metrics.items():
            full_key = f"{phase}/{key}"
            if full_key not in self.current_epoch:
                self.current_epoch[full_key] = []
            self.current_epoch[full_key].append(value)

    def get_last(self, key: str) -> float | None:
        """Get last value for a metric."""
        if key in self.history and self.history[key]:
            return self.history[key][-1][1]
        return None

    def get_best(self, key: str, mode: str = "min") -> tuple[int, float] | None:
        """Get best value and epoch for a metric."""
        if key not in self.history or not self.history[key]:
            return None

        values = self.history[key]
        if mode == "min":
            best = min(values, key=lambda x: x[1])
        else:
            best = max(values, key=lambda x: x[1])
        return best

    def get_summary(self) -> dict:
        """Get summary of all metrics."""
        summary = {}

        for key, values in self.history.items():
            if not values:
                continue

            epochs, vals = zip(*values)
            vals_array = np.array(vals)

            summary[key] = {
                "last": vals[-1],
                "best": float(vals_array.min()) if "loss" in key else float(vals_array.max()),
                "mean": float(vals_array.mean()),
                "std": float(vals_array.std()),
            }

        return summary

    def reset_epoch(self) -> dict:
        """Reset current epoch metrics and return averages."""
        averages = {}
        for key, values in self.current_epoch.items():
            if values:
                averages[key] = np.mean(values)
        self.current_epoch.clear()
        return averages


def compute_regression_metrics(
    predictions: np.ndarray,
    targets: np.ndarray,
    mask: np.ndarray | None = None,
) -> dict:
    """Compute regression metrics."""
    if mask is not None:
        predictions = predictions[mask]
        targets = targets[mask]

    diff = predictions - targets

    return {
        "mae": float(np.abs(diff).mean()),
        "mse": float((diff**2).mean()),
        "rmse": float(np.sqrt((diff**2).mean())),
        "r2": float(1 - (diff**2).sum() / ((targets - targets.mean()) ** 2).sum()),
        "pearson": float(np.corrcoef(predictions.flatten(), targets.flatten())[0, 1]),
    }


def compute_classification_metrics(
    predictions: np.ndarray,
    targets: np.ndarray,
    threshold: float = 0.5,
) -> dict:
    """Compute binary classification metrics."""
    preds_binary = (predictions >= threshold).astype(int)
    targets_binary = targets.astype(int)

    tp = ((preds_binary == 1) & (targets_binary == 1)).sum()
    tn = ((preds_binary == 0) & (targets_binary == 0)).sum()
    fp = ((preds_binary == 1) & (targets_binary == 0)).sum()
    fn = ((preds_binary == 0) & (targets_binary == 1)).sum()

    accuracy = (tp + tn) / (tp + tn + fp + fn) if (tp + tn + fp + fn) > 0 else 0
    precision = tp / (tp + fp) if (tp + fp) > 0 else 0
    recall = tp / (tp + fn) if (tp + fn) > 0 else 0
    f1 = 2 * precision * recall / (precision + recall) if (precision + recall) > 0 else 0

    return {
        "accuracy": float(accuracy),
        "precision": float(precision),
        "recall": float(recall),
        "f1": float(f1),
        "tp": int(tp),
        "tn": int(tn),
        "fp": int(fp),
        "fn": int(fn),
    }
