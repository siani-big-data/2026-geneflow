"""Dataset for FASTQ files with quality_enhanced scores.

FASTQ format:
    @read_id
    SEQUENCE
    +
    QUALITY_STRING (Phred+33 encoded)

This dataset converts sequences to one-hot encoding (4 channels)
compatible with the QualityPredictor model.
"""

import gzip
from dataclasses import dataclass
from pathlib import Path
from typing import Iterator

import numpy as np
import torch
from torch.utils.data import Dataset


@dataclass
class FastqRecord:
    """A single FASTQ record."""

    id: str
    sequence: str
    quality_scores: np.ndarray  # Phred scores (0-40+)

    @property
    def length(self) -> int:
        return len(self.sequence)

    @property
    def mean_quality(self) -> float:
        return float(np.mean(self.quality_scores)) if len(self.quality_scores) > 0 else 0.0


def parse_fastq(file_path: Path, max_records: int | None = None) -> Iterator[FastqRecord]:
    """Parse FASTQ file and yield records.

    Args:
        file_path: Path to .fastq or .fastq.gz file
        max_records: Maximum records to parse

    Yields:
        FastqRecord objects
    """
    opener = gzip.open if str(file_path).endswith('.gz') else open
    mode = 'rt' if str(file_path).endswith('.gz') else 'r'

    count = 0
    with opener(file_path, mode) as f:
        while True:
            if max_records and count >= max_records:
                break

            # Read 4 lines
            header = f.readline().strip()
            if not header:
                break
            if not header.startswith('@'):
                continue

            sequence = f.readline().strip().upper()
            f.readline()  # + line
            quality_str = f.readline().strip()

            if len(sequence) != len(quality_str):
                continue

            # Decode Phred+33 quality_enhanced scores
            quality_scores = np.array([ord(c) - 33 for c in quality_str], dtype=np.float32)

            yield FastqRecord(
                id=header[1:].split()[0],
                sequence=sequence,
                quality_scores=quality_scores,
            )
            count += 1


class FastqDataset(Dataset):
    """PyTorch Dataset for FASTQ files.

    Converts sequences to one-hot encoding (4 channels: A, T, C, G)
    compatible with QualityPredictor model.

    Features:
    - Sequence as one-hot (4, seq_len)
    - Quality scores as target (seq_len,)
    - Auxiliary features from sequence composition
    """

    # Nucleotide to channel index
    NUC_TO_IDX = {'A': 0, 'T': 1, 'C': 2, 'G': 3}

    def __init__(
        self,
        fastq_dir: str | Path,
        max_length: int = 300,  # NGS reads are typically shorter
        min_length: int = 50,
        max_records_per_file: int | None = None,
        include_aux_features: bool = True,
    ):
        """Initialize dataset.

        Args:
            fastq_dir: Directory with .fastq files
            max_length: Maximum sequence length
            min_length: Minimum sequence length
            max_records_per_file: Limit records per file
            include_aux_features: Include auxiliary features
        """
        self.fastq_dir = Path(fastq_dir)
        self.max_length = max_length
        self.min_length = min_length
        self.max_records_per_file = max_records_per_file
        self.include_aux_features = include_aux_features

        # Load all records
        self.records: list[FastqRecord] = []
        self._load_records()

    def _load_records(self) -> None:
        """Load records from all FASTQ files."""
        patterns = ['*.fastq', '*.fastq.gz', '*.fq', '*.fq.gz']

        for pattern in patterns:
            for fastq_file in self.fastq_dir.glob(pattern):
                for record in parse_fastq(fastq_file, self.max_records_per_file):
                    if self.min_length <= record.length:
                        self.records.append(record)

        print(f"Loaded {len(self.records)} FASTQ records")

    def __len__(self) -> int:
        return len(self.records)

    def _sequence_to_onehot(self, sequence: str) -> np.ndarray:
        """Convert sequence to one-hot encoding (4, seq_len)."""
        seq_len = min(len(sequence), self.max_length)
        onehot = np.zeros((4, self.max_length), dtype=np.float32)

        for i, nuc in enumerate(sequence[:seq_len]):
            if nuc in self.NUC_TO_IDX:
                onehot[self.NUC_TO_IDX[nuc], i] = 1.0

        return onehot

    def _extract_aux_features(self, sequence: str, quality: np.ndarray) -> np.ndarray:
        """Extract auxiliary features per position.

        Features (8 channels):
        - Local GC content (window)
        - Is in homopolymer run
        - Distance to sequence end
        - Local quality_enhanced gradient
        - Is N/ambiguous
        - Nucleotide entropy (window)
        - Quality z-score
        - Position normalized
        """
        seq_len = min(len(sequence), self.max_length)
        aux = np.zeros((8, self.max_length), dtype=np.float32)

        window = 5
        half_win = window // 2

        for i in range(seq_len):
            # Window boundaries
            start = max(0, i - half_win)
            end = min(seq_len, i + half_win + 1)
            window_seq = sequence[start:end]

            # Local GC content
            gc = sum(1 for c in window_seq if c in 'GC') / len(window_seq)
            aux[0, i] = gc

            # Homopolymer detection
            if i > 0 and sequence[i] == sequence[i - 1]:
                aux[1, i] = 1.0

            # Distance to end (normalized)
            dist_to_end = min(i, seq_len - 1 - i) / (seq_len / 2)
            aux[2, i] = dist_to_end

            # Local quality_enhanced gradient
            if i > 0 and i < len(quality) - 1:
                grad = abs(quality[min(i + 1, len(quality) - 1)] - quality[max(i - 1, 0)]) / 2
                aux[3, i] = grad / 40.0  # Normalize

            # Is ambiguous (N)
            aux[4, i] = 1.0 if sequence[i] == 'N' else 0.0

            # Local nucleotide entropy
            counts = [window_seq.count(n) for n in 'ATCG']
            total = sum(counts)
            if total > 0:
                probs = [c / total for c in counts if c > 0]
                entropy = -sum(p * np.log2(p) for p in probs) / 2.0  # Normalize
                aux[5, i] = entropy

            # Quality z-score (local)
            if len(quality) > 0:
                mean_q = np.mean(quality[:seq_len])
                std_q = np.std(quality[:seq_len]) + 1e-6
                if i < len(quality):
                    aux[6, i] = (quality[i] - mean_q) / std_q

            # Position normalized
            aux[7, i] = i / self.max_length

        return aux

    def __getitem__(self, idx: int) -> dict[str, torch.Tensor]:
        """Get a training sample.

        Returns:
            Dictionary with:
            - signals: (4, max_length) one-hot encoded sequence
            - aux_features: (8, max_length) auxiliary features
            - quality_enhanced: (max_length,) target quality_enhanced scores
            - mask: (max_length,) valid position mask
        """
        record = self.records[idx]
        seq_len = min(record.length, self.max_length)

        # One-hot encode sequence
        signals = self._sequence_to_onehot(record.sequence)

        # Prepare quality_enhanced scores
        quality = np.zeros(self.max_length, dtype=np.float32)
        quality[:seq_len] = record.quality_scores[:seq_len]

        # Create mask
        mask = np.zeros(self.max_length, dtype=bool)
        mask[:seq_len] = True

        output = {
            "signals": torch.tensor(signals, dtype=torch.float32),
            "quality_enhanced": torch.tensor(quality, dtype=torch.float32),
            "mask": torch.tensor(mask, dtype=torch.bool),
        }

        if self.include_aux_features:
            aux = self._extract_aux_features(record.sequence, record.quality_scores)
            output["aux_features"] = torch.tensor(aux, dtype=torch.float32)

        return output

    def compute_statistics(self) -> dict:
        """Compute dataset statistics."""
        qualities = []
        lengths = []

        for record in self.records:
            qualities.extend(record.quality_scores.tolist())
            lengths.append(record.length)

        return {
            "num_records": len(self.records),
            "total_bases": sum(lengths),
            "mean_length": float(np.mean(lengths)),
            "mean_quality": float(np.mean(qualities)),
            "std_quality": float(np.std(qualities)),
            "min_quality": float(np.min(qualities)),
            "max_quality": float(np.max(qualities)),
        }


class FastqDatasetBuilder:
    """Build train/val splits from FASTQ files."""

    def __init__(
        self,
        max_length: int = 300,
        val_split: float = 0.1,
        seed: int = 42,
    ):
        self.max_length = max_length
        self.val_split = val_split
        self.seed = seed

    def build(
        self,
        input_dir: Path,
        output_dir: Path,
        max_records: int | None = None,
    ) -> dict:
        """Build precomputed dataset.

        Args:
            input_dir: Directory with FASTQ files
            output_dir: Output directory

        Returns:
            Dataset statistics
        """
        import json
        import random

        random.seed(self.seed)

        # Load all records
        records = []
        for pattern in ['*.fastq', '*.fastq.gz', '*.fq', '*.fq.gz']:
            for fastq_file in input_dir.glob(pattern):
                for record in parse_fastq(fastq_file):
                    if record.length >= 50:
                        records.append(record)

        if max_records and len(records) > max_records:
            random.shuffle(records)
            records = records[:max_records]

        print(f"Loaded {len(records)} records")

        # Split
        random.shuffle(records)
        split_idx = int(len(records) * (1 - self.val_split))
        train_records = records[:split_idx]
        val_records = records[split_idx:]

        # Save
        train_dir = output_dir / "train"
        val_dir = output_dir / "val"
        train_dir.mkdir(parents=True, exist_ok=True)
        val_dir.mkdir(parents=True, exist_ok=True)

        self._save_records(train_records, train_dir)
        self._save_records(val_records, val_dir)

        # Statistics
        all_qualities = []
        for r in records:
            all_qualities.extend(r.quality_scores.tolist())

        stats = {
            "total_records": len(records),
            "train_records": len(train_records),
            "val_records": len(val_records),
            "mean_quality": float(np.mean(all_qualities)),
            "std_quality": float(np.std(all_qualities)),
            "max_length": self.max_length,
        }

        with open(output_dir / "metadata.json", "w") as f:
            json.dump(stats, f, indent=2)

        return stats

    def _save_records(self, records: list[FastqRecord], output_dir: Path) -> None:
        """Save records as .npz files."""
        for i, record in enumerate(records):
            seq_len = min(record.length, self.max_length)

            # One-hot
            onehot = np.zeros((4, self.max_length), dtype=np.float32)
            for j, nuc in enumerate(record.sequence[:seq_len]):
                if nuc in FastqDataset.NUC_TO_IDX:
                    onehot[FastqDataset.NUC_TO_IDX[nuc], j] = 1.0

            # Quality
            quality = np.zeros(self.max_length, dtype=np.float32)
            quality[:seq_len] = record.quality_scores[:seq_len]

            # Mask
            mask = np.zeros(self.max_length, dtype=bool)
            mask[:seq_len] = True

            np.savez_compressed(
                output_dir / f"sample_{i:06d}.npz",
                signals=onehot,
                quality=quality,
                mask=mask,
            )
