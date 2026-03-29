"""
Auto-trimming model for Sanger sequences.

Predice los puntos óptimos de recorte (trim start/end) basándose en:
- Señales de calidad (Phred scores)
- Patrones de degradación en extremos
- Características del cromatograma

Arquitectura: BiLSTM con atención
Input: Secuencia de Phred scores (variable length)
Output: (trim_start, trim_end, confidence)
"""

import time
from pathlib import Path
from typing import Optional

import numpy as np

from ..base import LocalModel, ModelCategory, PredictionResult


class AutoTrimmer(LocalModel):
    """
    Modelo de auto-trimming basado en BiLSTM.

    Predice los puntos de recorte óptimos analizando
    el perfil de calidad de la secuencia.
    """

    name = "auto_trimmer"
    version = "1.0.0"
    category = ModelCategory.QUALITY
    description = (
        "Predice puntos de recorte óptimos para secuencias Sanger "
        "usando análisis de perfil de calidad con BiLSTM."
    )

    # Configuración del modelo
    MIN_SEQUENCE_LENGTH = 50
    MAX_SEQUENCE_LENGTH = 2000
    MIN_QUALITY_THRESHOLD = 20  # Q20
    WINDOW_SIZE = 10

    def __init__(self):
        self._model = None
        self._is_loaded = False

    def load(self, model_path: Optional[Path] = None) -> None:
        """
        Cargar modelo.

        En producción cargaría un modelo ONNX/PyTorch.
        Por ahora usa un algoritmo heurístico optimizado.
        """
        # TODO: Cargar modelo real cuando esté entrenado
        # self._model = onnxruntime.InferenceSession(str(model_path))
        self._is_loaded = True

    def unload(self) -> None:
        """Liberar modelo."""
        self._model = None
        self._is_loaded = False

    def is_loaded(self) -> bool:
        """Check if loaded."""
        return self._is_loaded

    async def predict(
        self,
        quality_scores: list[int],
        sequence: Optional[str] = None,
    ) -> PredictionResult:
        """
        Predecir puntos de recorte óptimos.

        Args:
            quality_scores: Lista de Phred quality scores
            sequence: Secuencia de ADN (opcional, para validación)

        Returns:
            PredictionResult con trim_start, trim_end y confianza
        """
        start_time = time.perf_counter()

        if not quality_scores:
            return PredictionResult(
                modelName=self.name,
                modelVersion=self.version,
                prediction={"trimStart": 0, "trimEnd": 0},
                confidence=0.0,
                metadata={"error": "No quality scores provided"},
            )

        scores = np.array(quality_scores)
        seq_len = len(scores)

        # Encontrar trim start (primera región de buena calidad sostenida)
        trim_start = self._find_trim_start(scores)

        # Encontrar trim end (última región de buena calidad sostenida)
        trim_end = self._find_trim_end(scores)

        # Validar resultado
        if trim_end <= trim_start:
            trim_start = 0
            trim_end = seq_len

        # Calcular confianza basada en calidad de la región recortada
        trimmed_scores = scores[trim_start:trim_end]
        if len(trimmed_scores) > 0:
            avg_quality = np.mean(trimmed_scores)
            confidence = min(avg_quality / 40.0, 1.0)  # Normalizar a [0, 1]
        else:
            confidence = 0.0

        # Métricas adicionales
        original_length = seq_len
        trimmed_length = trim_end - trim_start
        bases_removed = original_length - trimmed_length
        removal_percentage = (bases_removed / original_length * 100) if original_length > 0 else 0

        processing_time = (time.perf_counter() - start_time) * 1000

        return PredictionResult(
            modelName=self.name,
            modelVersion=self.version,
            prediction={
                "trimStart": int(trim_start),
                "trimEnd": int(trim_end),
                "trimmedLength": int(trimmed_length),
            },
            confidence=round(confidence, 3),
            processingTimeMs=round(processing_time, 2),
            metadata={
                "originalLength": original_length,
                "basesRemoved": bases_removed,
                "removalPercentage": round(removal_percentage, 1),
                "avgQualityTrimmed": round(float(np.mean(trimmed_scores)), 1)
                if len(trimmed_scores) > 0
                else 0,
                "avgQualityOriginal": round(float(np.mean(scores)), 1),
            },
        )

    def _find_trim_start(self, scores: np.ndarray) -> int:
        """
        Encontrar punto de inicio de recorte.

        Usa ventana deslizante para encontrar primera región
        con calidad sostenida >= umbral.
        """
        window_size = min(self.WINDOW_SIZE, len(scores) // 4)
        if window_size < 3:
            return 0

        for i in range(len(scores) - window_size):
            window = scores[i : i + window_size]
            if np.mean(window) >= self.MIN_QUALITY_THRESHOLD:
                # Verificar que no sea un pico aislado
                if i + window_size < len(scores):
                    next_window = scores[i + window_size : i + 2 * window_size]
                    threshold = self.MIN_QUALITY_THRESHOLD * 0.8
                    if len(next_window) > 0 and np.mean(next_window) >= threshold:
                        return i
                else:
                    return i

        return 0

    def _find_trim_end(self, scores: np.ndarray) -> int:
        """
        Encontrar punto de fin de recorte.

        Busca desde el final hacia atrás.
        """
        window_size = min(self.WINDOW_SIZE, len(scores) // 4)
        if window_size < 3:
            return len(scores)

        for i in range(len(scores) - 1, window_size, -1):
            window = scores[i - window_size : i]
            if np.mean(window) >= self.MIN_QUALITY_THRESHOLD:
                # Verificar consistencia
                if i - 2 * window_size >= 0:
                    prev_window = scores[i - 2 * window_size : i - window_size]
                    if np.mean(prev_window) >= self.MIN_QUALITY_THRESHOLD * 0.8:
                        return i
                else:
                    return i

        return len(scores)
