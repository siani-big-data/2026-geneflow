"""Enhanced dataset for Taxonomy Classifier with rich sequence features.

This dataset extracts comprehensive compositional and structural features
from DNA sequences to improve taxonomy classification accuracy.
"""

import gzip
import json
from dataclasses import dataclass
from pathlib import Path
from typing import Any

import numpy as np
import torch
from torch.utils.data import Dataset

from .features.sequence_features import SequenceFeatureExtractor, SequenceFeatures


# Nucleotide encoding
NUCLEOTIDE_MAP = {
    "A": 0, "T": 1, "C": 2, "G": 3, "N": 4,
    "a": 0, "t": 1, "c": 2, "g": 3, "n": 4,
    "U": 1, "u": 1,  # RNA
}


@dataclass
class TaxonomyDatasetConfig:
    """Configuration for taxonomy feature dataset."""

    classification_level: str = "phylum"  # kingdom, phylum, class, order, family, genus, species
    max_seq_length: int = 2000
    min_seq_length: int = 100
    max_samples_per_class: int = 0  # 0 = unlimited
    include_features: bool = True  # Include extracted features
    featuREDACTED: int = 100
    compute_codons: bool = False


class TaxonomyFeatureDataset(Dataset):
    """PyTorch Dataset with enhanced features for taxonomy classification.

    Features extracted per sequence:
    - Nucleotide indices for embedding (seq_length,)
    - GC/AT content and composition metrics
    - Dinucleotide and trinucleotide frequencies (genomic signatures)
    - Sequence complexity and entropy measures
    - Homopolymer run statistics
    - Windowed GC content statistics

    The features provide taxonomically informative signals that complement
    the raw sequence representation.
    """

    def __init__(
        self,
        data_dir: str | Path,
        config: TaxonomyDatasetConfig | None = None,
    ):
        """Initialize the dataset.

        Args:
            data_dir: Root directory containing taxonomy folders
            config: Dataset configuration
        """
        self.data_dir = Path(data_dir)
        self.config = config or TaxonomyDatasetConfig()

        # Initialize feature extractor
        self.featuREDACTED = SequenceFeatureExtractor(
            window_size=self.config.featuREDACTED,
            compute_codons=self.config.compute_codons,
        )

        # Sample storage
        self.samples: list[dict[str, Any]] = []
        self.class_to_idx: dict[str, int] = {}
        self.idx_to_class: dict[int, str] = {}

        # Load samples
        self._load_samples()

    def _load_samples(self) -> None:
        """Scan data directory and load sample metadata."""
        class_counts: dict[str, int] = {}

        # Find all .json metadata files
        json_files = list(self.data_dir.rglob("*.json"))
        print(f"Found {len(json_files)} sequence metadata files")

        for json_path in json_files:
            try:
                with open(json_path, "r") as f:
                    metadata = json.load(f)
            except Exception as e:
                print(f"Error reading {json_path}: {e}")
                continue

            # Get classification label
            taxonomy = metadata.get("taxonomy", {})
            label = self._get_label_from_taxonomy(taxonomy)

            if not label:
                continue

            # Check class limit
            if self.config.max_samples_per_class > 0:
                if class_counts.get(label, 0) >= self.config.max_samples_per_class:
                    continue

            # Check sequence length
            total_length = metadata.get("total_length", 0)
            if total_length < self.config.min_seq_length:
                continue

            # Find FASTA file
            fasta_path = json_path.with_suffix(".fasta.gz")
            if not fasta_path.exists():
                fasta_path = json_path.with_suffix(".fasta")
                if not fasta_path.exists():
                    continue

            # Add to class mapping
            if label not in self.class_to_idx:
                idx = len(self.class_to_idx)
                self.class_to_idx[label] = idx
                self.idx_to_class[idx] = label

            # Store sample
            self.samples.append({
                "fasta_path": str(fasta_path),
                "json_path": str(json_path),
                "metadata": metadata,
                "label": label,
                "label_idx": self.class_to_idx[label],
                "taxonomy": taxonomy,
            })

            class_counts[label] = class_counts.get(label, 0) + 1

        print(f"Loaded {len(self.samples)} samples across {len(self.class_to_idx)} classes")
        for label, count in sorted(class_counts.items()):
            print(f"  {label}: {count} samples")

    def _get_label_from_taxonomy(self, taxonomy: dict) -> str | None:
        """Extract classification label from taxonomy dict."""
        level = self.config.classification_level

        if level in taxonomy:
            label = taxonomy[level]
            if label:
                return label.lower()

        # Handle kingdom variations
        if level == "kingdom":
            kingdom = taxonomy.get("kingdom", "")
            if kingdom:
                kingdom = kingdom.lower()
                if kingdom in ("metazoa", "animalia"):
                    return "animalia"
                return kingdom

        return None

    def _load_sequence(self, fasta_path: str) -> str:
        """Load sequence from FASTA file."""
        path = Path(fasta_path)

        try:
            if path.suffix == ".gz":
                with gzip.open(path, "rt") as f:
                    lines = f.readlines()
            else:
                with open(path, "r") as f:
                    lines = f.readlines()
        except Exception as e:
            print(f"Error reading {path}: {e}")
            return ""

        # Parse FASTA
        sequence_parts = []
        for line in lines:
            line = line.strip()
            if line.startswith(">"):
                continue
            sequence_parts.append(line)

        return "".join(sequence_parts)

    def _encode_sequence(self, sequence: str) -> torch.Tensor:
        """Encode DNA sequence to tensor of indices."""
        max_length = self.config.max_seq_length

        # Truncate from middle if needed
        if len(sequence) > max_length:
            start = (len(sequence) - max_length) // 2
            sequence = sequence[start : start + max_length]

        # Convert to indices
        indices = [NUCLEOTIDE_MAP.get(nuc, 4) for nuc in sequence]

        # Pad if needed
        while len(indices) < max_length:
            indices.append(4)  # Pad with N

        return torch.tensor(indices, dtype=torch.long)

    def __len__(self) -> int:
        return len(self.samples)

    def __getitem__(self, idx: int) -> dict[str, torch.Tensor]:
        """Get a training sample with features.

        Returns:
            Dictionary containing:
            - sequence: (max_seq_length,) - Encoded nucleotide indices
            - features: (n_features,) - Extracted sequence features
            - label: () - Class index
            - mask: (max_seq_length,) - Valid position mask
        """
        sample = self.samples[idx]

        # Load sequence
        sequence = self._load_sequence(sample["fasta_path"])

        if len(sequence) == 0:
            return self._dummy_sample(sample["label_idx"])

        # Encode sequence
        encoded = self._encode_sequence(sequence)

        # Create mask
        seq_len = min(len(sequence), self.config.max_seq_length)
        mask = torch.zeros(self.config.max_seq_length, dtype=torch.bool)
        mask[:seq_len] = True

        output = {
            "sequence": encoded,
            "label": torch.tensor(sample["label_idx"], dtype=torch.long),
            "mask": mask,
            "length": torch.tensor(seq_len, dtype=torch.long),
        }

        # Extract and add features if enabled
        if self.config.include_features:
            features = self.featuREDACTED.extract(sequence)
            featuREDACTED = features.to_array(
                include_kmers=True,
                include_codons=self.config.compute_codons,
            )
            output["features"] = torch.tensor(featuREDACTED, dtype=torch.float32)

        return output

    def _dummy_sample(self, label_idx: int) -> dict[str, torch.Tensor]:
        """Return dummy sample for failed loads."""
        output = {
            "sequence": torch.zeros(self.config.max_seq_length, dtype=torch.long),
            "label": torch.tensor(label_idx, dtype=torch.long),
            "mask": torch.zeros(self.config.max_seq_length, dtype=torch.bool),
            "length": torch.tensor(0, dtype=torch.long),
        }

        if self.config.include_features:
            # Get feature dimension
            n_features = len(SequenceFeatures.featuREDACTED(
                include_kmers=True,
                include_codons=self.config.compute_codons,
            ))
            output["features"] = torch.zeros(n_features, dtype=torch.float32)

        return output

    @property
    def num_classes(self) -> int:
        return len(self.class_to_idx)

    @property
    def class_labels(self) -> list[str]:
        return [self.idx_to_class[i] for i in range(len(self.idx_to_class))]

    @property
    def num_features(self) -> int:
        """Get number of extracted features."""
        return len(SequenceFeatures.featuREDACTED(
            include_kmers=True,
            include_codons=self.config.compute_codons,
        ))

    def get_class_weights(self) -> torch.Tensor:
        """Compute class weights for imbalanced datasets."""
        class_counts = torch.zeros(self.num_classes)
        for sample in self.samples:
            class_counts[sample["label_idx"]] += 1

        # Inverse frequency weighting
        weights = 1.0 / (class_counts + 1e-6)
        weights = weights / weights.sum() * self.num_classes

        return weights

    def get_featuREDACTED(self) -> dict[str, dict[str, float]]:
        """Compute feature statistics across the dataset."""
        all_features = []

        for idx in range(min(len(self), 1000)):  # Sample up to 1000
            sample = self[idx]
            if "features" in sample:
                all_features.append(sample["features"].numpy())

        if not all_features:
            return {}

        features_array = np.stack(all_features)
        featuREDACTED = SequenceFeatures.featuREDACTED(
            include_kmers=True,
            include_codons=self.config.compute_codons,
        )

        stats = {}
        for i, name in enumerate(featuREDACTED[:21]):  # Basic features only
            stats[name] = {
                "mean": float(np.mean(features_array[:, i])),
                "std": float(np.std(features_array[:, i])),
                "min": float(np.min(features_array[:, i])),
                "max": float(np.max(features_array[:, i])),
            }

        return stats


class TaxonomyDatasetBuilder:
    """Builds precomputed taxonomy datasets for efficient training."""

    def __init__(
        self,
        config: TaxonomyDatasetConfig | None = None,
        val_split: float = 0.1,
        test_split: float = 0.1,
        seed: int = 42,
    ):
        """Initialize builder.

        Args:
            config: Dataset configuration
            val_split: Validation set fraction
            test_split: Test set fraction
            seed: Random seed
        """
        self.config = config or TaxonomyDatasetConfig()
        self.val_split = val_split
        self.test_split = test_split
        self.seed = seed

        self.featuREDACTED = SequenceFeatureExtractor(
            window_size=self.config.featuREDACTED,
            compute_codons=self.config.compute_codons,
        )

    def build(
        self,
        input_dir: Path,
        output_dir: Path,
        max_samples: int | None = None,
    ) -> dict:
        """Build precomputed dataset.

        Args:
            input_dir: Root directory with taxonomy folders
            output_dir: Output directory
            max_samples: Maximum total samples

        Returns:
            Dictionary with dataset statistics
        """
        import random

        random.seed(self.seed)

        # Create temporary dataset to load samples
        temp_dataset = TaxonomyFeatureDataset(input_dir, self.config)

        # Collect all samples with their data
        samples = []
        class_counts: dict[str, int] = {}

        for idx in range(len(temp_dataset)):
            try:
                sample_meta = temp_dataset.samples[idx]
                sequence = temp_dataset._load_sequence(sample_meta["fasta_path"])

                if len(sequence) < self.config.min_seq_length:
                    continue

                # Extract features
                features = self.featuREDACTED.extract(sequence)
                featuREDACTED = features.to_array(
                    include_kmers=True,
                    include_codons=self.config.compute_codons,
                )

                # Encode sequence
                encoded = temp_dataset._encode_sequence(sequence).numpy()

                samples.append({
                    "sequence": encoded,
                    "features": featuREDACTED,
                    "label": sample_meta["label_idx"],
                    "label_name": sample_meta["label"],
                    "taxonomy": sample_meta["taxonomy"],
                    "length": min(len(sequence), self.config.max_seq_length),
                })

                class_counts[sample_meta["label"]] = class_counts.get(sample_meta["label"], 0) + 1

            except Exception as e:
                print(f"Error processing sample {idx}: {e}")
                continue

        if not samples:
            raise ValueError("No valid samples found")

        # Shuffle
        random.shuffle(samples)

        if max_samples and len(samples) > max_samples:
            samples = samples[:max_samples]

        # Split into train/val/test
        n_total = len(samples)
        n_test = int(n_total * self.test_split)
        n_val = int(n_total * self.val_split)
        n_train = n_total - n_val - n_test

        train_samples = samples[:n_train]
        val_samples = samples[n_train : n_train + n_val]
        test_samples = samples[n_train + n_val :]

        # Create output directories
        train_dir = output_dir / "train"
        val_dir = output_dir / "val"
        test_dir = output_dir / "test"

        for d in [train_dir, val_dir, test_dir]:
            d.mkdir(parents=True, exist_ok=True)

        # Save samples
        self._save_samples(train_samples, train_dir)
        self._save_samples(val_samples, val_dir)
        self._save_samples(test_samples, test_dir)

        # Compute statistics
        all_features = np.stack([s["features"] for s in samples])
        featuREDACTED = all_features.mean(axis=0)
        featuREDACTED = all_features.std(axis=0)

        # Save metadata
        metadata = {
            "total_samples": len(samples),
            "train_samples": len(train_samples),
            "val_samples": len(val_samples),
            "test_samples": len(test_samples),
            "num_classes": temp_dataset.num_classes,
            "class_labels": temp_dataset.class_labels,
            "class_to_idx": temp_dataset.class_to_idx,
            "class_counts": class_counts,
            "featuREDACTED": SequenceFeatures.featuREDACTED(
                include_kmers=True,
                include_codons=self.config.compute_codons,
            )[:21],  # Basic feature names
            "featuREDACTED": featuREDACTED[:21].tolist(),
            "featuREDACTED": featuREDACTED[:21].tolist(),
            "config": {
                "classification_level": self.config.classification_level,
                "max_seq_length": self.config.max_seq_length,
                "min_seq_length": self.config.min_seq_length,
                "include_features": self.config.include_features,
                "compute_codons": self.config.compute_codons,
            },
        }

        with open(output_dir / "metadata.json", "w") as f:
            json.dump(metadata, f, indent=2)

        return metadata

    def _save_samples(self, samples: list[dict], output_dir: Path) -> None:
        """Save samples as .npz files."""
        for i, sample in enumerate(samples):
            file_path = output_dir / f"sample_{i:06d}.npz"
            np.savez_compressed(
                file_path,
                sequence=sample["sequence"],
                features=sample["features"],
                label=sample["label"],
                length=sample["length"],
            )


class PrecomputedTaxonomyDataset(Dataset):
    """Dataset that loads precomputed taxonomy features from disk."""

    def __init__(
        self,
        data_dir: str | Path,
        include_features: bool = True,
    ):
        """Initialize from precomputed data directory.

        Args:
            data_dir: Directory with precomputed .npz files
            include_features: Whether to include sequence features
        """
        self.data_dir = Path(data_dir)
        self.include_features = include_features

        # Find all sample files
        self.files = sorted(self.data_dir.glob("sample_*.npz"))

        # Load metadata
        metadata_path = self.data_dir.parent / "metadata.json"
        self.metadata = {}
        if metadata_path.exists():
            with open(metadata_path) as f:
                self.metadata = json.load(f)

    def __len__(self) -> int:
        return len(self.files)

    def __getitem__(self, idx: int) -> dict[str, torch.Tensor]:
        """Load precomputed sample."""
        data = np.load(self.files[idx])

        sequence = torch.tensor(data["sequence"], dtype=torch.long)
        length = int(data["length"])

        # Create mask
        mask = torch.zeros(len(sequence), dtype=torch.bool)
        mask[:length] = True

        output = {
            "sequence": sequence,
            "label": torch.tensor(data["label"], dtype=torch.long),
            "mask": mask,
            "length": torch.tensor(length, dtype=torch.long),
        }

        if self.include_features and "features" in data:
            output["features"] = torch.tensor(data["features"], dtype=torch.float32)

        return output

    @property
    def num_classes(self) -> int:
        return self.metadata.get("num_classes", 0)

    @property
    def class_labels(self) -> list[str]:
        return self.metadata.get("class_labels", [])

    def get_class_weights(self) -> torch.Tensor:
        """Get class weights from metadata."""
        class_counts = self.metadata.get("class_counts", {})
        num_classes = self.num_classes

        if not class_counts or num_classes == 0:
            return torch.ones(max(num_classes, 1))

        counts = torch.zeros(num_classes)
        class_to_idx = self.metadata.get("class_to_idx", {})

        for label, count in class_counts.items():
            if label in class_to_idx:
                counts[class_to_idx[label]] = count

        weights = 1.0 / (counts + 1e-6)
        weights = weights / weights.sum() * num_classes

        return weights
