"""
Machine Learning models for GeneFlow AI.

Este módulo contiene modelos ML locales especializados que proveen
información al agente conversacional (DeepSeek/Claude).

Categorías:
- quality_enhanced: Análisis de calidad de secuencias Sanger
- variants: Detección de variantes y mutaciones
- annotation: Anotación automática de secuencias
- phylo: Clustering y análisis filogenético
- functional: Predicción de impacto funcional
"""

from .base import LocalModel, ModelRegistry, PredictionResult
from .registry import get_registry, load_all_models

__all__ = [
    "LocalModel",
    "ModelRegistry",
    "PredictionResult",
    "get_registry",
    "load_all_models",
]
