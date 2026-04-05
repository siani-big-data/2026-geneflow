"""Enhanced dataset for Hierarchical Taxonomy Classification with features.

This dataset extracts comprehensive compositional and structural features
from DNA sequences and provides labels for multiple taxonomic levels.
"""

import gzip
import json
from dataclasses import dataclass, field
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
    "U": 1, "u": 1,
}

# Taxonomic levels in hierarchical order
TAXONOMY_LEVELS = ["kingdom", "phylum", "class", "order", "family", "genus"]


@dataclass
class HierarchicalTaxonomyDatasetConfig:
    """Configuration for hierarchical taxonomy feature dataset."""

    levels: list[str] = field(default_factory=lambda: ["kingdom", "phylum", "class"])
    max_seq_length: int = 2000
    min_seq_length: int = 100
    min_samples_per_class: int = 5
    include_features: bool = True
    featuREDACTED: int = 100
    compute_codons: bool = False


class HierarchicalTaxonomyFeatureDataset(Dataset):
    """PyTorch Dataset with features for hierarchical taxonomy classification.

    Extracts features per sequence:
    - Nucleotide indices for embedding
    - GC/AT content and composition metrics
    - Dinucleotide and trinucleotide frequencies
    - Sequence complexity and entropy measures
    - Homopolymer run statistics
    - Windowed GC content statistics

    Provides labels for multiple taxonomic levels simultaneously.
    """

    def __init__(
        self,
        data_dir: str | Path,
        config: HierarchicalTaxonomyDatasetConfig | None = None,
    ):
        self.data_dir = Path(data_dir)
        self.config = config or HierarchicalTaxonomyDatasetConfig()

        # Initialize feature extractor
        self.featuREDACTED = SequenceFeatureExtractor(
            window_size=self.config.featuREDACTED,
            compute_codons=self.config.compute_codons,
        )

        # Mappings per level
        self.class_to_idx: dict[str, dict[str, int]] = {level: {} for level in self.config.levels}
        self.idx_to_class: dict[str, dict[int, str]] = {level: {} for level in self.config.levels}

        # Samples
        self.samples: list[dict[str, Any]] = []

        self._load_samples()

    def _load_samples(self) -> None:
        """Load samples with filtering by minimum samples per class."""
        # First pass: collect all samples and count classes
        all_samples = []
        class_counts: dict[str, dict[str, int]] = {level: {} for level in self.config.levels}

        json_files = list(self.data_dir.rglob("*.json"))
        print(f"Found {len(json_files)} sequence metadata files")

        for json_path in json_files:
            try:
                with open(json_path, "r") as f:
                    metadata = json.load(f)
            except Exception:
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

            # Extract labels for all levels
            taxonomy = metadata.get("taxonomy", {})
            labels = self._extract_labels(taxonomy)

            if not labels:
                continue

            # Count classes
            for level, label in labels.items():
                class_counts[level][label] = class_counts[level].get(label, 0) + 1

            all_samples.append({
                "fasta_path": str(fasta_path),
                "json_path": str(json_path),
                "metadata": metadata,
                "labels": labels,
                "taxonomy": taxonomy,
            })

        # Second pass: filter classes with too few samples
        valid_classes: dict[str, set[str]] = {}
        for level in self.config.levels:
            valid_classes[level] = {
                label for label, count in class_counts[level].items()
                if count >= self.config.min_samples_per_class
            }
            print(f"Level {level}: {len(valid_classes[level])} classes with >= {self.config.min_samples_per_class} samples")

        # Third pass: build final dataset
        for sample in all_samples:
            labels = sample["labels"]

            # Check if all labels are valid
            valid = all(
                level not in labels or labels[level] in valid_classes[level]
                for level in self.config.levels
            )

            if not valid:
                continue

            # Build label indices
            label_indices = {}
            for level in self.config.levels:
                if level in labels:
                    label = labels[level]
                    if label not in self.class_to_idx[level]:
                        idx = len(self.class_to_idx[level])
                        self.class_to_idx[level][label] = idx
                        self.idx_to_class[level][idx] = label
                    label_indices[level] = self.class_to_idx[level][label]
                else:
                    label_indices[level] = -1

            self.samples.append({
                **sample,
                "label_indices": label_indices,
            })

        print(f"\nLoaded {len(self.samples)} samples")
        print("Classes per level:")
        for level in self.config.levels:
            print(f"  {level}: {len(self.class_to_idx[level])} classes")

    def _extract_labels(self, taxonomy: dict) -> dict[str, str]:
        """Extract labels for all configured levels."""
        labels = {}
        for level in self.config.levels:
            label = taxonomy.get(level) or taxonomy.get(f"{level}_")
            if label:
                label = label.lower().strip()
                if level == "kingdom" and label in ("metazoa", "animalia"):
                    label = "animalia"
                if label:
                    labels[level] = label
        return labels

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
        except Exception:
            return ""

        return "".join(line.strip() for line in lines if not line.startswith(">"))

    def _encode_sequence(self, sequence: str) -> torch.Tensor:
        """Encode DNA sequence to tensor."""
        max_length = self.config.max_seq_length

        if len(sequence) > max_length:
            start = (len(sequence) - max_length) // 2
            sequence = sequence[start : start + max_length]

        indices = [NUCLEOTIDE_MAP.get(nuc, 4) for nuc in sequence]

        while len(indices) < max_length:
            indices.append(4)

        return torch.tensor(indices, dtype=torch.long)

    def __len__(self) -> int:
        return len(self.samples)

    def __getitem__(self, idx: int) -> dict[str, torch.Tensor]:
        """Get a training sample with features and multi-level labels."""
        sample = self.samples[idx]

        sequence = self._load_sequence(sample["fasta_path"])

        if len(sequence) == 0:
            return self._dummy_sample(sample["label_indices"])

        encoded = self._encode_sequence(sequence)

        seq_len = min(len(sequence), self.config.max_seq_length)
        mask = torch.zeros(self.config.max_seq_length, dtype=torch.bool)
        mask[:seq_len] = True

        # Build labels dict
        labels = {
            level: torch.tensor(sample["label_indices"][level], dtype=torch.long)
            for level in self.config.levels
        }

        output = {
            "sequence": encoded,
            "labels": labels,
            "mask": mask,
            "length": torch.tensor(seq_len, dtype=torch.long),
        }

        if self.config.include_features:
            features = self.featuREDACTED.extract(sequence)
            featuREDACTED = features.to_array(
                include_kmers=True,
                include_codons=self.config.compute_codons,
            )
            output["features"] = torch.tensor(featuREDACTED, dtype=torch.float32)

        return output

    def _dummy_sample(self, label_indices: dict) -> dict[str, torch.Tensor]:
        """Return dummy sample for failed loads."""
        labels = {
            level: torch.tensor(label_indices[level], dtype=torch.long)
            for level in self.config.levels
        }

        output = {
            "sequence": torch.zeros(self.config.max_seq_length, dtype=torch.long),
            "labels": labels,
            "mask": torch.zeros(self.config.max_seq_length, dtype=torch.bool),
            "length": torch.tensor(0, dtype=torch.long),
        }

        if self.config.include_features:
            n_features = len(SequenceFeatures.featuREDACTED(
                include_kmers=True,
                include_codons=self.config.compute_codons,
            ))
            output["features"] = torch.zeros(n_features, dtype=torch.float32)

        return output

    @property
    def num_classes_per_level(self) -> dict[str, int]:
        return {level: len(self.class_to_idx[level]) for level in self.config.levels}

    @property
    def class_labels_per_level(self) -> dict[str, list[str]]:
        return {
            level: [self.idx_to_class[level][i] for i in range(len(self.idx_to_class[level]))]
            for level in self.config.levels
            if self.idx_to_class[level]
        }

    @property
    def num_features(self) -> int:
        return len(SequenceFeatures.featuREDACTED(
            include_kmers=True,
            include_codons=self.config.compute_codons,
        ))

    def get_class_weights(self, level: str) -> torch.Tensor:
        """Compute class weights for a specific level."""
        num_classes = len(self.class_to_idx[level])
        if num_classes == 0:
            return torch.ones(1)

        class_counts = torch.zeros(num_classes)
        for sample in self.samples:
            idx = sample["label_indices"].get(level, -1)
            if idx >= 0:
                class_counts[idx] += 1

        weights = 1.0 / (class_counts + 1e-6)
        weights = weights / weights.sum() * num_classes
        return weights

    def get_all_class_weights(self) -> dict[str, torch.Tensor]:
        return {level: self.get_class_weights(level) for level in self.config.levels}


class HierarchicalTaxonomyDatasetBuilder:
    """Builds precomputed hierarchical taxonomy datasets."""

    def __init__(
        self,
        config: HierarchicalTaxonomyDatasetConfig | None = None,
        val_split: float = 0.1,
        test_split: float = 0.1,
        seed: int = 42,
    ):
        self.config = config or HierarchicalTaxonomyDatasetConfig()
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
        """Build precomputed dataset."""
        import random
        random.seed(self.seed)

        # Load via feature dataset
        temp_dataset = HierarchicalTaxonomyFeatureDataset(input_dir, self.config)

        if len(temp_dataset) == 0:
            raise ValueError("No valid samples found")

        # Process all samples
        samples = []
        print(f"\nProcessing {len(temp_dataset)} samples...")

        for idx in range(len(temp_dataset)):
            if idx % 1000 == 0:
                print(f"  Processed {idx}/{len(temp_dataset)} samples...")

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
                    "label_indices": sample_meta["label_indices"],
                    "labels": sample_meta["labels"],
                    "taxonomy": sample_meta["taxonomy"],
                    "length": min(len(sequence), self.config.max_seq_length),
                })

            except Exception as e:
                print(f"Error processing sample {idx}: {e}")
                continue

        print(f"Successfully processed {len(samples)} samples")

        # Shuffle and limit
        random.shuffle(samples)
        if max_samples and len(samples) > max_samples:
            samples = samples[:max_samples]

        # Split
        n_total = len(samples)
        n_test = int(n_total * self.test_split)
        n_val = int(n_total * self.val_split)
        n_train = n_total - n_val - n_test

        train_samples = samples[:n_train]
        val_samples = samples[n_train : n_train + n_val]
        test_samples = samples[n_train + n_val :]

        # Create directories
        for split_name, split_samples in [
            ("train", train_samples),
            ("val", val_samples),
            ("test", test_samples),
        ]:
            split_dir = output_dir / split_name
            split_dir.mkdir(parents=True, exist_ok=True)
            self._save_samples(split_samples, split_dir)

        # Compute statistics
        all_features = np.stack([s["features"] for s in samples])
        featuREDACTED = all_features.mean(axis=0)
        featuREDACTED = all_features.std(axis=0)

        # Count classes per level
        class_counts_per_level = {level: {} for level in self.config.levels}
        for sample in samples:
            for level, label in sample["labels"].items():
                class_counts_per_level[level][label] = class_counts_per_level[level].get(label, 0) + 1

        # Save metadata
        metadata = {
            "total_samples": len(samples),
            "train_samples": len(train_samples),
            "val_samples": len(val_samples),
            "test_samples": len(test_samples),
            "levels": self.config.levels,
            "num_classes_per_level": temp_dataset.num_classes_per_level,
            "class_labels_per_level": temp_dataset.class_labels_per_level,
            "class_to_idx_per_level": temp_dataset.class_to_idx,
            "class_counts_per_level": class_counts_per_level,
            "featuREDACTED": SequenceFeatures.featuREDACTED(
                include_kmers=True,
                include_codons=self.config.compute_codons,
            )[:21],
            "featuREDACTED": featuREDACTED[:21].tolist(),
            "featuREDACTED": featuREDACTED[:21].tolist(),
            "num_features": len(featuREDACTED),
            "config": {
                "levels": self.config.levels,
                "max_seq_length": self.config.max_seq_length,
                "min_seq_length": self.config.min_seq_length,
                "min_samples_per_class": self.config.min_samples_per_class,
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
            # Convert label_indices to arrays for numpy
            label_arrays = {
                f"label_{level}": np.array(idx, dtype=np.int64)
                for level, idx in sample["label_indices"].items()
            }

            np.savez_compressed(
                output_dir / f"sample_{i:06d}.npz",
                sequence=sample["sequence"],
                features=sample["features"],
                length=sample["length"],
                **label_arrays,
            )


class PrecomputedHierarchicalTaxonomyDataset(Dataset):
    """Dataset that loads precomputed hierarchical taxonomy features."""

    def __init__(
        self,
        data_dir: str | Path,
        include_features: bool = True,
    ):
        self.data_dir = Path(data_dir)
        self.include_features = include_features

        self.files = sorted(self.data_dir.glob("sample_*.npz"))

        # Load metadata
        metadata_path = self.data_dir.parent / "metadata.json"
        self.metadata = {}
        if metadata_path.exists():
            with open(metadata_path) as f:
                self.metadata = json.load(f)

        self.levels = self.metadata.get("levels", TAXONOMY_LEVELS[:3])

    def __len__(self) -> int:
        return len(self.files)

    def __getitem__(self, idx: int) -> dict[str, torch.Tensor]:
        data = np.load(self.files[idx])

        sequence = torch.tensor(data["sequence"], dtype=torch.long)
        length = int(data["length"])

        mask = torch.zeros(len(sequence), dtype=torch.bool)
        mask[:length] = True

        # Build labels dict
        labels = {}
        for level in self.levels:
            key = f"label_{level}"
            if key in data:
                labels[level] = torch.tensor(int(data[key]), dtype=torch.long)
            else:
                labels[level] = torch.tensor(-1, dtype=torch.long)

        output = {
            "sequence": sequence,
            "labels": labels,
            "mask": mask,
            "length": torch.tensor(length, dtype=torch.long),
        }

        if self.include_features and "features" in data:
            output["features"] = torch.tensor(data["features"], dtype=torch.float32)

        return output

    @property
    def num_classes_per_level(self) -> dict[str, int]:
        return self.metadata.get("num_classes_per_level", {})

    @property
    def class_labels_per_level(self) -> dict[str, list[str]]:
        return self.metadata.get("class_labels_per_level", {})

    def get_class_weights(self, level: str) -> torch.Tensor:
        """Get class weights for a specific level."""
        class_counts = self.metadata.get("class_counts_per_level", {}).get(level, {})
        num_classes = self.num_classes_per_level.get(level, 0)

        if not class_counts or num_classes == 0:
            return torch.ones(max(num_classes, 1))

        counts = torch.zeros(num_classes)
        class_to_idx = self.metadata.get("class_to_idx_per_level", {}).get(level, {})

        for label, count in class_counts.items():
            if label in class_to_idx:
                counts[class_to_idx[label]] = count

        weights = 1.0 / (counts + 1e-6)
        weights = weights / weights.sum() * num_classes
        return weights

    def get_all_class_weights(self) -> dict[str, torch.Tensor]:
        return {level: self.get_class_weights(level) for level in self.levels}
