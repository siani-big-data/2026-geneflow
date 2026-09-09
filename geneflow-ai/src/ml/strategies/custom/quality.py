"""Custom quality_enhanced strategy using trained model."""

from pathlib import Path

import numpy as np

from ..base import ModelStrategy, StrategyResult, StrategyType


class CustomQualityStrategy(ModelStrategy):
    """Quality prediction using custom trained CNN model.

    Uses the QualityPredictor model trained on Sanger trace data.
    Falls back to heuristic if model not available.
    """

    strategy_type = StrategyType.CUSTOM
    model_name = "custom_quality_cnn"
    model_version = "1.0.0"

    # Default model path
    DEFAULT_MODEL_PATH = "models/quality_predictor/best.pt"

    def __init__(self, model_path: str | Path | None = None):
        self._model_path = Path(model_path) if model_path else Path(self.DEFAULT_MODEL_PATH)
        self._model = None
        self._load_attempted = False

    def _load_model(self) -> bool:
        """Lazy load the model."""
        if self._load_attempted:
            return self._model is not None

        self._load_attempted = True

        if not self._model_path.exists():
            return False

        try:
            from ...models.quality import QualityPredictor, QualityPredictorConfig

            config = QualityPredictorConfig()
            self._model = QualityPredictor(config)
            self._model.load(self._model_path)
            self._model.eval()
            self._model.to_device()
            return True

        except Exception:
            return False

    @property
    def is_available(self) -> bool:
        """Check if model is available."""
        return self._load_model()

    async def execute(
        self,
        signal_a: list[float] | None = None,
        signal_t: list[float] | None = None,
        signal_c: list[float] | None = None,
        signal_g: list[float] | None = None,
        quality_scores: list[int] | None = None,
        **kwargs,
    ) -> StrategyResult:
        """Predict quality_enhanced scores from signals.

        Args:
            signal_a: A channel signal
            signal_t: T channel signal
            signal_c: C channel signal
            signal_g: G channel signal
            quality_scores: Existing quality_enhanced scores (for comparison)

        Returns:
            StrategyResult with predicted quality_enhanced metrics
        """
        if not self.is_available:
            return StrategyResult(
                data={"error": "Custom quality_enhanced model not available"},
                confidence=0.0,
                strategy_used=self.strategy_type,
                model_name=self.model_name,
                model_version=self.model_version,
            )

        # Prepare signals
        if signal_a is None or signal_t is None or signal_c is None or signal_g is None:
            return StrategyResult(
                data={"error": "All 4 signal channels required"},
                confidence=0.0,
                strategy_used=self.strategy_type,
                model_name=self.model_name,
                model_version=self.model_version,
            )

        signals = np.stack(
            [
                np.array(signal_a, dtype=np.float32),
                np.array(signal_t, dtype=np.float32),
                np.array(signal_c, dtype=np.float32),
                np.array(signal_g, dtype=np.float32),
            ]
        )

        # Normalize
        max_val = signals.max()
        if max_val > 0:
            signals = signals / max_val

        # Predict
        predicted_quality = self._model.predict(signals)

        # Compute metrics
        q20 = float(np.sum(predicted_quality >= 20) / len(predicted_quality) * 100)
        q30 = float(np.sum(predicted_quality >= 30) / len(predicted_quality) * 100)
        mean_quality = float(np.mean(predicted_quality))

        # Confidence based on prediction certainty
        confidence = min(mean_quality / 40, 1.0)

        # Compare with ground truth if available
        comparison = {}
        if quality_scores is not None:
            gt = np.array(quality_scores)
            if len(gt) == len(predicted_quality):
                mae = float(np.abs(predicted_quality - gt).mean())
                comparison = {
                    "maeVsGroundTruth": round(mae, 2),
                    "correlationVsGroundTruth": round(
                        float(np.corrcoef(predicted_quality, gt)[0, 1]), 4
                    ),
                }

        return StrategyResult(
            data={
                "q20Percentage": round(q20, 2),
                "q30Percentage": round(q30, 2),
                "meanQuality": round(mean_quality, 2),
                "predictedAccuracy": round(100 - (10 ** (-mean_quality / 10)) * 100, 4),
                "qualityScores": predicted_quality.tolist(),
                **comparison,
            },
            confidence=round(confidence, 3),
            strategy_used=self.strategy_type,
            model_name=self.model_name,
            model_version=self.model_version,
        )
