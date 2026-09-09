"""Heuristic strategies for functional prediction.

Includes mutation impact prediction which can benefit from ML.
RNA structure prediction is better handled by ViennaRNA.
"""

from ..base import ModelStrategy, StrategyResult, StrategyType


class HeuristicMutationImpactStrategy(ModelStrategy):
    """Rule-based mutation impact prediction.

    This is a candidate for ML enhancement - tools like SIFT/PolyPhen
    use conservation + ML for better predictions.
    """

    strategy_type = StrategyType.HEURISTIC
    model_name = "heuristic_mutation_impact"
    model_version = "1.0.0"

    CODON_TABLE = {
        "TTT": "F", "TTC": "F", "TTA": "L", "TTG": "L",
        "TCT": "S", "TCC": "S", "TCA": "S", "TCG": "S",
        "TAT": "Y", "TAC": "Y", "TAA": "*", "TAG": "*",
        "TGT": "C", "TGC": "C", "TGA": "*", "TGG": "W",
        "CTT": "L", "CTC": "L", "CTA": "L", "CTG": "L",
        "CCT": "P", "CCC": "P", "CCA": "P", "CCG": "P",
        "CAT": "H", "CAC": "H", "CAA": "Q", "CAG": "Q",
        "CGT": "R", "CGC": "R", "CGA": "R", "CGG": "R",
        "ATT": "I", "ATC": "I", "ATA": "I", "ATG": "M",
        "ACT": "T", "ACC": "T", "ACA": "T", "ACG": "T",
        "AAT": "N", "AAC": "N", "AAA": "K", "AAG": "K",
        "AGT": "S", "AGC": "S", "AGA": "R", "AGG": "R",
        "GTT": "V", "GTC": "V", "GTA": "V", "GTG": "V",
        "GCT": "A", "GCC": "A", "GCA": "A", "GCG": "A",
        "GAT": "D", "GAC": "D", "GAA": "E", "GAG": "E",
        "GGT": "G", "GGC": "G", "GGA": "G", "GGG": "G",
    }

    async def execute(
        self,
        reference_codon: str,
        alternate_codon: str,
        position: int = 0,
        **kwargs,
    ) -> StrategyResult:
        """Predict mutation impact."""
        ref = reference_codon.upper()
        alt = alternate_codon.upper()

        ref_aa = self.CODON_TABLE.get(ref, "X")
        alt_aa = self.CODON_TABLE.get(alt, "X")

        # Determine mutation type
        if ref_aa == alt_aa:
            mutation_type = "synonymous"
            impact = "benign"
            severity = 0.1
        elif alt_aa == "*":
            mutation_type = "nonsense"
            impact = "damaging"
            severity = 0.95
        elif ref_aa == "*":
            mutation_type = "readthrough"
            impact = "damaging"
            severity = 0.8
        else:
            mutation_type = "missense"
            severity = self._estimate_missense_severity(ref_aa, alt_aa)
            impact = "damaging" if severity > 0.5 else "tolerated"

        return StrategyResult(
            data={
                "position": position,
                "referenceCodon": ref,
                "alternateCodon": alt,
                "referenceAA": ref_aa,
                "alternateAA": alt_aa,
                "mutationType": mutation_type,
                "impact": impact,
                "severity": round(severity, 3),
            },
            confidence=0.6,  # Heuristic has lower confidence
            strategy_used=self.strategy_type,
            model_name=self.model_name,
            model_version=self.model_version,
        )

    def _estimate_missense_severity(self, ref_aa: str, alt_aa: str) -> float:
        """Estimate severity based on amino acid properties."""
        HYDROPHOBIC = set("AILMFVPW")
        POLAR = set("STNQCY")
        CHARGED_POS = set("RKH")
        CHARGED_NEG = set("DE")
        SPECIAL = set("GP")

        def get_class(aa: str) -> str:
            if aa in HYDROPHOBIC:
                return "hydrophobic"
            if aa in POLAR:
                return "polar"
            if aa in CHARGED_POS:
                return "positive"
            if aa in CHARGED_NEG:
                return "negative"
            if aa in SPECIAL:
                return "special"
            return "unknown"

        ref_class = get_class(ref_aa)
        alt_class = get_class(alt_aa)

        if ref_class == alt_class:
            return 0.3  # Conservative
        if {ref_class, alt_class} == {"positive", "negative"}:
            return 0.9  # Charge reversal
        if "special" in {ref_class, alt_class}:
            return 0.7  # Proline/Glycine changes
        return 0.5  # Moderate
