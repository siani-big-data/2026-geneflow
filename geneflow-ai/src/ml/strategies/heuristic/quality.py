"""Heuristic strategies for quality_enhanced analysis.

Only includes artifact detection - other quality_enhanced metrics
are handled by geneflow-analysis with superior algorithms.
"""

import numpy as np

from ..base import ModelStrategy, StrategyResult, StrategyType


class HeuristicArtifactStrategy(ModelStrategy):
    """Rule-based artifact detection in chromatograms.

    This is a candidate for ML enhancement - pattern recognition
    in chromatogram signals can benefit from trained models.
    """

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
