"""Enhanced dataset for Quality Predictor with rich signal features.

This dataset extracts comprehensive features from chromatogram signals
to improve quality_enhanced score prediction accuracy.
"""

import json
from dataclasses import dataclass
from pathlib import Path
from typing import Iterator

import numpy as np
import torch
from torch.utils.data import Dataset

from .features.signal_features import SignalFeatureExtractor
from .trace_dataset import AB1Parser, TraceSample


@dataclass
class QualityDatasetConfig:
    """Configuration for quality_enhanced feature dataset."""

    max_length: int = 1000
    min_length: int = 100
    normalize_signals: bool = True
    include_aux_features: bool = True
    noise_window: int = 10
    smoothing_sigma: float = 1.0


class QualityFeatureDataset(Dataset):
    """PyTorch Dataset with enhanced features for quality_enhanced prediction.

    Features extracted per position:
    - 4-channel normalized signals (A, T, C, G)
    - Signal-to-noise ratio (SNR)
    - Peak clarity (max/second_max ratio)
    - Channel entropy (ambiguity measure)
    - Base calling confidence
    - Peak height, separation, width
    - Signal gradient and curvature
    - Local noise estimate
    - Signal smoothness

    Total: 4 base channels + 8 auxiliary features = 12 channels
    """

    def __init__(
        self,
        trace_dir: str | Path,
        config: QualityDatasetConfig | None = None,
    ):
        """Initialize the dataset.

        Args:
            trace_dir: Directory containing .ab1 trace files
            config: Dataset configuration
        """
        self.trace_dir = Path(trace_dir)
        self.config = config or QualityDatasetConfig()

        # Initialize feature extractor
        self.featuREDACTED = SignalFeatureExtractor(
            noise_window=self.config.noise_window,
            smoothing_sigma=self.config.smoothing_sigma,
        )

        # Find all trace files
        self.files = list(self.trace_dir.glob("**/*.ab1"))
        self._samples_cache: dict[int, tuple[TraceSample, dict]] = {}

    def __len__(self) -> int:
        return len(self.files)

    def _load_and_extract(self, idx: int) -> tuple[TraceSample, dict] | None:
        """Load trace and extract features."""
        if idx in self._samples_cache:
            return self._samples_cache[idx]

        try:
            parser = AB1Parser(self.files[idx])
            sample = parser.to_sample()

            # Validate sample
            if sample.length < self.config.min_length:
                return None
            if len(sample.quality_scores) != sample.length:
                return None
            if len(sample.peak_locations) != sample.length:
                return None

            # Extract features
            features = self.featuREDACTED.extract(
                signal_a=sample.signal_a,
                signal_t=sample.signal_t,
                signal_c=sample.signal_c,
                signal_g=sample.signal_g,
                peak_locations=sample.peak_locations,
                normalize=self.config.normalize_signals,
            )

            # Get global stats
            global_stats = self.featuREDACTED.extract_global_stats(
                sample.signal_a,
                sample.signal_t,
                sample.signal_c,
                sample.signal_g,
            )

            result = (sample, {"features": features, "global_stats": global_stats})
            self._samples_cache[idx] = result
            return result

        except Exception:
            return None

    def __getitem__(self, idx: int) -> dict[str, torch.Tensor]:
        """Get a training sample with full features.

        Returns:
            Dictionary containing:
            - signals: (4, max_length) - Base signal channels
            - aux_features: (8, max_length) - Auxiliary feature channels
            - quality_enhanced: (max_length,) - Target quality_enhanced scores
            - mask: (max_length,) - Valid position mask
            - global_stats: (5,) - Global signal statistics
        """
        result = self._load_and_extract(idx)

        if result is None:
            return self._dummy_sample()

        sample, extracted = result
        features = extracted["features"]
        global_stats = extracted["global_stats"]

        # Get tensor dict from features
        tensor_dict = features.to_tensor_dict(self.config.max_length)

        # Prepare quality_enhanced scores
        quality = sample.quality_scores.astype(np.float32)
        seq_len = min(len(quality), self.config.max_length)

        if seq_len < self.config.max_length:
            quality = np.pad(quality, (0, self.config.max_length - seq_len), mode="constant")
        else:
            quality = quality[: self.config.max_length]

        # Global stats array
        stats_array = np.array(
            [
                global_stats["mean_intensity"],
                global_stats["max_intensity"],
                global_stats["std_intensity"],
                global_stats["signal_range"],
                global_stats["baseline_noise"],
            ],
            dtype=np.float32,
        )

        output = {
            "signals": torch.tensor(tensor_dict["signals"], dtype=torch.float32),
            "quality_enhanced": torch.tensor(quality, dtype=torch.float32),
            "mask": torch.tensor(tensor_dict["mask"], dtype=torch.bool),
            "global_stats": torch.tensor(stats_array, dtype=torch.float32),
        }

        if self.config.include_aux_features:
            output["aux_features"] = torch.tensor(
                tensor_dict["aux_features"], dtype=torch.float32
            )

        return output

    def _dummy_sample(self) -> dict[str, torch.Tensor]:
        """Return dummy sample for failed loads."""
        output = {
            "signals": torch.zeros(4, self.config.max_length),
            "quality_enhanced": torch.zeros(self.config.max_length),
            "mask": torch.zeros(self.config.max_length, dtype=torch.bool),
            "global_stats": torch.zeros(5),
        }

        if self.config.include_aux_features:
            output["aux_features"] = torch.zeros(8, self.config.max_length)

        return output

    def get_input_channels(self) -> int:
        """Get number of input channels for model."""
        base_channels = 4
        aux_channels = 8 if self.config.include_aux_features else 0
        return base_channels + aux_channels

    def iter_samples(self) -> Iterator[tuple[TraceSample, dict]]:
        """Iterate over valid samples with features."""
        for idx in range(len(self)):
            result = self._load_and_extract(idx)
            if result is not None:
                yield result

    def compute_statistics(self) -> dict:
        """Compute dataset statistics for normalization."""
        quality_scores = []
        snr_values = []
        signal_values = []

        for sample, extracted in self.iter_samples():
            quality_scores.extend(sample.quality_scores.tolist())
            features = extracted["features"]
            snr_values.extend(features.snr.tolist())
            signal_values.extend(features.peak_height.tolist())

        return {
            "quality_enhanced": {
                "mean": float(np.mean(quality_scores)),
                "std": float(np.std(quality_scores)),
                "min": float(np.min(quality_scores)),
                "max": float(np.max(quality_scores)),
            },
            "snr": {
                "mean": float(np.mean(snr_values)),
                "std": float(np.std(snr_values)),
            },
            "signal": {
                "mean": float(np.mean(signal_values)),
                "std": float(np.std(signal_values)),
            },
        }


class PrecomputedQualityDataset(Dataset):
    """Dataset that loads precomputed features from disk.

    Use QualityDatasetBuilder to create the precomputed dataset.
    """

    def __init__(
        self,
        data_dir: str | Path,
        max_length: int = 1000,
        include_aux_features: bool = True,
    ):
        """Initialize from precomputed data directory.

        Args:
            data_dir: Directory with precomputed .npz files
            max_length: Maximum sequence length
            include_aux_features: Whether to include auxiliary features
        """
        self.data_dir = Path(data_dir)
        self.max_length = max_length
        self.include_aux_features = include_aux_features

        # Find all sample files
        self.files = sorted(self.data_dir.glob("sample_*.npz"))

        # Load metadata if available
        metadata_path = self.data_dir / "metadata.json"
        self.metadata = {}
        if metadata_path.exists():
            with open(metadata_path) as f:
                self.metadata = json.load(f)

    def __len__(self) -> int:
        return len(self.files)

    def __getitem__(self, idx: int) -> dict[str, torch.Tensor]:
        """Load precomputed sample."""
        data = np.load(self.files[idx])

        output = {
            "signals": torch.tensor(data["signals"], dtype=torch.float32),
            "quality_enhanced": torch.tensor(data["quality_enhanced"], dtype=torch.float32),
            "mask": torch.tensor(data["mask"], dtype=torch.bool),
        }

        if self.include_aux_features and "aux_features" in data:
            output["aux_features"] = torch.tensor(
                data["aux_features"], dtype=torch.float32
            )

        if "global_stats" in data:
            output["global_stats"] = torch.tensor(
                data["global_stats"], dtype=torch.float32
            )

        return output


class QualityDatasetBuilder:
    """Builds precomputed quality_enhanced datasets for efficient training."""

    def __init__(
        self,
        config: QualityDatasetConfig | None = None,
        val_split: float = 0.1,
        seed: int = 42,
    ):
        """Initialize builder.

        Args:
            config: Dataset configuration
            val_split: Validation set fraction
            seed: Random seed for splitting
        """
        self.config = config or QualityDatasetConfig()
        self.val_split = val_split
        self.seed = seed

        self.featuREDACTED = SignalFeatureExtractor(
            noise_window=self.config.noise_window,
            smoothing_sigma=self.config.smoothing_sigma,
        )

    def build(
        self,
        input_dir: Path,
        output_dir: Path,
        max_samples: int | None = None,
    ) -> dict:
        """Build precomputed dataset from trace files.

        Args:
            input_dir: Directory with .ab1 files
            output_dir: Output directory for precomputed data
            max_samples: Maximum samples to process

        Returns:
            Dictionary with dataset statistics
        """
        import random

        random.seed(self.seed)

        # Find all trace files
        trace_files = list(input_dir.glob("**/*.ab1"))
        random.shuffle(trace_files)

        if max_samples:
            trace_files = trace_files[:max_samples]

        # Process traces
        samples = []
        quality_all = []
        snr_all = []

        for file_path in trace_files:
            try:
                data = self._process_trace(file_path)
                if data is not None:
                    samples.append(data)
                    quality_all.extend(data["quality_enhanced"].tolist())
                    snr_all.extend(data["aux_features"][0].tolist())
            except Exception:
                continue

        if not samples:
            raise ValueError("No valid samples found")

        # Split train/val
        split_idx = int(len(samples) * (1 - self.val_split))
        train_samples = samples[:split_idx]
        val_samples = samples[split_idx:]

        # Save datasets
        train_dir = output_dir / "train"
        val_dir = output_dir / "val"
        train_dir.mkdir(parents=True, exist_ok=True)
        val_dir.mkdir(parents=True, exist_ok=True)

        self._save_samples(train_samples, train_dir)
        self._save_samples(val_samples, val_dir)

        # Compute and save statistics
        stats = {
            "total_samples": len(samples),
            "train_samples": len(train_samples),
            "val_samples": len(val_samples),
            "quality_mean": float(np.mean(quality_all)),
            "quality_std": float(np.std(quality_all)),
            "snr_mean": float(np.mean(snr_all)),
            "snr_std": float(np.std(snr_all)),
            "config": {
                "max_length": self.config.max_length,
                "min_length": self.config.min_length,
                "normalize_signals": self.config.normalize_signals,
                "include_aux_features": self.config.include_aux_features,
            },
        }

        # Save metadata
        with open(output_dir / "metadata.json", "w") as f:
            json.dump(stats, f, indent=2)

        return stats

    def _process_trace(self, file_path: Path) -> dict | None:
        """Process a single trace file."""
        parser = AB1Parser(file_path)
        sample = parser.to_sample()

        # Validate
        if sample.length < self.config.min_length:
            return None
        if len(sample.quality_scores) != sample.length:
            return None
        if len(sample.peak_locations) != sample.length:
            return None

        # Extract features
        features = self.featuREDACTED.extract(
            signal_a=sample.signal_a,
            signal_t=sample.signal_t,
            signal_c=sample.signal_c,
            signal_g=sample.signal_g,
            peak_locations=sample.peak_locations,
            normalize=self.config.normalize_signals,
        )

        # Get tensor dict
        tensor_dict = features.to_tensor_dict(self.config.max_length)

        # Prepare quality_enhanced
        quality = sample.quality_scores.astype(np.float32)
        seq_len = min(len(quality), self.config.max_length)

        if seq_len < self.config.max_length:
            quality = np.pad(quality, (0, self.config.max_length - seq_len))
        else:
            quality = quality[: self.config.max_length]

        # Global stats
        global_stats = self.featuREDACTED.extract_global_stats(
            sample.signal_a,
            sample.signal_t,
            sample.signal_c,
            sample.signal_g,
        )

        stats_array = np.array(
            [
                global_stats["mean_intensity"],
                global_stats["max_intensity"],
                global_stats["std_intensity"],
                global_stats["signal_range"],
                global_stats["baseline_noise"],
            ],
            dtype=np.float32,
        )

        return {
            "file": file_path.name,
            "signals": tensor_dict["signals"],
            "aux_features": tensor_dict["aux_features"],
            "quality_enhanced": quality,
            "mask": tensor_dict["mask"],
            "global_stats": stats_array,
        }

    def _save_samples(self, samples: list[dict], output_dir: Path) -> None:
        """Save samples as .npz files."""
        for i, sample in enumerate(samples):
            file_path = output_dir / f"sample_{i:06d}.npz"
            np.savez_compressed(
                file_path,
                signals=sample["signals"],
                aux_features=sample["aux_features"],
                quality=sample["quality_enhanced"],
                mask=sample["mask"],
                global_stats=sample["global_stats"],
            )
