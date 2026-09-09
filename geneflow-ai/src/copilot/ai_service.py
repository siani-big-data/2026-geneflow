"""GeneFlow AI Service - Unified ML model management for the Copilot.

This module provides a unified interface to all ML models available in GeneFlow,
handling model loading, inference, and result formatting for the agent system.

Models:
    - TaxonomyClassifier: Hierarchical DNA sequence classification (kingdom to genus)
    - HeterozygoteClassifier: Detection of heterozygous positions
    - TrimmingPredictor: Optimal sequence trimming based on quality
    - QualityClassifier: Quality bin classification (Q10-Q50+)
    - QualityPredictor: Per-position quality score prediction
"""

from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

import numpy as np
import structlog
import torch

logger = structlog.get_logger()


class RFModelWrapper:
    """Wrapper for Random Forest models with metadata."""

    def __init__(self, model: Any, class_names: list[str], level: str):
        self.model = model
        self.class_names = class_names
        self.level = level

    def predict(self, X: np.ndarray) -> np.ndarray:
        """Predict class indices."""
        return self.model.predict(X)

    def predict_proba(self, X: np.ndarray) -> np.ndarray:
        """Predict class probabilities."""
        return self.model.predict_proba(X)

    def predict_with_names(self, X: np.ndarray) -> list[dict]:
        """Predict and return class names with probabilities."""
        probs = self.predict_proba(X)
        results = []
        for i in range(len(X)):
            pred_idx = int(np.argmax(probs[i]))
            if pred_idx < len(self.class_names):
                pred_name = self.class_names[pred_idx]
            else:
                pred_name = f"class_{pred_idx}"
            results.append({
                "label": pred_name,
                "confidence": float(probs[i, pred_idx]),
                "level": self.level,
                "all_probs": {
                    (
                        self.class_names[j] if j < len(self.class_names) else f"class_{j}"
                    ): float(probs[i, j])
                    for j in np.argsort(probs[i])[-5:][::-1]  # Top 5
                },
            })
        return results


@dataclass
class ModelInfo:
    """Information about a loaded model."""

    name: str
    version: str
    checkpoint_path: str
    is_loaded: bool = False
    device: str = "cpu"
    parameters: int = 0


@dataclass
class AIServiceConfig:
    """Configuration for the AI Service."""

    checkpoint_dir: Path = field(default_factory=lambda: Path("checkpoints"))
    device: str = "cuda" if torch.cuda.is_available() else "cpu"
    lazy_load: bool = True  # Load models on first use


class GeneFlowAIService:
    """Unified AI service for GeneFlow ML models.

    Manages loading, inference, and lifecycle of all ML models used by the copilot.
    Supports lazy loading to minimize memory usage.

    Usage:
        service = GeneFlowAIService()
        await service.initialize()

        # Classify taxonomy
        result = await service.classify_taxonomy("ATCGATCG...")

        # Detect heterozygotes
        result = await service.detect_heterozygotes(signals)
    """

    def __init__(self, config: AIServiceConfig | None = None):
        self.config = config or AIServiceConfig()
        self._models: dict[str, Any] = {}
        self._model_info: dict[str, ModelInfo] = {}
        self._initialized = False

        # Define available models and their checkpoint paths
        self._model_configs = {
            "taxonomy": {
                "type": "rf",  # Random Forest (pickle)
                "checkpoint": "taxonomy_rf/randomforest_kingdom_20260405_075458/model.pkl",
                "result_file": "taxonomy_rf/randomforest_kingdom_20260405_075458/result.json",
                "level": "kingdom",
            },
            "taxonomy_genus": {
                "type": "rf",
                "checkpoint": "taxonomy_rf/randomforest_genus_20260405_090833/model.pkl",
                "result_file": "taxonomy_rf/randomforest_genus_20260405_090833/result.json",
                "level": "genus",
            },
            "heterozygote": {
                "class": "HeterozygoteClassifier",
                "module": "src.ml.models.heterozygote",
                "checkpoint": "heterozygote/best.pt",
            },
            "trimming": {
                "class": "TrimmingPredictor",
                "module": "src.ml.models.trimming",
                "checkpoint": "trimming/best.pt",
            },
            "quality_classifier": {
                "class": "QualityClassifierCNN",
                "module": "src.ml.models.quality.classifier",
                "checkpoint": "quality_classifier/best.pt",
                "config_file": "quality_classifier/config.json",  # External config
            },
        }

        # Feature extractor for taxonomy RF models (lazy loaded)
        self._featuREDACTED = None

    @property
    def is_initialized(self) -> bool:
        """Check if service is initialized."""
        return self._initialized

    @property
    def available_models(self) -> list[str]:
        """List of available model names."""
        return list(self._model_configs.keys())

    @property
    def loaded_models(self) -> list[str]:
        """List of currently loaded model names."""
        return [name for name, model in self._models.items() if model is not None]

    def get_model_info(self) -> dict[str, ModelInfo]:
        """Get information about all models."""
        return self._model_info.copy()

    async def initialize(self) -> None:
        """Initialize the AI service.

        If lazy_load is True (default), models are loaded on first use.
        Otherwise, all models are loaded immediately.
        """
        if self._initialized:
            return

        logger.info("ai_service_initializing", device=self.config.device)

        # Scan available checkpoints
        for model_name, model_config in self._model_configs.items():
            checkpoint_path = self.config.checkpoint_dir / model_config["checkpoint"]

            # Try fallback if primary doesn't exist
            if not checkpoint_path.exists() and "fallback" in model_config:
                checkpoint_path = self.config.checkpoint_dir / model_config["fallback"]

            self._model_info[model_name] = ModelInfo(
                name=model_name,
                version="unknown",
                checkpoint_path=str(checkpoint_path),
                is_loaded=False,
                device=self.config.device,
            )

            if not self.config.lazy_load:
                await self._load_model(model_name)

        self._initialized = True
        logger.info(
            "ai_service_initialized",
            available_models=self.available_models,
            lazy_load=self.config.lazy_load,
        )

    async def _load_model(self, model_name: str) -> Any:
        """Load a specific model."""
        if model_name in self._models and self._models[model_name] is not None:
            return self._models[model_name]

        if model_name not in self._model_configs:
            raise ValueError(f"Unknown model: {model_name}")

        config = self._model_configs[model_name]
        checkpoint_path = Path(self._model_info[model_name].checkpoint_path)

        if not checkpoint_path.exists():
            logger.warning(
                "model_checkpoint_not_found", model=model_name, path=str(checkpoint_path)
            )
            self._models[model_name] = None
            return None

        try:
            import json
            import pickle

            # Handle RF models (pickle files)
            if config.get("type") == "rf":
                with open(checkpoint_path, "rb") as f:
                    model = pickle.load(f)

                # Load class names from result file
                result_file = self.config.checkpoint_dir / config.get("result_file", "")
                class_names = []
                if result_file.exists():
                    # Read only first 200 lines to get class_names (file is huge)
                    with open(result_file, "r") as f:
                        content = ""
                        for i, line in enumerate(f):
                            content += line
                            if i > 200 and '"class_names"' in content:
                                break
                    try:
                        # Try to parse partial JSON to get class_names
                        import re
                        match = re.search(r'"class_names":\s*\[(.*?)\]', content, re.DOTALL)
                        if match:
                            names_str = "[" + match.group(1) + "]"
                            class_names = json.loads(names_str)
                    except Exception:
                        pass

                # Wrap RF model with metadata
                model = RFModelWrapper(
                    model=model,
                    class_names=class_names,
                    level=config.get("level", "unknown"),
                )

                self._models[model_name] = model
                self._model_info[model_name].is_loaded = True
                logger.info("model_loaded", model=model_name, path=str(checkpoint_path), type="rf")
                return model

            # Handle PyTorch models
            import importlib
            module_path = config["module"]
            class_name = config["class"]

            module = importlib.import_module(module_path)
            model_class = getattr(module, class_name)

            # First, try to load checkpoint to get config
            ckpt = torch.load(checkpoint_path, map_location="cpu", weights_only=False)

            # Check for external config file
            external_config = None
            if "config_file" in config:
                config_file_path = self.config.checkpoint_dir / config["config_file"]
                if config_file_path.exists():
                    with open(config_file_path) as f:
                        external_config = json.load(f)
                    logger.debug("external_config_loaded", path=str(config_file_path))

            # Try different loading strategies
            model = None

            # Strategy 1: Class has load_from_checkpoint (like TaxonomyClassifier)
            if hasattr(model_class, "load_from_checkpoint"):
                model = model_class.load_from_checkpoint(checkpoint_path)

            # Strategy 2: Class has a classmethod load (like HeterozygoteClassifier.load)
            elif hasattr(model_class, "load"):
                getattr(model_class, "load")
                # Check if it's a classmethod by trying to call it
                try:
                    model = model_class.load(checkpoint_path)
                except TypeError:
                    # It's an instance method - need to create instance first
                    pass

            # Strategy 3: Reconstruct from external config (preferred for CNN models)
            if model is None and external_config and "args" in external_config:
                args = external_config["args"]

                # Find the config class
                config_class_name = class_name + "Config"
                if "CNN" in class_name:
                    config_class_name = class_name.replace("CNN", "CNNConfig")

                config_class = None
                for name in [config_class_name, class_name + "Config"]:
                    if hasattr(module, name):
                        config_class = getattr(module, name)
                        break

                if config_class:
                    import dataclasses
                    if dataclasses.is_dataclass(config_class):
                        valid_fields = {f.name for f in dataclasses.fields(config_class)}
                        # Map external config args to config class fields
                        config_mapping = {
                            "hidden_channels": args.get("hidden_channels"),
                            "num_layers": args.get("num_layers"),
                            "kernel_size": args.get("kernel_size"),
                            "dropout": args.get("dropout"),
                            "num_classes": args.get("num_classes"),
                            "input_channels": external_config.get("n_features"),
                        }
                        filtered_config = {k: v for k, v in config_mapping.items()
                                          if k in valid_fields and v is not None}
                        model_config = config_class(**filtered_config)
                        model = model_class(model_config)

                        # Load state dict
                        if "state_dict" in ckpt:
                            model.load_state_dict(ckpt["state_dict"])

            # Strategy 4: Reconstruct from checkpoint config
            if model is None and "config" in ckpt:
                config_data = ckpt["config"]

                # Find the config class
                config_class_name = class_name + "Config"
                if "CNN" in class_name:
                    config_class_name = class_name.replace("CNN", "CNNConfig")

                # Try to find config class in module
                config_class = None
                for name in [config_class_name, class_name + "Config", "Config"]:
                    if hasattr(module, name):
                        config_class = getattr(module, name)
                        break

                if config_class:
                    # Filter config_data to only include valid fields
                    import dataclasses
                    if dataclasses.is_dataclass(config_class):
                        valid_fields = {f.name for f in dataclasses.fields(config_class)}
                        filtered_config = {
                            k: v for k, v in config_data.items() if k in valid_fields
                        }
                    else:
                        filtered_config = config_data

                    model_config = config_class(**filtered_config)
                    model = model_class(model_config)

                    # Load state dict
                    if "state_dict" in ckpt:
                        model.load_state_dict(ckpt["state_dict"])
                    elif hasattr(model, "load"):
                        model.load(checkpoint_path)
                else:
                    raise ValueError(f"Cannot find config class for {class_name}")

            if model is None:
                raise ValueError(f"Could not load model {class_name}")

            # Move to device
            if hasattr(model, "to"):
                model = model.to(self.config.device)

            # Set to eval mode
            if hasattr(model, "eval"):
                model.eval()

            self._models[model_name] = model

            # Update model info
            self._model_info[model_name].is_loaded = True
            if hasattr(model, "parameters"):
                self._model_info[model_name].parameters = sum(
                    p.numel() for p in model.parameters()
                )

            logger.info(
                "model_loaded",
                model=model_name,
                path=str(checkpoint_path),
                parameters=self._model_info[model_name].parameters,
            )

            return model

        except Exception as e:
            logger.error("model_load_error", model=model_name, error=str(e))
            self._models[model_name] = None
            return None

    async def _ensuREDACTED(self, model_name: str) -> Any:
        """Ensure a model is loaded and return it."""
        if model_name not in self._models or self._models[model_name] is None:
            await self._load_model(model_name)
        return self._models.get(model_name)

    # =========================================================================
    # Taxonomy Classification
    # =========================================================================

    def _get_featuREDACTED(self):
        """Get or create the feature extractor for taxonomy."""
        if self._featuREDACTED is None:
            from src.ml.datasets.features.sequence_features import SequenceFeatureExtractor
            self._featuREDACTED = SequenceFeatureExtractor(
                window_size=100,
                compute_codons=False,
            )
        return self._featuREDACTED

    def _extract_taxonomy_features(self, sequence: str) -> np.ndarray:
        """Extract features from a DNA sequence for taxonomy classification."""
        extractor = self._get_featuREDACTED()
        features = extractor.extract(sequence)
        return features.to_array(include_kmers=True, include_codons=False)

    async def classify_taxonomy(
        self,
        sequence: str,
        features: np.ndarray | None = None,
        include_genus: bool = False,
    ) -> dict[str, Any]:
        """Classify a DNA sequence taxonomically.

        Args:
            sequence: DNA sequence string (ATCG)
            features: Optional precomputed features (101-dim vector)
            include_genus: If True, also load and predict genus (slower, lower accuracy)

        Returns:
            Dictionary with predictions for each taxonomic level:
            {
                "predictions": {
                    "kingdom": {"label": "animalia", "confidence": 0.95, ...},
                    "genus": {"label": "homo", "confidence": 0.45, ...},  # if include_genus
                },
                "hierarchy": ["animalia", ..., "homo"],
                "model": {"name": "taxonomy_rf", "type": "random_forest"}
            }
        """
        # Load kingdom model (always)
        kingdom_model = await self._ensuREDACTED("taxonomy")

        # Load genus model only if requested (slow, 1978 classes)
        genus_model = None
        if include_genus:
            genus_model = await self._ensuREDACTED("taxonomy_genus")

        if kingdom_model is None and genus_model is None:
            return {"error": "No taxonomy models available"}

        try:
            # Extract features if not provided
            if features is None:
                if len(sequence) < 100:
                    return {"error": "Sequence too short (minimum 100 bp)"}
                features = self._extract_taxonomy_features(sequence)

            # Reshape for prediction (single sample)
            X = features.reshape(1, -1)

            predictions = {}
            hierarchy = []

            # Predict kingdom
            if kingdom_model is not None and isinstance(kingdom_model, RFModelWrapper):
                result = kingdom_model.predict_with_names(X)[0]
                predictions["kingdom"] = result
                hierarchy.append(result["label"])

            # Predict genus
            if genus_model is not None and isinstance(genus_model, RFModelWrapper):
                result = genus_model.predict_with_names(X)[0]
                predictions["genus"] = result
                hierarchy.append(result["label"])

            return {
                "predictions": predictions,
                "hierarchy": hierarchy,
                "sequence_length": len(sequence),
                "model": {
                    "name": "taxonomy_rf",
                    "type": "random_forest",
                    "levels": list(predictions.keys()),
                },
            }

        except Exception as e:
            logger.error("taxonomy_classification_error", error=str(e))
            return {"error": str(e)}

    # =========================================================================
    # Heterozygote Detection
    # =========================================================================

    async def detect_heterozygotes(
        self,
        signals: np.ndarray,
        threshold: float = 0.5,
    ) -> dict[str, Any]:
        """Detect heterozygous positions from chromatogram signals.

        Args:
            signals: Normalized chromatogram signals (4, seq_len) for A, C, G, T
                     or (8, seq_len) for enhanced features
            threshold: Probability threshold for heterozygote call

        Returns:
            {
                "positions": [list of heterozygous positions],
                "probabilities": [probability at each position],
                "count": number of heterozygotes found,
                "model": {"name": "heterozygote", ...}
            }
        """
        model = await self._ensuREDACTED("heterozygote")
        if model is None:
            return {"error": "Heterozygote model not available"}

        try:
            # Ensure correct shape (channels, seq_len)
            if signals.ndim == 1:
                return {"error": "Signals must be 2D array (channels, seq_len)"}

            if signals.shape[0] > signals.shape[1]:
                signals = signals.T

            # Check expected input channels from model config
            expected_channels = getattr(model.config, "input_channels", 4)

            if signals.shape[0] != expected_channels:
                if signals.shape[0] == 4 and expected_channels == 8:
                    # Expand 4-channel to 8-channel with derived features
                    # Channels 0-3: A, C, G, T signals
                    # Channels 4-7: derived features (ratios, etc.)
                    a, c, g, t = signals[0], signals[1], signals[2], signals[3]
                    total = a + c + g + t + 1e-8
                    gc_ratio = (g + c) / total
                    at_ratio = (a + t) / total
                    max_signal = np.maximum.reduce([a, c, g, t])
                    second_max = np.sort(signals, axis=0)[-2]
                    signals = np.vstack([a, c, g, t, gc_ratio, at_ratio, max_signal, second_max])
                else:
                    return {
                    "error": (
                        f"Model expects {expected_channels} channels, "
                        f"got {signals.shape[0]}"
                    )
                }

            # Predict using model's predict method
            probs = model.predict(signals)

            # Find heterozygous positions
            hetero_positions = np.where(probs > threshold)[0].tolist()

            return {
                "positions": hetero_positions,
                "probabilities": probs.tolist(),
                "count": len(hetero_positions),
                "threshold": threshold,
                "model": {
                    "name": "heterozygote",
                    "version": getattr(model.config, "version", "1.0.0"),
                },
            }

        except Exception as e:
            logger.error("heterozygote_detection_error", error=str(e))
            return {"error": str(e)}

    # =========================================================================
    # Quality Classification
    # =========================================================================

    async def classify_quality(
        self,
        signals: np.ndarray,
    ) -> dict[str, Any]:
        """Classify quality into bins at each position.

        Args:
            signals: Chromatogram feature signals (channels, seq_len)

        Returns:
            {
                "classes": [class index per position],
                "class_names": ["Q10", "Q20", "Q30", "Q40", "Q50+"],
                "probabilities": [[probs per class] per position],
                "summary": {"Q30+": 85.5, "mean_class": 2.3},
                "model": {...}
            }
        """
        model = await self._ensuREDACTED("quality_classifier")
        if model is None:
            return {"error": "Quality classifier not available"}

        try:
            class_names = ["Q10", "Q20", "Q30", "Q40", "Q50+"]

            # Predict classes
            classes = model.predict(signals)
            probs = model.predict_proba(signals)

            # Summary statistics
            q30_plus = np.mean(classes >= 2) * 100  # Classes 2, 3, 4 are Q30+
            mean_class = np.mean(classes)

            return {
                "classes": classes.tolist(),
                "class_names": class_names,
                "probabilities": probs.tolist(),
                "summary": {
                    "Q30_plus_percent": round(q30_plus, 2),
                    "mean_quality_class": round(mean_class, 2),
                    "positions_analyzed": len(classes),
                },
                "model": {
                    "name": "quality_classifier",
                    "version": getattr(model.cfg, "version", "1.0.0"),
                },
            }

        except Exception as e:
            logger.error("quality_classification_error", error=str(e))
            return {"error": str(e)}

    # =========================================================================
    # Trimming Prediction
    # =========================================================================

    async def predict_trim_points(
        self,
        quality_scores: list[int] | np.ndarray,
    ) -> dict[str, Any]:
        """Predict optimal trim points for a sequence.

        Args:
            quality_scores: Phred quality scores array

        Returns:
            {
                "start": trim start position,
                "end": trim end position,
                "trimmed_length": length after trimming,
                "original_length": original length,
                "trim_percent": percentage of sequence trimmed,
                "model": {...}
            }
        """
        model = await self._ensuREDACTED("trimming")
        if model is None:
            return {"error": "Trimming model not available"}

        try:
            if isinstance(quality_scores, list):
                quality_scores = np.array(quality_scores)

            start, end = model.predict(quality_scores)
            original_len = len(quality_scores)
            trimmed_len = end - start

            return {
                "start": start,
                "end": end,
                "trimmed_length": trimmed_len,
                "original_length": original_len,
                "trim_percent": round((1 - trimmed_len / original_len) * 100, 2),
                "recommendation": self._format_trim_recommendation(start, end, original_len),
                "model": {
                    "name": "trimming",
                    "version": "1.0.0",
                },
            }

        except Exception as e:
            logger.error("trimming_prediction_error", error=str(e))
            return {"error": str(e)}

    def _format_trim_recommendation(self, start: int, end: int, total: int) -> str:
        """Format a human-readable trim recommendation."""
        trim_start_pct = start / total * 100
        trim_end_pct = (total - end) / total * 100

        parts = []
        if trim_start_pct > 5:
            parts.append(f"Recortar {start} bp del inicio ({trim_start_pct:.1f}%)")
        if trim_end_pct > 5:
            parts.append(f"Recortar {total - end} bp del final ({trim_end_pct:.1f}%)")

        if not parts:
            return "Secuencia de buena calidad, recorte mínimo recomendado"

        return "; ".join(parts)

    # =========================================================================
    # Batch / Combined Analysis
    # =========================================================================

    async def analyze_trace(
        self,
        sequence: str,
        quality_scores: list[int] | np.ndarray,
        signals: np.ndarray | None = None,
        quality_signals: np.ndarray | None = None,
    ) -> dict[str, Any]:
        """Perform comprehensive analysis on a sequencing trace.

        Combines multiple models for a complete analysis:
        - Taxonomy classification
        - Quality assessment
        - Trimming recommendations
        - Heterozygote detection (if signals provided)

        Args:
            sequence: DNA sequence string
            quality_scores: Phred quality scores
            signals: Optional chromatogram signals (4, seq_len) for A, C, G, T
            quality_signals: Optional extended feature signals (13, seq_len) for quality
                           If not provided but signals has 13 channels, first 4 are used
                           for heterozygote and all 13 for quality.

        Returns:
            Combined analysis results from all applicable models.
        """
        results = {
            "sequence_length": len(sequence),
            "analyses": {},
        }

        # Taxonomy
        taxonomy_result = await self.classify_taxonomy(sequence)
        if "error" not in taxonomy_result:
            results["analyses"]["taxonomy"] = taxonomy_result

        # Trimming
        trim_result = await self.predict_trim_points(quality_scores)
        if "error" not in trim_result:
            results["analyses"]["trimming"] = trim_result

        # Determine signal configuration
        chromatogram_signals = signals  # 4-channel for heterozygote
        quality_featuREDACTED = quality_signals  # 13-channel for quality

        # If signals has 13 channels and no separate quality_signals, split them
        if signals is not None and signals.shape[0] >= 13 and quality_signals is None:
            chromatogram_signals = signals[:4]  # First 4: A, C, G, T
            quality_featuREDACTED = signals  # All 13 for quality

        # Heterozygote detection (requires 4-channel chromatogram)
        if chromatogram_signals is not None and chromatogram_signals.shape[0] >= 4:
            if chromatogram_signals.shape[0] > 4:
                hetero_signals = chromatogram_signals[:4]
            else:
                hetero_signals = chromatogram_signals
            hetero_result = await self.detect_heterozygotes(hetero_signals)
            if "error" not in hetero_result:
                results["analyses"]["heterozygotes"] = hetero_result

        # Quality classification (requires 13-channel features)
        if quality_featuREDACTED is not None:
            quality_result = await self.classify_quality(quality_featuREDACTED)
            if "error" not in quality_result:
                results["analyses"]["quality"] = quality_result

        return results

    # =========================================================================
    # Lifecycle
    # =========================================================================

    async def shutdown(self) -> None:
        """Shutdown the AI service and release resources."""
        logger.info("ai_service_shutting_down")

        for model_name in list(self._models.keys()):
            if self._models[model_name] is not None:
                # Move to CPU to free GPU memory
                if hasattr(self._models[model_name], "cpu"):
                    self._models[model_name].cpu()
                self._models[model_name] = None
                self._model_info[model_name].is_loaded = False

        self._initialized = False
        logger.info("ai_service_shutdown_complete")


# Singleton instance
_ai_service: GeneFlowAIService | None = None


def get_ai_service() -> GeneFlowAIService:
    """Get the global AI service instance."""
    global _ai_service
    if _ai_service is None:
        _ai_service = GeneFlowAIService()
    return _ai_service


async def initialize_ai_service(config: AIServiceConfig | None = None) -> GeneFlowAIService:
    """Initialize and return the global AI service."""
    global _ai_service
    if _ai_service is None:
        _ai_service = GeneFlowAIService(config)
    await _ai_service.initialize()
    return _ai_service
