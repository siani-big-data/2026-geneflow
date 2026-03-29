"""
Artifact detection model for Sanger chromatograms.

Detecta artefactos comunes en cromatogramas:
- Dye blobs: Manchas de fluoróforo no incorporado
- Pull-ups: Bleeding de señal entre canales
- Spikes: Picos anómalos de ruido
- Shoulders: Picos con hombros (posible heterocigoto o artefacto)

Arquitectura: CNN 1D multi-canal
Input: 4 canales de señal (A, T, C, G) + calidad
Output: Lista de artefactos con posición y tipo
"""

import time
from dataclasses import dataclass
from enum import Enum
from pathlib import Path
from typing import Optional

import numpy as np

from ..base import LocalModel, ModelCategory, PredictionResult


class ArtifactType(str, Enum):
    """Tipos de artefactos detectables."""

    DYE_BLOB = "dye_blob"
    PULL_UP = "pull_up"
    SPIKE = "spike"
    SHOULDER = "shoulder"
    BASELINE_DRIFT = "baseline_drift"
    LOW_SIGNAL = "low_signal"


@dataclass
class DetectedArtifact:
    """Un artefacto detectado."""

    type: ArtifactType
    position: int
    length: int
    severity: float  # 0-1
    channel: Optional[str] = None  # A, T, C, G or None for all

    def to_dict(self) -> dict:
        return {
            "type": self.type.value,
            "position": self.position,
            "length": self.length,
            "severity": round(self.severity, 3),
            "channel": self.channel,
        }


class ArtifactDetector(LocalModel):
    """
    Detector de artefactos en cromatogramas Sanger.

    Analiza las señales crudas de los 4 canales para
    identificar artefactos que afectan la calidad.
    """

    name = "artifact_detector"
    version = "1.0.0"
    category = ModelCategory.QUALITY
    description = (
        "Detecta artefactos en cromatogramas Sanger: dye blobs, "
        "pull-ups, spikes y otros problemas de señal."
    )

    # Umbrales de detección
    SPIKE_THRESHOLD = 3.0  # Desviaciones estándar
    PULL_UP_RATIO = 0.3  # Ratio de bleeding entre canales
    DYE_BLOB_WIDTH = 50  # Ancho típico de dye blob en puntos
    MIN_SIGNAL_RATIO = 0.1  # Señal mínima vs máximo

    def __init__(self):
        self._model = None
        self._is_loaded = False

    def load(self, model_path: Optional[Path] = None) -> None:
        """Cargar modelo."""
        # TODO: Cargar modelo CNN entrenado
        self._is_loaded = True

    def unload(self) -> None:
        """Liberar modelo."""
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
        quality_scores: Optional[list[int]] = None,
    ) -> PredictionResult:
        """
        Detectar artefactos en señales de cromatograma.

        Args:
            signal_a: Señal del canal A (adenina)
            signal_t: Señal del canal T (timina)
            signal_c: Señal del canal C (citosina)
            signal_g: Señal del canal G (guanina)
            quality_scores: Phred scores opcionales

        Returns:
            PredictionResult con lista de artefactos detectados
        """
        start_time = time.perf_counter()

        # Convertir a arrays
        signals = {
            "A": np.array(signal_a),
            "T": np.array(signal_t),
            "C": np.array(signal_c),
            "G": np.array(signal_g),
        }

        artifacts = []

        # Detectar cada tipo de artefacto
        artifacts.extend(self._detect_spikes(signals))
        artifacts.extend(self._detect_pull_ups(signals))
        artifacts.extend(self._detect_dye_blobs(signals))
        artifacts.extend(self._detect_low_signal(signals))
        artifacts.extend(self._detect_baseline_drift(signals))

        # Ordenar por posición
        artifacts.sort(key=lambda a: a.position)

        # Calcular calidad general
        total_affected = sum(a.length for a in artifacts)
        seq_length = len(signal_a)
        affected_ratio = total_affected / seq_length if seq_length > 0 else 0

        # Confianza inversamente proporcional a artefactos
        confidence = max(0, 1 - affected_ratio)

        processing_time = (time.perf_counter() - start_time) * 1000

        return PredictionResult(
            modelName=self.name,
            modelVersion=self.version,
            prediction={
                "artifacts": [a.to_dict() for a in artifacts],
                "totalArtifacts": len(artifacts),
                "affectedBases": total_affected,
            },
            confidence=round(confidence, 3),
            processingTimeMs=round(processing_time, 2),
            metadata={
                "sequenceLength": seq_length,
                "affectedRatio": round(affected_ratio, 3),
                "artifactsByType": self._count_by_type(artifacts),
            },
        )

    def _detect_spikes(self, signals: dict[str, np.ndarray]) -> list[DetectedArtifact]:
        """Detectar picos anómalos (spikes)."""
        artifacts = []

        for channel, signal in signals.items():
            if len(signal) < 10:
                continue

            mean = np.mean(signal)
            std = np.std(signal)

            if std == 0:
                continue

            # Buscar valores que excedan umbral
            z_scores = np.abs((signal - mean) / std)
            spike_positions = np.where(z_scores > self.SPIKE_THRESHOLD)[0]

            # Agrupar spikes consecutivos
            if len(spike_positions) > 0:
                groups = self._group_consecutive(spike_positions)
                for group in groups:
                    severity = float(np.max(z_scores[group]) / 10)  # Normalizar
                    artifacts.append(
                        DetectedArtifact(
                            type=ArtifactType.SPIKE,
                            position=int(group[0]),
                            length=len(group),
                            severity=min(severity, 1.0),
                            channel=channel,
                        )
                    )

        return artifacts

    def _detect_pull_ups(self, signals: dict[str, np.ndarray]) -> list[DetectedArtifact]:
        """Detectar pull-ups (bleeding entre canales)."""
        artifacts = []
        channels = list(signals.keys())

        for i, ch1 in enumerate(channels):
            for ch2 in channels[i + 1 :]:
                sig1 = signals[ch1]
                sig2 = signals[ch2]

                if len(sig1) != len(sig2) or len(sig1) < 10:
                    continue

                # Buscar correlación alta en picos
                for pos in range(len(sig1)):
                    if sig1[pos] > np.mean(sig1) * 2:  # Pico en canal 1
                        ratio = sig2[pos] / sig1[pos] if sig1[pos] > 0 else 0
                        if self.PULL_UP_RATIO < ratio < 0.8:  # Pull-up detectado
                            artifacts.append(
                                DetectedArtifact(
                                    type=ArtifactType.PULL_UP,
                                    position=pos,
                                    length=1,
                                    severity=ratio,
                                    channel=f"{ch1}->{ch2}",
                                )
                            )

        return artifacts[:20]  # Limitar a 20 pull-ups

    def _detect_dye_blobs(self, signals: dict[str, np.ndarray]) -> list[DetectedArtifact]:
        """Detectar dye blobs (manchas de fluoróforo)."""
        artifacts = []

        for channel, signal in signals.items():
            if len(signal) < self.DYE_BLOB_WIDTH:
                continue

            # Dye blobs típicamente en primeras 100 posiciones
            early_region = signal[: min(150, len(signal))]

            # Buscar regiones anchas de alta señal
            threshold = np.mean(signal) * 2
            high_signal = early_region > threshold

            # Encontrar regiones anchas
            groups = self._group_consecutive(np.where(high_signal)[0])
            for group in groups:
                if len(group) >= self.DYE_BLOB_WIDTH // 2:
                    severity = len(group) / self.DYE_BLOB_WIDTH
                    artifacts.append(
                        DetectedArtifact(
                            type=ArtifactType.DYE_BLOB,
                            position=int(group[0]),
                            length=len(group),
                            severity=min(severity, 1.0),
                            channel=channel,
                        )
                    )

        return artifacts

    def _detect_low_signal(self, signals: dict[str, np.ndarray]) -> list[DetectedArtifact]:
        """Detectar regiones de señal baja."""
        artifacts = []

        # Señal combinada
        combined = sum(signals.values())
        if len(combined) < 10:
            return artifacts

        max_signal = np.max(combined)
        if max_signal == 0:
            return artifacts

        threshold = max_signal * self.MIN_SIGNAL_RATIO
        low_regions = combined < threshold

        groups = self._group_consecutive(np.where(low_regions)[0])
        for group in groups:
            if len(group) >= 20:  # Mínimo 20 bases
                artifacts.append(
                    DetectedArtifact(
                        type=ArtifactType.LOW_SIGNAL,
                        position=int(group[0]),
                        length=len(group),
                        severity=0.5,
                        channel=None,
                    )
                )

        return artifacts

    def _detect_baseline_drift(self, signals: dict[str, np.ndarray]) -> list[DetectedArtifact]:
        """Detectar deriva de línea base."""
        artifacts = []

        for channel, signal in signals.items():
            if len(signal) < 100:
                continue

            # Dividir en ventanas y comparar mínimos
            window_size = len(signal) // 10
            baselines = []

            for i in range(0, len(signal), window_size):
                window = signal[i : i + window_size]
                if len(window) > 0:
                    baselines.append(np.percentile(window, 10))

            if len(baselines) >= 3:
                baseline_std = np.std(baselines)
                baseline_mean = np.mean(baselines)

                if baseline_mean > 0 and baseline_std / baseline_mean > 0.3:
                    artifacts.append(
                        DetectedArtifact(
                            type=ArtifactType.BASELINE_DRIFT,
                            position=0,
                            length=len(signal),
                            severity=min(baseline_std / baseline_mean, 1.0),
                            channel=channel,
                        )
                    )

        return artifacts

    def _group_consecutive(self, indices: np.ndarray) -> list[np.ndarray]:
        """Agrupar índices consecutivos."""
        if len(indices) == 0:
            return []

        groups = []
        current_group = [indices[0]]

        for i in range(1, len(indices)):
            if indices[i] == indices[i - 1] + 1:
                current_group.append(indices[i])
            else:
                groups.append(np.array(current_group))
                current_group = [indices[i]]

        groups.append(np.array(current_group))
        return groups

    def _count_by_type(self, artifacts: list[DetectedArtifact]) -> dict[str, int]:
        """Contar artefactos por tipo."""
        counts = {}
        for a in artifacts:
            counts[a.type.value] = counts.get(a.type.value, 0) + 1
        return counts
