"""Heuristic strategies for variant detection."""

from ..base import ModelStrategy, StrategyResult, StrategyType


class HeuristicSNPStrategy(ModelStrategy):
    """Rule-based SNP calling."""

    strategy_type = StrategyType.HEURISTIC
    model_name = "heuristic_snp"
    model_version = "1.0.0"

    async def execute(
        self,
        query_sequence: str,
        reference_sequence: str,
        quality_scores: list[int] | None = None,
        **kwargs,
    ) -> StrategyResult:
        """Detect SNPs by comparing sequences."""
        query = query_sequence.upper()
        ref = reference_sequence.upper()
        min_len = min(len(query), len(ref))

        snps = []
        for i in range(min_len):
            if query[i] != ref[i] and query[i] in "ATCG" and ref[i] in "ATCG":
                quality = quality_scores[i] if quality_scores and i < len(quality_scores) else 30
                snps.append(
                    {
                        "position": i + 1,
                        "reference": ref[i],
                        "alternate": query[i],
                        "quality": quality,
                        "type": "SNP",
                    }
                )

        confidence = 0.8 if quality_scores else 0.6

        return StrategyResult(
            data={
                "variants": snps,
                "totalVariants": len(snps),
                "variantRate": round(len(snps) / min_len * 100, 4) if min_len > 0 else 0,
            },
            confidence=confidence,
            strategy_used=self.strategy_type,
            model_name=self.model_name,
            model_version=self.model_version,
        )


class HeuristicHeterozygoteStrategy(ModelStrategy):
    """Rule-based heterozygote detection."""

    strategy_type = StrategyType.HEURISTIC
    model_name = "heuristic_heterozygote"
    model_version = "1.0.0"

    MIN_RATIO = 0.25
    MAX_RATIO = 0.75
    MIN_HEIGHT = 100

    async def execute(
        self,
        signal_a: list[float],
        signal_t: list[float],
        signal_c: list[float],
        signal_g: list[float],
        **kwargs,
    ) -> StrategyResult:
        """Detect heterozygous positions from chromatogram signals."""
        signals = {"A": signal_a, "T": signal_t, "C": signal_c, "G": signal_g}
        seq_len = len(signal_a)
        heterozygotes = []

        for pos in range(seq_len):
            heights = [(b, signals[b][pos]) for b in "ATCG"]
            heights.sort(key=lambda x: x[1], reverse=True)

            primary_base, primary_h = heights[0]
            secondary_base, secondary_h = heights[1]

            if primary_h < self.MIN_HEIGHT:
                continue

            ratio = secondary_h / primary_h if primary_h > 0 else 0
            if self.MIN_RATIO <= ratio <= self.MAX_RATIO:
                heterozygotes.append(
                    {
                        "position": pos + 1,
                        "allele1": primary_base,
                        "allele2": secondary_base,
                        "ratio": round(ratio, 3),
                        "genotype": f"{primary_base}/{secondary_base}",
                    }
                )

        return StrategyResult(
            data={
                "heterozygotes": heterozygotes,
                "totalHeterozygotes": len(heterozygotes),
                "heterozygoteRate": (
                    round(len(heterozygotes) / seq_len * 100, 4) if seq_len > 0 else 0
                ),
            },
            confidence=0.75,
            strategy_used=self.strategy_type,
            model_name=self.model_name,
            model_version=self.model_version,
        )
