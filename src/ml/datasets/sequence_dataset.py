"""Dataset for DNA/RNA sequences."""

from pathlib import Path
from typing import Iterator

import numpy as np
import torch
from torch.utils.data import Dataset


class SequenceDataset(Dataset):
    """PyTorch Dataset for DNA/RNA sequences."""

    # Nucleotide encoding
    NUC_TO_IDX = {"A": 0, "T": 1, "C": 2, "G": 3, "U": 1, "N": 4}
    IDX_TO_NUC = {0: "A", 1: "T", 2: "C", 3: "G", 4: "N"}

    def __init__(
        self,
        fasta_file: str | Path | None = None,
        sequences: list[str] | None = None,
        labels: list | None = None,
        max_length: int = 1000,
        one_hot: bool = True,
    ):
        self.max_length = max_length
        self.one_hot = one_hot
        self.sequences: list[str] = []
        self.labels: list = []

        if fasta_file:
            self._load_fasta(Path(fasta_file))
        elif sequences:
            self.sequences = sequences
            self.labels = labels or [None] * len(sequences)

    def _load_fasta(self, path: Path) -> None:
        """Load sequences from FASTA file."""
        current_seq = []
        current_header = ""

        with open(path) as f:
            for line in f:
                line = line.strip()
                if line.startswith(">"):
                    if current_seq:
                        self.sequences.append("".join(current_seq))
                        self.labels.append(current_header)
                    current_header = line[1:]
                    current_seq = []
                else:
                    current_seq.append(line.upper())

            if current_seq:
                self.sequences.append("".join(current_seq))
                self.labels.append(current_header)

    def __len__(self) -> int:
        return len(self.sequences)

    def encode_sequence(self, seq: str) -> np.ndarray:
        """Encode sequence to indices."""
        indices = np.array([self.NUC_TO_IDX.get(c, 4) for c in seq.upper()])
        return indices

    def to_one_hot(self, indices: np.ndarray) -> np.ndarray:
        """Convert indices to one-hot encoding."""
        one_hot = np.zeros((len(indices), 5), dtype=np.float32)
        one_hot[np.arange(len(indices)), indices] = 1.0
        return one_hot[:, :4]  # Drop N column

    def __getitem__(self, idx: int) -> dict[str, torch.Tensor]:
        """Get encoded sequence."""
        seq = self.sequences[idx]
        seq_len = min(len(seq), self.max_length)

        # Encode
        indices = self.encode_sequence(seq[:seq_len])

        # Pad
        if seq_len < self.max_length:
            pad_len = self.max_length - seq_len
            indices = np.pad(indices, (0, pad_len), constant_values=4)
            mask = np.concatenate([np.ones(seq_len), np.zeros(pad_len)])
        else:
            mask = np.ones(self.max_length)

        result = {
            "indices": torch.tensor(indices, dtype=torch.long),
            "mask": torch.tensor(mask, dtype=torch.bool),
            "length": torch.tensor(seq_len, dtype=torch.long),
        }

        if self.one_hot:
            one_hot = self.to_one_hot(indices)
            result["one_hot"] = torch.tensor(one_hot, dtype=torch.float32)

        if self.labels[idx] is not None:
            label = self.labels[idx]
            if isinstance(label, (int, float)):
                result["label"] = torch.tensor(label, dtype=torch.float32)

        return result

    def iter_sequences(self) -> Iterator[tuple[str, str]]:
        """Iterate over (sequence, label) pairs."""
        for seq, label in zip(self.sequences, self.labels):
            yield seq, label
