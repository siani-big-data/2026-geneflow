"""
RNA secondary structure prediction.

Predice estructura secundaria de ARN usando algoritmo de Nussinov
simplificado (en producción usar ViennaRNA).

Output: Estructura en notación dot-bracket y energía libre.
"""

import time
from dataclasses import dataclass
from pathlib import Path
from typing import Optional

import numpy as np

from ..base import LocalModel, ModelCategory, PredictionResult


@dataclass
class RNAStructure:
    """Estructura secundaria de ARN predicha."""

    sequence: str
    structure: str  # Notación dot-bracket
    energy: float  # Energía libre (kcal/mol)
    basePairs: list[tuple[int, int]]
    stemLoops: int

    def to_dict(self) -> dict:
        return {
            "sequence": self.sequence,
            "structure": self.structure,
            "energy": round(self.energy, 2),
            "basePairs": self.basePairs,
            "stemLoops": self.stemLoops,
            "length": len(self.sequence),
            "pairedBases": len(self.basePairs) * 2,
        }


class RNAStructurePredictor(LocalModel):
    """
    Predictor de estructura secundaria de ARN.

    Usa algoritmo de Nussinov simplificado para predecir
    apareamientos de bases y estructura secundaria.
    """

    name = "rna_structure"
    version = "1.0.0"
    category = ModelCategory.FUNCTIONAL
    description = (
        "Predice estructura secundaria de ARN en notación dot-bracket "
        "usando algoritmo de plegamiento."
    )

    # Pares de bases permitidos (Watson-Crick + wobble)
    VALID_PAIRS = {
        ("A", "U"),
        ("U", "A"),
        ("G", "C"),
        ("C", "G"),
        ("G", "U"),
        ("U", "G"),  # Wobble
    }

    # Energías de apareamiento (simplificadas, kcal/mol)
    PAIR_ENERGIES = {
        ("A", "U"): -2.0,
        ("U", "A"): -2.0,
        ("G", "C"): -3.0,
        ("C", "G"): -3.0,
        ("G", "U"): -1.0,
        ("U", "G"): -1.0,
    }

    MIN_LOOP_SIZE = 3  # Mínimo 3 bases en loop

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
        sequence: str,
    ) -> PredictionResult:
        """
        Predecir estructura secundaria de ARN.

        Args:
            sequence: Secuencia de ARN (o ADN, se convertirá)

        Returns:
            PredictionResult con estructura predicha
        """
        start_time = time.perf_counter()

        # Convertir a ARN
        rna = sequence.upper().replace("T", "U")

        if len(rna) < 5:
            return PredictionResult(
                modelName=self.name,
                modelVersion=self.version,
                prediction={"error": "Sequence too short (min 5 bases)"},
                confidence=0.0,
            )

        if len(rna) > 500:
            return PredictionResult(
                modelName=self.name,
                modelVersion=self.version,
                prediction={"error": "Sequence too long (max 500 bases)"},
                confidence=0.0,
            )

        # Ejecutar Nussinov
        pairs, energy = self._nussinov(rna)

        # Convertir a dot-bracket
        structure = self._to_dot_bracket(len(rna), pairs)

        # Contar stem-loops
        stem_loops = self._count_stem_loops(structure)

        result = RNAStructure(
            sequence=rna,
            structure=structure,
            energy=energy,
            basePairs=pairs,
            stemLoops=stem_loops,
        )

        processing_time = (time.perf_counter() - start_time) * 1000

        return PredictionResult(
            modelName=self.name,
            modelVersion=self.version,
            prediction=result.to_dict(),
            confidence=0.7,  # Nussinov es simplificado
            processingTimeMs=round(processing_time, 2),
            metadata={
                "algorithm": "nussinov_simplified",
                "minLoopSize": self.MIN_LOOP_SIZE,
            },
        )

    def _nussinov(self, seq: str) -> tuple[list[tuple[int, int]], float]:
        """Algoritmo de Nussinov para estructura óptima."""
        n = len(seq)
        dp = np.zeros((n, n), dtype=int)
        traceback = [[None for _ in range(n)] for _ in range(n)]

        # Llenar matriz DP
        for length in range(self.MIN_LOOP_SIZE + 1, n):
            for i in range(n - length):
                j = i + length

                # Opción 1: i no apareado
                dp[i][j] = dp[i + 1][j]
                traceback[i][j] = ("skip_i", i + 1, j)

                # Opción 2: j no apareado
                if dp[i][j - 1] > dp[i][j]:
                    dp[i][j] = dp[i][j - 1]
                    traceback[i][j] = ("skip_j", i, j - 1)

                # Opción 3: i-j apareados
                if self._can_pair(seq[i], seq[j]):
                    score = 1 + dp[i + 1][j - 1]
                    if score > dp[i][j]:
                        dp[i][j] = score
                        traceback[i][j] = ("pair", i + 1, j - 1)

                # Opción 4: bifurcación
                for k in range(i + 1, j):
                    score = dp[i][k] + dp[k + 1][j]
                    if score > dp[i][j]:
                        dp[i][j] = score
                        traceback[i][j] = ("bifurc", k)

        # Traceback para obtener pares
        pairs = []
        self._traceback(traceback, seq, 0, n - 1, pairs)

        # Calcular energía
        energy = sum(self.PAIR_ENERGIES.get((seq[i], seq[j]), -1.0) for i, j in pairs)

        return pairs, energy

    def _traceback(
        self,
        tb: list,
        seq: str,
        i: int,
        j: int,
        pairs: list[tuple[int, int]],
    ) -> None:
        """Traceback recursivo para obtener pares."""
        if i >= j:
            return

        action = tb[i][j]
        if action is None:
            return

        if action[0] == "skip_i":
            self._traceback(tb, seq, action[1], action[2], pairs)
        elif action[0] == "skip_j":
            self._traceback(tb, seq, action[1], action[2], pairs)
        elif action[0] == "pair":
            pairs.append((i, j))
            self._traceback(tb, seq, action[1], action[2], pairs)
        elif action[0] == "bifurc":
            k = action[1]
            self._traceback(tb, seq, i, k, pairs)
            self._traceback(tb, seq, k + 1, j, pairs)

    def _can_pair(self, base1: str, base2: str) -> bool:
        """Verificar si dos bases pueden aparearse."""
        return (base1, base2) in self.VALID_PAIRS

    def _to_dot_bracket(self, length: int, pairs: list[tuple[int, int]]) -> str:
        """Convertir pares a notación dot-bracket."""
        structure = ["."] * length

        for i, j in pairs:
            structure[i] = "("
            structure[j] = ")"

        return "".join(structure)

    def _count_stem_loops(self, structure: str) -> int:
        """Contar stem-loops en la estructura."""
        # Buscar patrones de apertura seguidos de cierre
        count = 0
        i = 0
        while i < len(structure):
            if structure[i] == "(":
                # Encontrar el loop correspondiente
                depth = 1
                j = i + 1
                while j < len(structure) and depth > 0:
                    if structure[j] == "(":
                        depth += 1
                    elif structure[j] == ")":
                        depth -= 1
                    j += 1
                # Verificar si es un stem-loop simple
                inner = structure[i + 1 : j - 1]
                if "(" not in inner and ")" not in inner and "." in inner:
                    count += 1
            i += 1

        return count
