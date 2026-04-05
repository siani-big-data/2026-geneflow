"""Dataset for loading DNA sequences from the datalake for taxonomy classification."""

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


def encode_sequence(sequence: str, max_length: int = 2000) -> torch.Tensor:
    """Encode DNA sequence to tensor of indices.

    Args:
        sequence: DNA sequence string (A, T, C, G, N)
        max_length: Maximum sequence length (truncate or pad)

    Returns:
        Tensor of shape (max_length,) with nucleotide indices
    """
    # Truncate if needed
    if len(sequence) > max_length:
        # Take from middle for more representative sample
        start = (len(sequence) - max_length) // 2
        sequence = sequence[start : start + max_length]

    # Convert to indices
    indices = []
    for nuc in sequence:
        idx = NUCLEOTIDE_MAP.get(nuc, 4)  # Unknown -> N (4)
        indices.append(idx)

    # Pad if needed
    while len(indices) < max_length:
        indices.append(4)  # Pad with N

    return torch.tensor(indices, dtype=torch.long)


class TaxonomyDataset(Dataset):
    """Dataset that loads DNA sequences from the datalake structure.

    Expects structure:
        data_dir/
            kingdom1/
                phylum/class/order/family/genus/species/
                    taxon_id.fasta.gz
                    taxon_id.json
            kingdom2/
                ...

    The classification level can be set to: kingdom, phylum, class, order, family, genus, species
    """

    def __init__(
        self,
        data_dir: str | Path,
        classification_level: str = "kingdom",
        max_seq_length: int = 2000,
        max_samples_per_class: int = 0,  # 0 = unlimited
        min_seq_length: int = 100,
    ):
        """Initialize the dataset.

        Args:
            data_dir: Root directory containing taxonomy folders
            classification_level: Which level to classify (kingdom, phylum, etc.)
            max_seq_length: Maximum sequence length to use
            max_samples_per_class: Limit samples per class (0 = unlimited)
            min_seq_length: Minimum sequence length to include
        """
        self.data_dir = Path(data_dir)
        self.classification_level = classification_level
        self.max_seq_length = max_seq_length
        self.min_seq_length = min_seq_length

        # Find all samples
        self.samples: list[dict[str, Any]] = []
        self.class_to_idx: dict[str, int] = {}
        self.idx_to_class: dict[int, str] = {}

        self._load_samples(max_samples_per_class)

    def _load_samples(self, max_samples_per_class: int):
        """Scan the data directory and load sample metadata."""
        class_counts: dict[str, int] = {}

        # Find all .json files (metadata)
        json_files = list(self.data_dir.rglob("*.json"))
        print(f"Found {len(json_files)} sequence metadata files")

        for json_path in json_files:
            # Load metadata
            try:
                with open(json_path, "r") as f:
                    metadata = json.load(f)
            except Exception as e:
                print(f"Error reading {json_path}: {e}")
                continue

            # Get classification label from taxonomy
            taxonomy = metadata.get("taxonomy", {})
            label = self._get_label_from_taxonomy(taxonomy)

            if not label:
                continue

            # Check if we've hit the limit for this class
            if max_samples_per_class > 0:
                if class_counts.get(label, 0) >= max_samples_per_class:
                    continue

            # Check sequence length
            total_length = metadata.get("total_length", 0)
            if total_length < self.min_seq_length:
                continue

            # Find corresponding FASTA file
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
                "metadata": metadata,
                "label": label,
                "label_idx": self.class_to_idx[label],
            })

            class_counts[label] = class_counts.get(label, 0) + 1

        print(f"Loaded {len(self.samples)} samples across {len(self.class_to_idx)} classes")
        for label, count in sorted(class_counts.items()):
            print(f"  {label}: {count} samples")

    def _get_label_from_taxonomy(self, taxonomy: dict) -> str | None:
        """Extract classification label from taxonomy dict."""
        level = self.classification_level

        # Try direct key first
        if level in taxonomy:
            label = taxonomy[level]
            if label:
                return label.lower()

        # For kingdom, also check 'kingdom' field which might say 'Metazoa' instead of 'animalia'
        if level == "kingdom":
            kingdom = taxonomy.get("kingdom", "")
            if kingdom:
                # Normalize common variations
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

        # Parse FASTA - skip header lines (start with >)
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

        # Load sequence
        sequence = self._load_sequence(sample["fasta_path"])

        # Encode
        encoded = encode_sequence(sequence, self.max_seq_length)

        return {
            "sequence": encoded,
            "label": torch.tensor(sample["label_idx"], dtype=torch.long),
        }

    @property
    def num_classes(self) -> int:
        return len(self.class_to_idx)

    @property
    def class_labels(self) -> list[str]:
        return [self.idx_to_class[i] for i in range(len(self.idx_to_class))]

    def get_class_weights(self) -> torch.Tensor:
        """Compute class weights for imbalanced datasets."""
        class_counts = torch.zeros(self.num_classes)
        for sample in self.samples:
            class_counts[sample["label_idx"]] += 1

        # Inverse frequency weighting
        weights = 1.0 / (class_counts + 1e-6)
        weights = weights / weights.sum() * self.num_classes

        return weights
