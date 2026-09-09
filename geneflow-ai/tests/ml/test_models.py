"""Tests for ML models."""

import tempfile
from pathlib import Path

import numpy as np
import pytest
import torch

from src.ml.models.base import ModelConfig
from src.ml.models.quality import QualityPredictor, QualityPredictorConfig
from src.ml.models.quality.model import QualityLoss


class TestModelConfig:
    """Tests for ModelConfig."""

    def test_default_config(self):
        config = ModelConfig(name="test")
        assert config.name == "test"
        assert config.version == "1.0.0"
        assert config.hidden_size == 256

    def test_to_dict(self):
        config = ModelConfig(name="test", hidden_size=128)
        d = config.to_dict()
        assert d["name"] == "test"
        assert d["hidden_size"] == 128


class TestQualityPredictor:
    """Tests for QualityPredictor model."""

    @pytest.fixture
    def model(self):
        config = QualityPredictorConfig(
            hidden_channels=32,
            num_layers=2,
        )
        return QualityPredictor(config)

    def test_init(self, model):
        assert model.name == "quality_predictor"
        assert model.count_parameters() > 0

    def test_forward(self, model):
        batch_size = 4
        seq_len = 100
        x = torch.randn(batch_size, 4, seq_len)

        output = model(x)

        assert output.shape == (batch_size, seq_len)
        assert output.min() >= 0
        assert output.max() <= 60

    def test_predict(self, model):
        signals = np.random.randn(4, 100).astype(np.float32)
        quality = model.predict(signals)

        assert quality.shape == (100,)
        assert quality.min() >= 0

    def test_predict_batch(self, model):
        signals = np.random.randn(8, 4, 100).astype(np.float32)
        quality = model.predict(signals)

        assert quality.shape == (8, 100)

    def test_save_load(self, model):
        with tempfile.TemporaryDirectory() as tmpdir:
            path = Path(tmpdir) / "model.pt"
            model.save(path)

            # Load into new model
            new_model = QualityPredictor(model.cfg)
            new_model.load(path)

            # Check weights are same
            for p1, p2 in zip(model.parameters(), new_model.parameters()):
                assert torch.allclose(p1, p2)


class TestQualityLoss:
    """Tests for QualityLoss."""

    @pytest.fixture
    def loss_fn(self):
        return QualityLoss(mse_weight=1.0, smooth_weight=0.1)

    def test_loss_computation(self, loss_fn):
        preds = torch.randn(4, 100)
        targets = torch.randn(4, 100)

        loss = loss_fn(preds, targets)

        assert loss.shape == ()
        assert loss >= 0

    def test_loss_with_mask(self, loss_fn):
        preds = torch.randn(4, 100)
        targets = torch.randn(4, 100)
        mask = torch.ones(4, 100, dtype=torch.bool)
        mask[:, 80:] = False

        loss = loss_fn(preds, targets, mask)

        assert loss.shape == ()

    def test_perfect_prediction(self, loss_fn):
        # Use constant values to avoid smoothness penalty
        targets = torch.ones(4, 100) * 30.0
        preds = targets.clone()

        loss = loss_fn(preds, targets)

        assert loss < 0.01  # MSE=0, smoothness=0
