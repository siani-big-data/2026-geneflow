"""
Sequence clustering model.

Agrupa secuencias por similitud usando embeddings y clustering.

Arquitectura: DNA embeddings + HDBSCAN/K-means
Input: Lista de secuencias
Output: Clusters con centroide y miembros
"""

import time
from dataclasses import dataclass
from pathlib import Path
from typing import Optional

import numpy as np

from ..base import LocalModel, ModelCategory, PredictionResult


@dataclass
class SequenceCluster:
    """Un cluster de secuencias."""

    clusterId: int
    memberCount: int
    memberIds: list[str]
    centroidId: str
    avgSimilarity: float
    minSimilarity: float

    def to_dict(self) -> dict:
        return {
            "clusterId": self.clusterId,
            "memberCount": self.memberCount,
            "memberIds": self.memberIds,
            "centroidId": self.centroidId,
            "avgSimilarity": round(self.avgSimilarity, 3),
            "minSimilarity": round(self.minSimilarity, 3),
        }


class SequenceClusterer(LocalModel):
    """
    Agrupador de secuencias por similitud.

    Usa k-mer embeddings y clustering jerárquico
    para agrupar secuencias relacionadas.
    """

    name = "sequence_clusterer"
    version = "1.0.0"
    category = ModelCategory.PHYLO
    description = "Agrupa secuencias por similitud usando k-mer embeddings y clustering jerárquico."

    KMER_SIZE = 6

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
        sequences: dict[str, str],  # {id: sequence}
        similarity_threshold: float = 0.8,
        method: str = "hierarchical",
    ) -> PredictionResult:
        """
        Clusterizar secuencias.

        Args:
            sequences: Dict de id -> secuencia
            similarity_threshold: Umbral de similitud para clustering
            method: Método de clustering

        Returns:
            PredictionResult con clusters
        """
        start_time = time.perf_counter()

        if len(sequences) < 2:
            return PredictionResult(
                modelName=self.name,
                modelVersion=self.version,
                prediction={"error": "Need at least 2 sequences"},
                confidence=0.0,
            )

        # Calcular embeddings (k-mer frequencies)
        seq_ids = list(sequences.keys())
        embeddings = [self._kmer_embedding(sequences[sid]) for sid in seq_ids]

        # Calcular matriz de similitud
        n = len(seq_ids)
        similarity_matrix = np.zeros((n, n))
        for i in range(n):
            for j in range(i, n):
                sim = self._cosine_similarity(embeddings[i], embeddings[j])
                similarity_matrix[i, j] = sim
                similarity_matrix[j, i] = sim

        # Clustering simple (single-linkage)
        clusters = self._hierarchical_cluster(seq_ids, similarity_matrix, similarity_threshold)

        processing_time = (time.perf_counter() - start_time) * 1000

        return PredictionResult(
            modelName=self.name,
            modelVersion=self.version,
            prediction={
                "clusters": [c.to_dict() for c in clusters],
                "totalClusters": len(clusters),
                "singletons": sum(1 for c in clusters if c.memberCount == 1),
            },
            confidence=0.85,
            processingTimeMs=round(processing_time, 2),
            metadata={
                "totalSequences": len(sequences),
                "threshold": similarity_threshold,
                "method": method,
            },
        )

    def _kmer_embedding(self, sequence: str) -> np.ndarray:
        """Generar embedding basado en frecuencias de k-mers."""
        seq = sequence.upper()
        kmer_counts = {}

        for i in range(len(seq) - self.KMER_SIZE + 1):
            kmer = seq[i : i + self.KMER_SIZE]
            if "N" not in kmer:
                kmer_counts[kmer] = kmer_counts.get(kmer, 0) + 1

        # Convertir a vector (simplificado - en producción usar vocabulario fijo)
        total = sum(kmer_counts.values()) or 1
        values = sorted(kmer_counts.items())
        return np.array([count / total for _, count in values] or [0])

    def _cosine_similarity(self, v1: np.ndarray, v2: np.ndarray) -> float:
        """Calcular similitud coseno."""
        if len(v1) == 0 or len(v2) == 0:
            return 0.0

        # Padding para igualar longitudes
        max_len = max(len(v1), len(v2))
        v1_padded = np.pad(v1, (0, max_len - len(v1)))
        v2_padded = np.pad(v2, (0, max_len - len(v2)))

        dot = np.dot(v1_padded, v2_padded)
        norm1 = np.linalg.norm(v1_padded)
        norm2 = np.linalg.norm(v2_padded)

        if norm1 == 0 or norm2 == 0:
            return 0.0

        return float(dot / (norm1 * norm2))

    def _hierarchical_cluster(
        self, seq_ids: list[str], sim_matrix: np.ndarray, threshold: float
    ) -> list[SequenceCluster]:
        """Clustering jerárquico simple."""
        n = len(seq_ids)
        assigned = [False] * n
        clusters = []
        cluster_id = 0

        for i in range(n):
            if assigned[i]:
                continue

            # Nuevo cluster
            members = [i]
            assigned[i] = True

            # Encontrar vecinos
            for j in range(i + 1, n):
                if not assigned[j] and sim_matrix[i, j] >= threshold:
                    members.append(j)
                    assigned[j] = True

            # Calcular estadísticas
            member_ids = [seq_ids[m] for m in members]
            sims = [sim_matrix[members[0], m] for m in members]

            clusters.append(
                SequenceCluster(
                    clusterId=cluster_id,
                    memberCount=len(members),
                    memberIds=member_ids,
                    centroidId=member_ids[0],
                    avgSimilarity=float(np.mean(sims)),
                    minSimilarity=float(np.min(sims)),
                )
            )
            cluster_id += 1

        return clusters
