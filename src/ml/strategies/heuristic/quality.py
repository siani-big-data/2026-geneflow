"""Heuristic strategies for quality analysis."""

import numpy as np

from ..base import ModelStrategy, StrategyResult, StrategyType


class HeuristicQualityStrategy(ModelStrategy):
    """Rule-based quality prediction."""

    strategy_type = StrategyType.HEURISTIC
    model_name = "heuristic_quality"
    model_version = "1.0.0"

    async def execute(
        self,
        quality_scores: list[int] | None = None,
        sequence: str | None = None,
        **kwargs,
    ) -> StrategyResult:
        """Predict quality metrics using heuristics."""
        if quality_scores:
            scores = np.array(quality_scores)
            q20 = float(np.sum(scores >= 20) / len(scores) * 100)
            q30 = float(np.sum(scores >= 30) / len(scores) * 100)
            mean_quality = float(np.mean(scores))
            confidence = min(mean_quality / 40, 1.0)
        else:
            # Estimate from sequence if no scores
            q20, q30 = 85.0, 70.0
            mean_quality = 25.0
            confidence = 0.5

        return StrategyResult(
            data={
                "q20Percentage": round(q20, 2),
                "q30Percentage": round(q30, 2),
                "meanQuality": round(mean_quality, 2),
                "predictedAccuracy": round(100 - (10 ** (-mean_quality / 10)) * 100, 4),
            },
            confidence=round(confidence, 3),
            strategy_used=self.strategy_type,
            model_name=self.model_name,
            model_version=self.model_version,
        )


class HeuristicTrimStrategy(ModelStrategy):
    """Rule-based auto-trimming."""

    strategy_type = StrategyType.HEURISTIC
    model_name = "heuristic_trimmer"
    model_version = "1.0.0"

    MIN_QUALITY = 20
    WINDOW_SIZE = 10

    async def execute(
        self,
        quality_scores: list[int],
        **kwargs,
    ) -> StrategyResult:
        """Find optimal trim points using sliding window."""
        scores = np.array(quality_scores)
        seq_len = len(scores)
        window = min(self.WINDOW_SIZE, seq_len // 4)

        # Find trim start
        trim_start = 0
        for i in range(seq_len - window):
            if np.mean(scores[i : i + window]) >= self.MIN_QUALITY:
                trim_start = i
                break

        # Find trim end
        trim_end = seq_len
        for i in range(seq_len - 1, window, -1):
            if np.mean(scores[i - window : i]) >= self.MIN_QUALITY:
                trim_end = i
                break

        if trim_end <= trim_start:
            trim_start, trim_end = 0, seq_len

        trimmed_scores = scores[trim_start:trim_end]
        confidence = min(np.mean(trimmed_scores) / 40, 1.0) if len(trimmed_scores) > 0 else 0.0

        return StrategyResult(
            data={
                "trimStart": int(trim_start),
                "trimEnd": int(trim_end),
                "trimmedLength": int(trim_end - trim_start),
                "basesRemoved": int(seq_len - (trim_end - trim_start)),
            },
            confidence=round(float(confidence), 3),
            strategy_used=self.strategy_type,
            model_name=self.model_name,
            model_version=self.model_version,
        )


class HeuristicArtifactStrategy(ModelStrategy):
    """Rule-based artifact detection."""

    strategy_type = StrategyType.HEURISTIC
    model_name = "heuristic_artifact"
    model_version = "1.0.0"

    async def execute(
        self,
        signal_a: list[float],
        signal_t: list[float],
        signal_c: list[float],
        signal_g: list[float],
        **kwargs,
    ) -> StrategyResult:
        """Detect artifacts using signal analysis heuristics."""
        signals = {
            "A": np.array(signal_a),
            "T": np.array(signal_t),
            "C": np.array(signal_c),
            "G": np.array(signal_g),
        }

        artifacts = []
        seq_len = len(signal_a)

        for pos in range(seq_len):
            heights = [signals[b][pos] for b in "ATCG"]
            max_h = max(heights)

            # Spike detection
            if max_h > 3000:
                artifacts.append({"position": pos, "type": "spike", "severity": "high"})
            # Low signal
            elif max_h < 50:
                artifacts.append({"position": pos, "type": "low_signal", "severity": "medium"})

        return StrategyResult(
            data={
                "artifacts": artifacts,
                "totalArtifacts": len(artifacts),
                "artifactRate": round(len(artifacts) / seq_len * 100, 2) if seq_len > 0 else 0,
            },
            confidence=0.7,
            strategy_used=self.strategy_type,
            model_name=self.model_name,
            model_version=self.model_version,
        )
