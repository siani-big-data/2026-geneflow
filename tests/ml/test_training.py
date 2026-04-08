"""Tests for training utilities."""

import numpy as np
import torch
import torch.nn as nn

from src.ml.models.base import BaseModel, ModelConfig
from src.ml.training.callbacks import EarlyStopping
from src.ml.training.metrics import (
    MetricsTracker,
    compute_classification_metrics,
    compute_regression_metrics,
)


class DummyModel(BaseModel):
    """Dummy model for testing."""

    def __init__(self):
        super().__init__(ModelConfig(name="dummy"))
        self.linear = nn.Linear(10, 1)

    def forward(self, x):
        return self.linear(x)

    def predict(self, x):
        return self.forward(torch.tensor(x)).detach().numpy()


class TestMetricsTracker:
    """Tests for MetricsTracker."""

    def test_log_epoch(self):
        tracker = MetricsTracker()
        tracker.log_epoch("train", 0, {"loss": 1.0, "mae": 2.0})
        tracker.log_epoch("train", 1, {"loss": 0.5, "mae": 1.5})

        assert tracker.get_last("train/loss") == 0.5
        assert tracker.get_last("train/mae") == 1.5

    def test_get_best(self):
        tracker = MetricsTracker()
        tracker.log_epoch("val", 0, {"loss": 1.0})
        tracker.log_epoch("val", 1, {"loss": 0.5})
        tracker.log_epoch("val", 2, {"loss": 0.8})

        best = tracker.get_best("val/loss", mode="min")
        assert best == (1, 0.5)

    def test_get_summary(self):
        tracker = MetricsTracker()
        tracker.log_epoch("train", 0, {"loss": 1.0})
        tracker.log_epoch("train", 1, {"loss": 0.5})

        summary = tracker.get_summary()
        assert "train/loss" in summary
        assert summary["train/loss"]["last"] == 0.5
        assert summary["train/loss"]["best"] == 0.5


class TestEarlyStopping:
    """Tests for EarlyStopping callback."""

    def test_no_improvement(self):
        callback = EarlyStopping(patience=3, metric="loss", mode="min")

        # Simulate no improvement
        for i in range(5):
            stop = callback.on_epoch_end(None, i, {"loss": 1.0})
            if i < 3:
                assert stop is False
            else:
                assert stop is True
                break

    def test_improvement_resets_counter(self):
        callback = EarlyStopping(patience=3, metric="loss", mode="min")

        assert callback.on_epoch_end(None, 0, {"loss": 1.0}) is False
        assert callback.on_epoch_end(None, 1, {"loss": 1.0}) is False
        assert callback.on_epoch_end(None, 2, {"loss": 0.5}) is False  # Improved
        assert callback.on_epoch_end(None, 3, {"loss": 0.5}) is False  # Reset counter
        assert callback.on_epoch_end(None, 4, {"loss": 0.5}) is False
        assert callback.on_epoch_end(None, 5, {"loss": 0.5}) is True  # Patience exhausted


class TestRegressionMetrics:
    """Tests for regression metrics."""

    def test_perfect_prediction(self):
        targets = [1.0, 2.0, 3.0, 4.0, 5.0]
        predictions = targets.copy()

        metrics = compute_regression_metrics(
            predictions=np.array(predictions),
            targets=np.array(targets),
        )

        assert metrics["mae"] == 0.0
        assert metrics["mse"] == 0.0
        assert metrics["r2"] == 1.0

    def test_with_error(self):
        targets = np.array([1.0, 2.0, 3.0, 4.0, 5.0])
        predictions = targets + 1.0  # Off by 1

        metrics = compute_regression_metrics(predictions, targets)

        assert metrics["mae"] == 1.0
        assert metrics["mse"] == 1.0


class TestClassificationMetrics:
    """Tests for classification metrics."""

    def test_perfect_classification(self):
        predictions = np.array([0.9, 0.9, 0.1, 0.1])
        targets = np.array([1, 1, 0, 0])

        metrics = compute_classification_metrics(predictions, targets)

        assert metrics["accuracy"] == 1.0
        assert metrics["precision"] == 1.0
        assert metrics["recall"] == 1.0
        assert metrics["f1"] == 1.0

    def test_with_errors(self):
        predictions = np.array([0.9, 0.1, 0.9, 0.1])  # 2 correct, 2 wrong
        targets = np.array([1, 1, 0, 0])

        metrics = compute_classification_metrics(predictions, targets)

        assert metrics["accuracy"] == 0.5
