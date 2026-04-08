"""Dataset for hierarchical taxonomy classification.

This dataset loads DNA sequences and provides labels for multiple taxonomic levels
simultaneously, enabling training of the HierarchicalTaxonomyClassifier.
"""

import gzip
import json
from pathlib import Path
from typing import Any

import torch
from torch.utils.data import Dataset

# Nucleotide to index mapping
NUCLEOTIDE_MAP = {
    "A": 0, "T": 1, "C": 2, "G": 3, "N": 4,
    "a": 0, "t": 1, "c": 2, "g": 3, "n": 4,
    "U": 1, "u": 1,  # RNA
}

# Taxonomic levels in hierarchical order
TAXONOMY_LEVELS = ["kingdom", "phylum", "class", "order", "family", "genus"]


def encode_sequence(sequence: str, max_length: int = 2000) -> torch.Tensor:
    """Encode DNA sequence to tensor of indices."""
    if len(sequence) > max_length:
        start = (len(sequence) - max_length) // 2
        sequence = sequence[start : start + max_length]

    indices = [NUCLEOTIDE_MAP.get(nuc, 4) for nuc in sequence]

    while len(indices) < max_length:
        indices.append(4)

    return torch.tensor(indices, dtype=torch.long)


class HierarchicalTaxonomyDataset(Dataset):
    """Dataset that provides labels for multiple taxonomic levels.

    Expects structure:
        data_dir/
            kingdom1/
                phylum/class/order/family/genus/species/
                    taxon_id.fasta.gz
                    taxon_id.json

    Returns labels for all available taxonomic levels, allowing the model
    to learn hierarchical classification.
    """

    def __init__(
        self,
        data_dir: str | Path,
        levels: list[str] | None = None,
        max_seq_length: int = 2000,
        max_samples_per_class: int = 0,
        min_seq_length: int = 100,
        min_samples_per_class: int = 5,
    ):
        """Initialize the dataset.

        Args:
            data_dir: Root directory containing taxonomy folders
            levels: Which taxonomic levels to include (default: all available)
            max_seq_length: Maximum sequence length to use
            max_samples_per_class: Limit samples per class at the finest level (0 = unlimited)
            min_seq_length: Minimum sequence length to include
            min_samples_per_class: Minimum samples required to include a class
        """
        self.data_dir = Path(data_dir)
        self.levels = levels or TAXONOMY_LEVELS
        self.max_seq_length = max_seq_length
        self.min_seq_length = min_seq_length
        self.min_samples_per_class = min_samples_per_class

        # Mappings per level
        self.class_to_idx: dict[str, dict[str, int]] = {level: {} for level in self.levels}
        self.idx_to_class: dict[str, dict[int, str]] = {level: {} for level in self.levels}

        # Samples list
        self.samples: list[dict[str, Any]] = []

        self._load_samples(max_samples_per_class)

    def _load_samples(self, max_samples_per_class: int):
        """Scan the data directory and load sample metadata."""
        # First pass: collect all samples and count classes
        all_samples = []
        class_counts: dict[str, dict[str, int]] = {level: {} for level in self.levels}

        json_files = list(self.data_dir.rglob("*.json"))
        print(f"Found {len(json_files)} sequence metadata files")

        for json_path in json_files:
            try:
                with open(json_path, "r") as f:
                    metadata = json.load(f)
            except Exception as e:
                print(f"Error reading {json_path}: {e}")
                continue

            # Check sequence length
            total_length = metadata.get("total_length", 0)
            if total_length < self.min_seq_length:
                continue

            # Find FASTA file
            fasta_path = json_path.with_suffix(".fasta.gz")
            if not fasta_path.exists():
                fasta_path = json_path.with_suffix(".fasta")
                if not fasta_path.exists():
                    continue

            # Extract taxonomy for all levels
            taxonomy = metadata.get("taxonomy", {})
            labels = self._extract_labels(taxonomy)

            if not labels:
                continue

            # Count classes
            for level, label in labels.items():
                class_counts[level][label] = class_counts[level].get(label, 0) + 1

            all_samples.append({
                "fasta_path": str(fasta_path),
                "metadata": metadata,
                "labels": labels,
            })

        # Second pass: filter classes with too few samples
        valid_classes: dict[str, set[str]] = {}
        for level in self.levels:
            valid_classes[level] = {
                label for label, count in class_counts[level].items()
                if count >= self.min_samples_per_class
            }
            print(
                f"Level {level}: {len(valid_classes[level])} classes "
                f"with >= {self.min_samples_per_class} samples"
            )

        # Third pass: build final dataset with class indices
        final_class_counts: dict[str, dict[str, int]] = {level: {} for level in self.levels}

        for sample in all_samples:
            labels = sample["labels"]

            # Check if all labels are valid
            valid = True
            for level in self.levels:
                if level in labels and labels[level] not in valid_classes[level]:
                    valid = False
                    break

            if not valid:
                continue

            # Check max samples limit (at the finest available level)
            finest_level = None
            finest_label = None
            for level in reversed(self.levels):
                if level in labels:
                    finest_level = level
                    finest_label = labels[level]
                    break

            if finest_level and max_samples_per_class > 0:
                if final_class_counts[finest_level].get(finest_label, 0) >= max_samples_per_class:
                    continue

            # Build label indices
            label_indices = {}
            for level in self.levels:
                if level in labels:
                    label = labels[level]

                    # Add to mapping if new
                    if label not in self.class_to_idx[level]:
                        idx = len(self.class_to_idx[level])
                        self.class_to_idx[level][label] = idx
                        self.idx_to_class[level][idx] = label

                    label_indices[level] = self.class_to_idx[level][label]
                    final_class_counts[level][label] = final_class_counts[level].get(label, 0) + 1
                else:
                    label_indices[level] = -1  # Missing label

            self.samples.append({
                "fasta_path": sample["fasta_path"],
                "metadata": sample["metadata"],
                "labels": labels,
                "label_indices": label_indices,
            })

        # Print summary
        print(f"\nLoaded {len(self.samples)} samples")
        print("\nClasses per level:")
        for level in self.levels:
            num_classes = len(self.class_to_idx[level])
            if num_classes > 0:
                print(f"  {level}: {num_classes} classes")

    def _extract_labels(self, taxonomy: dict) -> dict[str, str]:
        """Extract classification labels from taxonomy dict for all levels."""
        labels = {}

        for level in self.levels:
            label = None

            # Try direct key
            if level in taxonomy:
                label = taxonomy[level]
            # Handle 'class' vs 'class_' naming
            elif level == "class" and "class_" in taxonomy:
                label = taxonomy["class_"]

            if label:
                # Normalize
                label = label.lower().strip()

                # Normalize kingdom variations
                if level == "kingdom":
                    if label in ("metazoa", "animalia"):
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
        except Exception as e:
            print(f"Error reading {path}: {e}")
            return ""

        sequence_parts = []
        for line in lines:
            line = line.strip()
            if line.startswith(">"):
                continue
            sequence_parts.append(line)

        return "".join(sequence_parts)

    def __len__(self) -> int:
        return len(self.samples)

    def __getitem__(self, idx: int) -> dict[str, torch.Tensor]:
        sample = self.samples[idx]

        # Load and encode sequence
        sequence = self._load_sequence(sample["fasta_path"])
        encoded = encode_sequence(sequence, self.max_seq_length)

        # Build label tensor per level
        labels = {}
        for level in self.levels:
            labels[level] = torch.tensor(sample["label_indices"][level], dtype=torch.long)

        return {
            "sequence": encoded,
            "labels": labels,
        }

    @property
    def num_classes_per_level(self) -> dict[str, int]:
        """Get number of classes per taxonomic level."""
        return {level: len(classes) for level, classes in self.class_to_idx.items() if classes}

    @property
    def class_labels_per_level(self) -> dict[str, list[str]]:
        """Get class labels per level."""
        result = {}
        for level in self.levels:
            if self.idx_to_class[level]:
                result[level] = [
                    self.idx_to_class[level][i]
                    for i in range(len(self.idx_to_class[level]))
                ]
        return result

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

        # Inverse frequency weighting
        weights = 1.0 / (class_counts + 1e-6)
        weights = weights / weights.sum() * num_classes

        return weights

    def get_all_class_weights(self) -> dict[str, torch.Tensor]:
        """Compute class weights for all levels."""
        return {
            level: self.get_class_weights(level)
            for level in self.levels
            if self.class_to_idx[level]
        }
