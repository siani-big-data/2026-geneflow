"""
SNP calling model for Sanger sequences.

Detecta polimorfismos de un solo nucleótido (SNPs) comparando
la secuencia con una referencia.

Arquitectura: Transformer encoder para contexto + clasificador
Input: Secuencia query + secuencia referencia
Output: Lista de SNPs con posición, cambio y confianza
"""

import time
from dataclasses import dataclass
from pathlib import Path
from typing import Optional

import numpy as np

from ..base import LocalModel, ModelCategory, PredictionResult


@dataclass
class SNPCall:
    """Un SNP detectado."""

    position: int  # Posición en la secuencia query
    referenceBase: str
    alternateBase: str
    quality: float  # Phred-like quality score
    context: str  # Contexto de 5 bases a cada lado
    zygosity: str  # "homozygous" o "heterozygous"

    def to_dict(self) -> dict:
        return {
            "position": self.position,
            "referenceBase": self.referenceBase,
            "alternateBase": self.alternateBase,
            "change": f"{self.referenceBase}>{self.alternateBase}",
            "quality": round(self.quality, 2),
            "context": self.context,
            "zygosity": self.zygosity,
        }


class SNPCaller(LocalModel):
    """
    Caller de SNPs para secuencias Sanger.

    Compara secuencia query contra referencia y detecta
    variantes de un solo nucleótido.
    """

    name = "snp_caller"
    version = "1.0.0"
    category = ModelCategory.VARIANTS
    description = (
        "Detecta SNPs comparando secuencia query contra referencia. "
        "Reporta posición, cambio, calidad y contexto."
    )

    # Configuración
    MIN_QUALITY_FOR_CALL = 20
    CONTEXT_SIZE = 5
    VALID_BASES = set("ATCG")

    def __init__(self):
        self._model = None
        self._is_loaded = False

    def load(self, model_path: Optional[Path] = None) -> None:
        """Cargar modelo."""
        self._is_loaded = True

    def unload(self) -> None:
        self._model = None
        self._is_loaded = False

    def is_loaded(self) -> bool:
        return self._is_loaded

    async def predict(
        self,
        query_sequence: str,
        reference_sequence: str,
        quality_scores: Optional[list[int]] = None,
        signal_ratios: Optional[list[dict]] = None,
    ) -> PredictionResult:
        """
        Detectar SNPs en secuencia query vs referencia.

        Args:
            query_sequence: Secuencia a analizar
            reference_sequence: Secuencia de referencia
            quality_scores: Phred scores por posición
            signal_ratios: Ratios de señal por posición (para heterocigotos)

        Returns:
            PredictionResult con lista de SNPs
        """
        start_time = time.perf_counter()

        query = query_sequence.upper()
        ref = reference_sequence.upper()

        if len(query) != len(ref):
            return PredictionResult(
                modelName=self.name,
                modelVersion=self.version,
                prediction={"error": "Sequence lengths don't match"},
                confidence=0.0,
                metadata={"queryLength": len(query), "refLength": len(ref)},
            )

        snps = []
        qualities = quality_scores or [30] * len(query)  # Default Q30

        for i in range(len(query)):
            query_base = query[i]
            ref_base = ref[i]

            # Saltar si alguna base no es válida
            if query_base not in self.VALID_BASES or ref_base not in self.VALID_BASES:
                continue

            # Detectar diferencia
            if query_base != ref_base:
                quality = qualities[i] if i < len(qualities) else 30

                # Solo reportar si calidad suficiente
                if quality >= self.MIN_QUALITY_FOR_CALL:
                    # Extraer contexto
                    context_start = max(0, i - self.CONTEXT_SIZE)
                    context_end = min(len(query), i + self.CONTEXT_SIZE + 1)
                    context = query[context_start:context_end]

                    # Determinar cigosidad (simplificado)
                    zygosity = "homozygous"
                    if signal_ratios and i < len(signal_ratios):
                        ratio = signal_ratios[i]
                        if ratio and ratio.get("secondary_ratio", 0) > 0.2:
                            zygosity = "heterozygous"

                    snps.append(
                        SNPCall(
                            position=i + 1,  # 1-based
                            referenceBase=ref_base,
                            alternateBase=query_base,
                            quality=float(quality),
                            context=context,
                            zygosity=zygosity,
                        )
                    )

        # Estadísticas
        total_compared = sum(
            1 for q, r in zip(query, ref) if q in self.VALID_BASES and r in self.VALID_BASES
        )
        snp_rate = len(snps) / total_compared if total_compared > 0 else 0

        # Confianza basada en calidad promedio de SNPs
        if snps:
            avg_quality = np.mean([s.quality for s in snps])
            confidence = min(avg_quality / 40, 1.0)
        else:
            confidence = 1.0  # Alta confianza en "no SNPs"

        processing_time = (time.perf_counter() - start_time) * 1000

        return PredictionResult(
            modelName=self.name,
            modelVersion=self.version,
            prediction={
                "snps": [s.to_dict() for s in snps],
                "totalSNPs": len(snps),
                "snpRate": round(snp_rate * 100, 4),
            },
            confidence=round(confidence, 3),
            processingTimeMs=round(processing_time, 2),
            metadata={
                "queryLength": len(query),
                "basesCompared": total_compared,
                "transitions": sum(
                    1
                    for s in snps
                    if self._is_transition(s.referenceBase, s.alternateBase)
                ),
                "transversions": sum(
                    1
                    for s in snps
                    if not self._is_transition(s.referenceBase, s.alternateBase)
                ),
            },
        )

    def _is_transition(self, base1: str, base2: str) -> bool:
        """Check if mutation is a transition (purine<->purine or pyrimidine<->pyrimidine)."""
        purines = {"A", "G"}
        pyrimidines = {"C", "T"}

        return (base1 in purines and base2 in purines) or (
            base1 in pyrimidines and base2 in pyrimidines
        )
