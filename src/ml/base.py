"""Base classes for ML models."""

from abc import ABC, abstractmethod
from dataclasses import dataclass, field
from datetime import datetime, timezone
from enum import Enum
from pathlib import Path
from typing import Any, Optional

import structlog

logger = structlog.get_logger()


class ModelCategory(str, Enum):
    """Categorías de modelos."""

    QUALITY = "quality"
    VARIANTS = "variants"
    ANNOTATION = "annotation"
    PHYLO = "phylo"
    FUNCTIONAL = "functional"


@dataclass
class PredictionResult:
    """Resultado de una predicción de modelo local."""

    modelName: str
    modelVersion: str
    prediction: Any
    confidence: float
    processingTimeMs: float = 0.0
    metadata: dict = field(default_factory=dict)
    timestamp: datetime = field(default_factory=lambda: datetime.now(timezone.utc))

    def to_dict(self) -> dict:
        """Serialize to dict."""
        return {
            "modelName": self.modelName,
            "modelVersion": self.modelVersion,
            "prediction": self.prediction,
            "confidence": self.confidence,
            "processingTimeMs": self.processingTimeMs,
            "metadata": self.metadata,
            "timestamp": self.timestamp.isoformat(),
        }


class LocalModel(ABC):
    """
    Clase base abstracta para modelos ML locales.

    Cada modelo debe implementar:
    - name: Nombre único del modelo
    - version: Versión del modelo
    - category: Categoría (quality, variants, etc.)
    - load(): Cargar modelo en memoria
    - predict(): Ejecutar predicción
    """

    @property
    @abstractmethod
    def name(self) -> str:
        """Nombre único del modelo."""
        pass

    @property
    @abstractmethod
    def version(self) -> str:
        """Versión del modelo."""
        pass

    @property
    @abstractmethod
    def category(self) -> ModelCategory:
        """Categoría del modelo."""
        pass

    @property
    def description(self) -> str:
        """Descripción del modelo para el agente."""
        return f"{self.name} v{self.version}"

    @abstractmethod
    def load(self, model_path: Optional[Path] = None) -> None:
        """
        Cargar modelo en memoria.

        Args:
            model_path: Ruta al archivo del modelo (opcional)
        """
        pass

    @abstractmethod
    def unload(self) -> None:
        """Liberar modelo de memoria."""
        pass

    @abstractmethod
    def is_loaded(self) -> bool:
        """Check if model is loaded in memory."""
        pass

    @abstractmethod
    async def predict(self, **kwargs) -> PredictionResult:
        """
        Ejecutar predicción.

        Returns:
            PredictionResult con la predicción y metadatos
        """
        pass

    def get_info(self) -> dict:
        """Get model info for API."""
        return {
            "name": self.name,
            "version": self.version,
            "category": self.category.value,
            "description": self.description,
            "loaded": self.is_loaded(),
        }


class ModelRegistry:
    """
    Registro central de modelos ML.

    Permite registrar, cargar y acceder a todos los modelos
    desde un punto central.
    """

    def __init__(self):
        self._models: dict[str, LocalModel] = {}
        self._loaded: set[str] = set()

    def register(self, model: LocalModel) -> None:
        """Registrar un modelo."""
        if model.name in self._models:
            logger.warning("model_already_registered", name=model.name)
            return

        self._models[model.name] = model
        logger.info(
            "model_registered",
            name=model.name,
            version=model.version,
            category=model.category.value,
        )

    def get(self, name: str) -> Optional[LocalModel]:
        """Obtener modelo por nombre."""
        return self._models.get(name)

    def get_by_category(self, category: ModelCategory) -> list[LocalModel]:
        """Obtener modelos por categoría."""
        return [m for m in self._models.values() if m.category == category]

    def list_models(self) -> list[dict]:
        """Listar todos los modelos registrados."""
        return [m.get_info() for m in self._models.values()]

    def load_model(self, name: str, model_path: Optional[Path] = None) -> bool:
        """Cargar un modelo específico."""
        model = self._models.get(name)
        if not model:
            logger.error("model_not_found", name=name)
            return False

        try:
            model.load(model_path)
            self._loaded.add(name)
            logger.info("model_loaded", name=name)
            return True
        except Exception as e:
            logger.error("model_load_failed", name=name, error=str(e))
            return False

    def unload_model(self, name: str) -> bool:
        """Descargar un modelo de memoria."""
        model = self._models.get(name)
        if not model:
            return False

        try:
            model.unload()
            self._loaded.discard(name)
            logger.info("model_unloaded", name=name)
            return True
        except Exception as e:
            logger.error("model_unload_failed", name=name, error=str(e))
            return False

    def load_all(self, models_dir: Optional[Path] = None) -> dict[str, bool]:
        """Cargar todos los modelos registrados."""
        results = {}
        for name in self._models:
            model_path = None
            if models_dir:
                model_path = models_dir / f"{name}.onnx"
            results[name] = self.load_model(name, model_path)
        return results

    @property
    def loaded_count(self) -> int:
        """Número de modelos cargados."""
        return len(self._loaded)

    @property
    def total_count(self) -> int:
        """Número total de modelos registrados."""
        return len(self._models)
