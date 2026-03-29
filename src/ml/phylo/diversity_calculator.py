"""
Genetic diversity calculator.

Calcula métricas de diversidad genética para conjuntos de secuencias.

Métricas: Pi (nucleotide diversity), Theta, Tajima's D, etc.
"""

import time
from pathlib import Path
from typing import Optional

import numpy as np

from ..base import LocalModel, ModelCategory, PredictionResult


class DiversityCalculator(LocalModel):
    """
    Calculador de métricas de diversidad genética.

    Calcula Pi, Theta, y otras métricas de diversidad
    poblacional a partir de un conjunto de secuencias.
    """

    name = "diversity_calculator"
    version = "1.0.0"
    category = ModelCategory.PHYLO
    description = (
        "Calcula métricas de diversidad genética: Pi, Theta, "
        "número de sitios segregantes, etc."
    )

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
        sequences: list[str],
    ) -> PredictionResult:
        """
        Calcular métricas de diversidad.

        Args:
            sequences: Lista de secuencias alineadas (misma longitud)

        Returns:
            PredictionResult con métricas de diversidad
        """
        start_time = time.perf_counter()

        if len(sequences) < 2:
            return PredictionResult(
                modelName=self.name,
                modelVersion=self.version,
                prediction={"error": "Need at least 2 sequences"},
                confidence=0.0,
            )

        # Verificar longitudes iguales
        lengths = set(len(s) for s in sequences)
        if len(lengths) > 1:
            return PredictionResult(
                modelName=self.name,
                modelVersion=self.version,
                prediction={"error": "Sequences must be aligned (same length)"},
                confidence=0.0,
            )

        seqs = [s.upper() for s in sequences]
        n = len(seqs)
        L = len(seqs[0])

        # Sitios segregantes (S)
        segregating_sites = self._count_segregating_sites(seqs)

        # Nucleotide diversity (Pi)
        pi = self._calculate_pi(seqs)

        # Watterson's Theta
        theta_w = self._calculate_theta_w(segregating_sites, n, L)

        # Tajima's D (simplificado)
        tajimas_d = self._calculate_tajimas_d(pi, theta_w, n, segregating_sites)

        # Haplotype diversity
        haplotype_div = self._calculate_haplotype_diversity(seqs)

        processing_time = (time.perf_counter() - start_time) * 1000

        return PredictionResult(
            modelName=self.name,
            modelVersion=self.version,
            prediction={
                "nucleotideDiversity": round(pi, 6),
                "thetaW": round(theta_w, 6),
                "tajimasD": round(tajimas_d, 4),
                "segregatingSites": segregating_sites,
                "haplotypeDiversity": round(haplotype_div, 4),
            },
            confidence=0.95,
            processingTimeMs=round(processing_time, 2),
            metadata={
                "nSequences": n,
                "alignmentLength": L,
                "nHaplotypes": len(set(seqs)),
            },
        )

    def _count_segregating_sites(self, seqs: list[str]) -> int:
        """Contar sitios segregantes (polimórficos)."""
        L = len(seqs[0])
        S = 0

        for i in range(L):
            bases = set(s[i] for s in seqs if s[i] in "ATCG")
            if len(bases) > 1:
                S += 1

        return S

    def _calculate_pi(self, seqs: list[str]) -> float:
        """Calcular diversidad nucleotídica (Pi)."""
        n = len(seqs)
        L = len(seqs[0])

        if n < 2 or L == 0:
            return 0.0

        total_diff = 0
        comparisons = 0

        for i in range(n):
            for j in range(i + 1, n):
                diff = sum(
                    1
                    for k in range(L)
                    if seqs[i][k] != seqs[j][k]
                    and seqs[i][k] in "ATCG"
                    and seqs[j][k] in "ATCG"
                )
                total_diff += diff
                comparisons += 1

        if comparisons == 0:
            return 0.0

        return total_diff / (comparisons * L)

    def _calculate_theta_w(self, S: int, n: int, L: int) -> float:
        """Calcular Watterson's Theta."""
        if n < 2 or L == 0:
            return 0.0

        # Harmonic number a1
        a1 = sum(1 / i for i in range(1, n))

        return S / (a1 * L)

    def _calculate_tajimas_d(
        self, pi: float, theta_w: float, n: int, S: int
    ) -> float:
        """Calcular Tajima's D (simplificado)."""
        if S == 0 or theta_w == 0:
            return 0.0

        # Coeficientes simplificados
        a1 = sum(1 / i for i in range(1, n))
        a2 = sum(1 / (i * i) for i in range(1, n))

        b1 = (n + 1) / (3 * (n - 1))
        b2 = 2 * (n * n + n + 3) / (9 * n * (n - 1))

        c1 = b1 - 1 / a1
        c2 = b2 - (n + 2) / (a1 * n) + a2 / (a1 * a1)

        e1 = c1 / a1
        e2 = c2 / (a1 * a1 + a2)

        variance = e1 * S + e2 * S * (S - 1)

        if variance <= 0:
            return 0.0

        d = (pi - theta_w) / np.sqrt(variance)
        return float(d)

    def _calculate_haplotype_diversity(self, seqs: list[str]) -> float:
        """Calcular diversidad haplotípica."""
        n = len(seqs)
        if n < 2:
            return 0.0

        # Contar frecuencias de haplotipos
        haplotype_counts = {}
        for seq in seqs:
            haplotype_counts[seq] = haplotype_counts.get(seq, 0) + 1

        # Fórmula de diversidad haplotípica
        sum_pi = sum((count / n) ** 2 for count in haplotype_counts.values())

        return (n / (n - 1)) * (1 - sum_pi)
