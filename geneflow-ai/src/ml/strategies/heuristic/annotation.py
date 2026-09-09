"""Heuristic strategies for sequence annotation.

Only includes motif scanning with biological motifs.
ORF detection is handled by geneflow-analysis.
"""

import re

from ..base import ModelStrategy, StrategyResult, StrategyType


class HeuristicMotifStrategy(ModelStrategy):
    """Rule-based scanning for known biological regulatory motifs."""

    strategy_type = StrategyType.HEURISTIC
    model_name = "heuristic_motif"
    model_version = "1.0.0"

    MOTIFS = {
        "TATA_box": r"TATA[AT]A[AT]",
        "CAAT_box": r"GG[CT]CAATCT",
        "GC_box": r"GGGCGG",
        "Kozak": r"[AG]CCATGG",
        "polyA_signal": r"AATAAA",
        "splice_donor": r"[ACGT]AGGT[AG]AGT",
        "splice_acceptor": r"[CT]{10}[ACGT]AG[GT]",
    }

    async def execute(
        self,
        sequence: str,
        motifs: list[str] | None = None,
        **kwargs,
    ) -> StrategyResult:
        """Scan sequence for regulatory motifs."""
        seq = sequence.upper()
        search_motifs = motifs or list(self.MOTIFS.keys())
        found = []

        for motif_name in search_motifs:
            if motif_name not in self.MOTIFS:
                continue
            pattern = self.MOTIFS[motif_name]
            for match in re.finditer(pattern, seq):
                found.append(
                    {
                        "motif": motif_name,
                        "start": match.start() + 1,
                        "end": match.end(),
                        "sequence": match.group(),
                        "strand": "+",
                    }
                )

        return StrategyResult(
            data={
                "motifs": found,
                "totalMotifs": len(found),
                "motifTypes": list(set(m["motif"] for m in found)),
            },
            confidence=0.85,
            strategy_used=self.strategy_type,
            model_name=self.model_name,
            model_version=self.model_version,
        )
