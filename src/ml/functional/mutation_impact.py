"""
Mutation impact prediction model.

Predice el impacto funcional de variantes: sinónima, missense,
nonsense, frameshift, etc.

Similar a SIFT/PolyPhen pero para uso local.
"""

import time
from dataclasses import dataclass
from enum import Enum
from pathlib import Path
from typing import Optional

from ..base import LocalModel, ModelCategory, PredictionResult


class ImpactSeverity(str, Enum):
    """Severidad del impacto."""

    BENIGN = "benign"
    TOLERATED = "tolerated"
    UNCERTAIN = "uncertain"
    POSSIBLY_DAMAGING = "possibly_damaging"
    PROBABLY_DAMAGING = "probably_damaging"
    DAMAGING = "damaging"


class VariantEffect(str, Enum):
    """Tipo de efecto de la variante."""

    SYNONYMOUS = "synonymous"
    MISSENSE = "missense"
    NONSENSE = "nonsense"
    FRAMESHIFT = "frameshift"
    START_LOST = "start_lost"
    STOP_LOST = "stop_lost"
    SPLICE_SITE = "splice_site"
    UTR = "utr"
    INTERGENIC = "intergenic"


@dataclass
class ImpactPrediction:
    """Predicción de impacto de una variante."""

    position: int
    refCodon: str
    altCodon: str
    refAA: str
    altAA: str
    effect: VariantEffect
    severity: ImpactSeverity
    score: float  # 0 = benigno, 1 = dañino
    conservation: Optional[float] = None

    def to_dict(self) -> dict:
        return {
            "position": self.position,
            "refCodon": self.refCodon,
            "altCodon": self.altCodon,
            "refAA": self.refAA,
            "altAA": self.altAA,
            "aaChange": f"{self.refAA}{self.position}{self.altAA}",
            "effect": self.effect.value,
            "severity": self.severity.value,
            "score": round(self.score, 3),
            "conservation": self.conservation,
        }


class MutationImpactPredictor(LocalModel):
    """
    Predictor de impacto de mutaciones.

    Clasifica variantes por su efecto (sinónima, missense, etc.)
    y predice severidad del impacto funcional.
    """

    name = "mutation_impact"
    version = "1.0.0"
    category = ModelCategory.FUNCTIONAL
    description = (
        "Predice el impacto funcional de variantes: tipo de efecto "
        "(missense, nonsense, etc.) y severidad (benigno a dañino)."
    )

    # Código genético estándar
    CODON_TABLE = {
        "TTT": "F", "TTC": "F", "TTA": "L", "TTG": "L",
        "CTT": "L", "CTC": "L", "CTA": "L", "CTG": "L",
        "ATT": "I", "ATC": "I", "ATA": "I", "ATG": "M",
        "GTT": "V", "GTC": "V", "GTA": "V", "GTG": "V",
        "TCT": "S", "TCC": "S", "TCA": "S", "TCG": "S",
        "CCT": "P", "CCC": "P", "CCA": "P", "CCG": "P",
        "ACT": "T", "ACC": "T", "ACA": "T", "ACG": "T",
        "GCT": "A", "GCC": "A", "GCA": "A", "GCG": "A",
        "TAT": "Y", "TAC": "Y", "TAA": "*", "TAG": "*",
        "CAT": "H", "CAC": "H", "CAA": "Q", "CAG": "Q",
        "AAT": "N", "AAC": "N", "AAA": "K", "AAG": "K",
        "GAT": "D", "GAC": "D", "GAA": "E", "GAG": "E",
        "TGT": "C", "TGC": "C", "TGA": "*", "TGG": "W",
        "CGT": "R", "CGC": "R", "CGA": "R", "CGG": "R",
        "AGT": "S", "AGC": "S", "AGA": "R", "AGG": "R",
        "GGT": "G", "GGC": "G", "GGA": "G", "GGG": "G",
    }

    # Propiedades de aminoácidos para scoring
    AA_PROPERTIES = {
        "A": {"hydrophobic": True, "charge": 0, "size": "small"},
        "R": {"hydrophobic": False, "charge": 1, "size": "large"},
        "N": {"hydrophobic": False, "charge": 0, "size": "medium"},
        "D": {"hydrophobic": False, "charge": -1, "size": "medium"},
        "C": {"hydrophobic": True, "charge": 0, "size": "small"},
        "E": {"hydrophobic": False, "charge": -1, "size": "medium"},
        "Q": {"hydrophobic": False, "charge": 0, "size": "medium"},
        "G": {"hydrophobic": True, "charge": 0, "size": "small"},
        "H": {"hydrophobic": False, "charge": 0, "size": "medium"},
        "I": {"hydrophobic": True, "charge": 0, "size": "large"},
        "L": {"hydrophobic": True, "charge": 0, "size": "large"},
        "K": {"hydrophobic": False, "charge": 1, "size": "large"},
        "M": {"hydrophobic": True, "charge": 0, "size": "large"},
        "F": {"hydrophobic": True, "charge": 0, "size": "large"},
        "P": {"hydrophobic": True, "charge": 0, "size": "small"},
        "S": {"hydrophobic": False, "charge": 0, "size": "small"},
        "T": {"hydrophobic": False, "charge": 0, "size": "small"},
        "W": {"hydrophobic": True, "charge": 0, "size": "large"},
        "Y": {"hydrophobic": True, "charge": 0, "size": "large"},
        "V": {"hydrophobic": True, "charge": 0, "size": "medium"},
        "*": {"hydrophobic": False, "charge": 0, "size": "none"},
    }

    def __init__(self):
        self._is_loaded = False

    def load(self, model_path: Optional[Path] = None) -> None:
        self._is_loaded = True

    def unload(self) -> None:
        self._is_loaded = False

    def is_loaded(self) -> bool:
        return self._is_loaded

    async def predict(
        self,
        cds_sequence: str,
        position: int,  # 1-based position in CDS
        ref_base: str,
        alt_base: str,
    ) -> PredictionResult:
        """
        Predecir impacto de una mutación.

        Args:
            cds_sequence: Secuencia codificante (CDS)
            position: Posición de la mutación (1-based)
            ref_base: Base de referencia
            alt_base: Base alternativa

        Returns:
            PredictionResult con predicción de impacto
        """
        start_time = time.perf_counter()

        cds = cds_sequence.upper()
        pos = position - 1  # Convert to 0-based

        if pos < 0 or pos >= len(cds):
            return PredictionResult(
                modelName=self.name,
                modelVersion=self.version,
                prediction={"error": "Position out of range"},
                confidence=0.0,
            )

        # Encontrar codón afectado
        codon_start = (pos // 3) * 3
        codon_pos = pos % 3

        ref_codon = cds[codon_start : codon_start + 3]
        if len(ref_codon) < 3:
            return PredictionResult(
                modelName=self.name,
                modelVersion=self.version,
                prediction={"error": "Incomplete codon"},
                confidence=0.0,
            )

        # Crear codón alternativo
        alt_codon = list(ref_codon)
        alt_codon[codon_pos] = alt_base.upper()
        alt_codon = "".join(alt_codon)

        # Traducir
        ref_aa = self.CODON_TABLE.get(ref_codon, "X")
        alt_aa = self.CODON_TABLE.get(alt_codon, "X")

        # Determinar efecto
        effect, severity, score = self._classify_effect(ref_aa, alt_aa, codon_start)

        aa_position = codon_start // 3 + 1

        impact = ImpactPrediction(
            position=aa_position,
            refCodon=ref_codon,
            altCodon=alt_codon,
            refAA=ref_aa,
            altAA=alt_aa,
            effect=effect,
            severity=severity,
            score=score,
        )

        processing_time = (time.perf_counter() - start_time) * 1000

        return PredictionResult(
            modelName=self.name,
            modelVersion=self.version,
            prediction=impact.to_dict(),
            confidence=0.85,
            processingTimeMs=round(processing_time, 2),
            metadata={
                "cdsLength": len(cds),
                "mutationType": f"{ref_base}>{alt_base}",
                "codonPosition": codon_pos + 1,
            },
        )

    def _classify_effect(
        self, ref_aa: str, alt_aa: str, codon_start: int
    ) -> tuple[VariantEffect, ImpactSeverity, float]:
        """Clasificar efecto y severidad de la mutación."""
        # Sinónima
        if ref_aa == alt_aa:
            return VariantEffect.SYNONYMOUS, ImpactSeverity.BENIGN, 0.0

        # Nonsense (stop ganado)
        if alt_aa == "*" and ref_aa != "*":
            return VariantEffect.NONSENSE, ImpactSeverity.DAMAGING, 1.0

        # Start perdido
        if codon_start == 0 and ref_aa == "M":
            return VariantEffect.START_LOST, ImpactSeverity.DAMAGING, 1.0

        # Stop perdido
        if ref_aa == "*" and alt_aa != "*":
            return VariantEffect.STOP_LOST, ImpactSeverity.PROBABLY_DAMAGING, 0.8

        # Missense - calcular score basado en propiedades
        score = self._calculate_missense_score(ref_aa, alt_aa)
        severity = self._scoREDACTED(score)

        return VariantEffect.MISSENSE, severity, score

    def _calculate_missense_score(self, ref_aa: str, alt_aa: str) -> float:
        """Calcular score de daño para missense."""
        ref_props = self.AA_PROPERTIES.get(ref_aa, {})
        alt_props = self.AA_PROPERTIES.get(alt_aa, {})

        score = 0.0

        # Cambio de hidrofobicidad
        if ref_props.get("hydrophobic") != alt_props.get("hydrophobic"):
            score += 0.3

        # Cambio de carga
        if ref_props.get("charge") != alt_props.get("charge"):
            score += 0.4

        # Cambio de tamaño
        if ref_props.get("size") != alt_props.get("size"):
            score += 0.2

        # Cambios específicos de alto impacto
        high_impact_changes = [
            ("P", "any"),  # Prolina rompe estructuras
            ("G", "any"),  # Glicina muy flexible
            ("C", "any"),  # Cisteína forma puentes disulfuro
        ]

        for aa, target in high_impact_changes:
            if ref_aa == aa or alt_aa == aa:
                score += 0.1

        return min(score, 1.0)

    def _scoREDACTED(self, score: float) -> ImpactSeverity:
        """Convertir score a categoría de severidad."""
        if score < 0.2:
            return ImpactSeverity.BENIGN
        elif score < 0.4:
            return ImpactSeverity.TOLERATED
        elif score < 0.6:
            return ImpactSeverity.UNCERTAIN
        elif score < 0.8:
            return ImpactSeverity.POSSIBLY_DAMAGING
        else:
            return ImpactSeverity.PROBABLY_DAMAGING
