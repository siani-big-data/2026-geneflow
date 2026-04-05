"""Sequence augmentation for balancing taxonomy datasets.

Generates additional sequences for species with few samples by applying
biologically-plausible transformations.
"""

import gzip
import json
import logging
import random
from dataclasses import dataclass
from pathlib import Path
from typing import Iterator

logger = logging.getLogger(__name__)


@dataclass
class AugmentationConfig:
    """Configuration for sequence augmentation."""
    # Target samples per species
    min_samples_per_species: int = 10
    max_augmented_per_original: int = 5

    # Mutation parameters
    mutation_rate: float = 0.02  # 2% of bases mutated
    indel_rate: float = 0.005  # 0.5% indel probability

    # Subsequence parameters
    min_subseq_ratio: float = 0.7  # Minimum 70% of original length
    max_subseq_ratio: float = 0.95  # Maximum 95% of original length

    # Include reverse complement
    include_reverse_complement: bool = True

    # Random seed
    seed: int = 42


class SequenceAugmenter:
    """Augment DNA sequences for dataset balancing.

    Applies biologically-plausible transformations:
    - Point mutations (SNPs)
    - Small insertions/deletions
    - Subsequence extraction
    - Reverse complement
    """

    BASES = ["A", "C", "G", "T"]
    COMPLEMENT = {"A": "T", "T": "A", "G": "C", "C": "G", "N": "N"}

    def __init__(self, config: AugmentationConfig | None = None):
        self.config = config or AugmentationConfig()
        random.seed(self.config.seed)

    def mutate(self, sequence: str, mutation_rate: float | None = None) -> str:
        """Apply random point mutations.

        Args:
            sequence: Original DNA sequence
            mutation_rate: Fraction of bases to mutate (default from config)

        Returns:
            Mutated sequence
        """
        rate = mutation_rate or self.config.mutation_rate
        seq_list = list(sequence.upper())

        for i in range(len(seq_list)):
            if random.random() < rate:
                # Mutate to a different base
                current = seq_list[i]
                if current in self.BASES:
                    others = [b for b in self.BASES if b != current]
                    seq_list[i] = random.choice(others)

        return "".join(seq_list)

    def add_indels(self, sequence: str, indel_rate: float | None = None) -> str:
        """Apply random insertions and deletions.

        Args:
            sequence: Original DNA sequence
            indel_rate: Probability of indel at each position

        Returns:
            Sequence with indels
        """
        rate = indel_rate or self.config.indel_rate
        result = []

        for base in sequence.upper():
            if random.random() < rate:
                # 50% insertion, 50% deletion
                if random.random() < 0.5:
                    # Insertion: add random base before current
                    result.append(random.choice(self.BASES))
                    result.append(base)
                else:
                    # Deletion: skip current base
                    pass
            else:
                result.append(base)

        return "".join(result)

    def reverse_complement(self, sequence: str) -> str:
        """Get reverse complement of sequence.

        Args:
            sequence: Original DNA sequence

        Returns:
            Reverse complement
        """
        complement = [self.COMPLEMENT.get(b, "N") for b in sequence.upper()]
        return "".join(reversed(complement))

    def extract_subsequence(
        self,
        sequence: str,
        min_ratio: float | None = None,
        max_ratio: float | None = None,
    ) -> str:
        """Extract a random subsequence.

        Args:
            sequence: Original DNA sequence
            min_ratio: Minimum length ratio
            max_ratio: Maximum length ratio

        Returns:
            Random subsequence
        """
        min_r = min_ratio or self.config.min_subseq_ratio
        max_r = max_ratio or self.config.max_subseq_ratio

        seq_len = len(sequence)
        subseq_len = int(seq_len * random.uniform(min_r, max_r))

        # Random start position
        max_start = seq_len - subseq_len
        if max_start <= 0:
            return sequence

        start = random.randint(0, max_start)
        return sequence[start:start + subseq_len]

    def augment_sequence(self, sequence: str, n_augmentations: int = 1) -> list[str]:
        """Generate augmented versions of a sequence.

        Args:
            sequence: Original DNA sequence
            n_augmentations: Number of augmented sequences to generate

        Returns:
            List of augmented sequences
        """
        augmented = []

        for i in range(n_augmentations):
            # Randomly choose augmentation strategy
            strategy = random.choice([
                "mutate",
                "mutate_indel",
                "subsequence",
                "subsequence_mutate",
                "reverse_complement",
                "reverse_complement_mutate",
            ])

            if strategy == "mutate":
                aug = self.mutate(sequence)
            elif strategy == "mutate_indel":
                aug = self.mutate(sequence)
                aug = self.add_indels(aug)
            elif strategy == "subsequence":
                aug = self.extract_subsequence(sequence)
            elif strategy == "subsequence_mutate":
                aug = self.extract_subsequence(sequence)
                aug = self.mutate(aug)
            elif strategy == "reverse_complement":
                aug = self.reverse_complement(sequence)
            elif strategy == "reverse_complement_mutate":
                aug = self.reverse_complement(sequence)
                aug = self.mutate(aug)
            else:
                aug = self.mutate(sequence)

            augmented.append(aug)

        return augmented

    def process_species_file(
        self,
        fasta_path: Path,
        target_count: int,
    ) -> list[tuple[str, str]]:
        """Process a species FASTA file and augment if needed.

        Args:
            fasta_path: Path to .fasta.gz file
            target_count: Target number of sequences

        Returns:
            List of (header, sequence) tuples including augmented
        """
        # Read existing sequences
        sequences = []
        try:
            with gzip.open(fasta_path, "rt") as f:
                current_header = ""
                current_seq = []

                for line in f:
                    line = line.strip()
                    if line.startswith(">"):
                        if current_seq:
                            sequences.append((current_header, "".join(current_seq)))
                        current_header = line[1:]
                        current_seq = []
                    else:
                        current_seq.append(line)

                if current_seq:
                    sequences.append((current_header, "".join(current_seq)))

        except Exception as e:
            logger.warning(f"Error reading {fasta_path}: {e}")
            return []

        current_count = len(sequences)
        if current_count >= target_count:
            return sequences  # No augmentation needed

        # Calculate how many to augment
        needed = target_count - current_count
        augmentations_per_seq = min(
            self.config.max_augmented_per_original,
            (needed // current_count) + 1,
        )

        # Generate augmented sequences
        augmented_sequences = []
        aug_count = 0

        for header, seq in sequences:
            if aug_count >= needed:
                break

            n_aug = min(augmentations_per_seq, needed - aug_count)
            for i, aug_seq in enumerate(self.augment_sequence(seq, n_aug)):
                aug_header = f"{header} [augmented:{i+1}]"
                augmented_sequences.append((aug_header, aug_seq))
                aug_count += 1

        return sequences + augmented_sequences

    def augment_datalake(
        self,
        raw_dir: Path,
        output_dir: Path,
        target_per_species: int | None = None,
        max_species: int | None = None,
    ) -> dict:
        """Augment entire datalake.

        Args:
            raw_dir: Path to datalake/raw
            output_dir: Output directory for augmented data
            target_per_species: Target sequences per species
            max_species: Maximum species to process

        Returns:
            Statistics dictionary
        """
        target = target_per_species or self.config.min_samples_per_species
        output_dir.mkdir(parents=True, exist_ok=True)

        # Find all FASTA files
        fasta_files = list(raw_dir.rglob("*.fasta.gz"))
        if max_species:
            fasta_files = fasta_files[:max_species]

        logger.info(f"Processing {len(fasta_files)} species for augmentation...")

        stats = {
            "total_species": 0,
            "species_augmented": 0,
            "original_sequences": 0,
            "augmented_sequences": 0,
            "total_sequences": 0,
        }

        for i, fasta_path in enumerate(fasta_files):
            try:
                # Get relative path for output
                rel_path = fasta_path.relative_to(raw_dir)
                out_path = output_dir / rel_path

                # Read and count original
                with gzip.open(fasta_path, "rt") as f:
                    original_count = sum(1 for line in f if line.startswith(">"))

                stats["original_sequences"] += original_count

                # Process and augment
                sequences = self.process_species_file(fasta_path, target)

                if len(sequences) > original_count:
                    stats["species_augmented"] += 1
                    stats["augmented_sequences"] += len(sequences) - original_count

                stats["total_sequences"] += len(sequences)
                stats["total_species"] += 1

                # Save augmented file
                out_path.parent.mkdir(parents=True, exist_ok=True)
                with gzip.open(out_path, "wt") as f:
                    for header, seq in sequences:
                        f.write(f">{header}\n")
                        # Write sequence in 80-char lines
                        for j in range(0, len(seq), 80):
                            f.write(seq[j:j+80] + "\n")

                # Copy metadata file
                meta_path = fasta_path.with_suffix("").with_suffix(".json")
                if meta_path.exists():
                    out_meta = out_path.with_suffix("").with_suffix(".json")
                    with open(meta_path) as f:
                        meta = json.load(f)
                    meta["augmented"] = True
                    meta["original_count"] = original_count
                    meta["augmented_count"] = len(sequences)
                    with open(out_meta, "w") as f:
                        json.dump(meta, f, indent=2)

                if (i + 1) % 500 == 0:
                    logger.info(
                        f"Processed {i + 1}/{len(fasta_files)} species, "
                        f"{stats['augmented_sequences']:,} new sequences"
                    )

            except Exception as e:
                logger.warning(f"Error processing {fasta_path}: {e}")

        return stats
