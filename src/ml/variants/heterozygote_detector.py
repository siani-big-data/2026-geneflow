"""
Heterozygote detection model for Sanger sequences.

Detecta posiciones heterocigotas analizando patrones de doble pico
en el cromatograma.

Arquitectura: CNN 1D sobre señales de 4 canales
Input: Señales de cromatograma (A, T, C, G)
Output: Lista de posiciones heterocigotas con alelos
"""

import time
from dataclasses import dataclass
from pathlib import Path
from typing import Optional

import numpy as np

from ..base import LocalModel, ModelCategory, PredictionResult


@dataclass
class HeterozygoteCall:
    """Una posición heterocigota detectada."""

    position: int
    allele1: str
    allele2: str
    ratio: float  # Ratio allele2/allele1 (idealmente ~0.5 para het)
    quality: float
    peakSeparation: float  # Separación entre picos

    def to_dict(self) -> dict:
        return {
            "position": self.position,
            "allele1": self.allele1,
            "allele2": self.allele2,
            "genotype": f"{self.allele1}/{self.allele2}",
            "ratio": round(self.ratio, 3),
            "quality": round(self.quality, 2),
            "peakSeparation": round(self.peakSeparation, 2),
        }


class HeterozygoteDetector(LocalModel):
    """
    Detector de posiciones heterocigotas.

    Analiza señales de cromatograma para identificar
    posiciones con doble pico (dos alelos).
    """

    name = "heterozygote_detector"
    version = "1.0.0"
    category = ModelCategory.VARIANTS
    description = (
        "Detecta posiciones heterocigotas analizando patrones de "
        "doble pico en cromatogramas Sanger."
    )

    # Configuración
    MIN_SECONDARY_RATIO = 0.25  # Ratio mínimo del segundo pico
    MAX_SECONDARY_RATIO = 0.75  # Ratio máximo (si es mayor, intercambiar)
    MIN_PEAK_HEIGHT = 100  # Altura mínima de pico
    IUPAC_CODES = {
        frozenset(["A", "G"]): "R",
        frozenset(["C", "T"]): "Y",
        frozenset(["A", "C"]): "M",
        frozenset(["G", "T"]): "K",
        frozenset(["A", "T"]): "W",
        frozenset(["C", "G"]): "S",
    }

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
        signal_a: list[float],
        signal_t: list[float],
        signal_c: list[float],
        signal_g: list[float],
        base_calls: Optional[str] = None,
        quality_scores: Optional[list[int]] = None,
    ) -> PredictionResult:
        """
        Detectar posiciones heterocigotas.

        Args:
            signal_a/t/c/g: Señales de cada canal
            base_calls: Secuencia de bases llamadas
            quality_scores: Phred scores

        Returns:
            PredictionResult con posiciones heterocigotas
        """
        start_time = time.perf_counter()

        signals = {
            "A": np.array(signal_a),
            "T": np.array(signal_t),
            "C": np.array(signal_c),
            "G": np.array(signal_g),
        }

        # Verificar longitudes
        lengths = [len(s) for s in signals.values()]
        if len(set(lengths)) > 1:
            return PredictionResult(
                modelName=self.name,
                modelVersion=self.version,
                prediction={"error": "Signal lengths don't match"},
                confidence=0.0,
            )

        seq_length = lengths[0]
        heterozygotes = []

        # Analizar cada posición
        for pos in range(seq_length):
            result = self._analyze_position(signals, pos)
            if result:
                heterozygotes.append(result)

        # Estadísticas
        het_rate = len(heterozygotes) / seq_length if seq_length > 0 else 0

        # Confianza basada en calidad de las llamadas
        if heterozygotes:
            avg_quality = np.mean([h.quality for h in heterozygotes])
            confidence = min(avg_quality / 40, 1.0)
        else:
            confidence = 1.0

        processing_time = (time.perf_counter() - start_time) * 1000

        return PredictionResult(
            modelName=self.name,
            modelVersion=self.version,
            prediction={
                "heterozygotes": [h.to_dict() for h in heterozygotes],
                "totalHeterozygotes": len(heterozygotes),
                "heterozygoteRate": round(het_rate * 100, 4),
            },
            confidence=round(confidence, 3),
            processingTimeMs=round(processing_time, 2),
            metadata={
                "sequenceLength": seq_length,
                "avgRatio": round(np.mean([h.ratio for h in heterozygotes]), 3)
                if heterozygotes
                else 0,
                "genotypeCounts": self._count_genotypes(heterozygotes),
            },
        )

    def _analyze_position(
        self, signals: dict[str, np.ndarray], pos: int
    ) -> Optional[HeterozygoteCall]:
        """Analizar una posición para heterocigosidad."""
        # Obtener alturas de señal en esta posición
        heights = {base: signals[base][pos] for base in signals}

        # Ordenar por altura
        sorted_bases = sorted(heights.items(), key=lambda x: x[1], reverse=True)

        primary_base, primary_height = sorted_bases[0]
        secondary_base, secondary_height = sorted_bases[1]

        # Verificar altura mínima
        if primary_height < self.MIN_PEAK_HEIGHT:
            return None

        # Calcular ratio
        ratio = secondary_height / primary_height if primary_height > 0 else 0

        # Verificar si es heterocigoto
        if not (self.MIN_SECONDARY_RATIO <= ratio <= self.MAX_SECONDARY_RATIO):
            return None

        # Verificar que el tercer pico sea significativamente menor
        third_height = sorted_bases[2][1] if len(sorted_bases) > 2 else 0
        if third_height > secondary_height * 0.5:
            return None  # Demasiado ruido

        # Calcular calidad estimada
        noise_ratio = third_height / primary_height if primary_height > 0 else 1
        quality = 40 * (1 - noise_ratio) * min(ratio / 0.5, 1.0)

        # Calcular separación de picos (simplificado)
        peak_separation = abs(ratio - 0.5) * 2  # 0 = perfecto het, 1 = muy sesgado

        return HeterozygoteCall(
            position=pos + 1,  # 1-based
            allele1=primary_base,
            allele2=secondary_base,
            ratio=ratio,
            quality=quality,
            peakSeparation=peak_separation,
        )

    def _count_genotypes(self, heterozygotes: list[HeterozygoteCall]) -> dict:
        """Contar genotipos por tipo."""
        counts = {}
        for h in heterozygotes:
            # Normalizar orden (A antes de T, etc.)
            bases = sorted([h.allele1, h.allele2])
            normalized = f"{bases[0]}/{bases[1]}"
            counts[normalized] = counts.get(normalized, 0) + 1
        return counts

    def get_iupac_code(self, allele1: str, allele2: str) -> str:
        """Obtener código IUPAC para heterocigoto."""
        key = frozenset([allele1, allele2])
        return self.IUPAC_CODES.get(key, "N")
