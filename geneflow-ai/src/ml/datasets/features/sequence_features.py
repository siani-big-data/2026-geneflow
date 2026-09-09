"""Feature extraction for DNA/RNA sequences.

Extracts compositional, structural, and statistical features
useful for taxonomy classification and other sequence analysis tasks.
"""

import math
from collections import Counter
from dataclasses import dataclass

import numpy as np


@dataclass
class SequenceFeatures:
    """Container for extracted sequence features."""

    # Basic composition (4 features)
    gc_content: float
    at_content: float
    purine_content: float  # A + G
    pyrimidine_content: float  # C + T

    # Nucleotide frequencies (4 features)
    freq_a: float
    freq_t: float
    freq_c: float
    freq_g: float

    # Skew metrics (2 features)
    gc_skew: float  # (G - C) / (G + C)
    at_skew: float  # (A - T) / (A + T)

    # K-mer features (16 di + 64 tri + 256 tetra = 336 features)
    dinucleotide_freq: dict[str, float]
    trinucleotide_freq: dict[str, float]
    tetranucleotide_freq: dict[str, float] | None  # 4-mers for better taxonomy

    # Complexity metrics (3 features)
    sequence_entropy: float  # Shannon entropy
    linguistic_complexity: float  # Unique kmers / possible kmers
    compression_ratio: float  # Approximation of compressibility

    # Structural features (4 features)
    max_homopolymer_a: int
    max_homopolymer_t: int
    max_homopolymer_c: int
    max_homopolymer_g: int

    # Codon features (for coding sequences, 64 features)
    codon_freq: dict[str, float] | None

    # Windowed GC content stats (4 features)
    gc_mean: float
    gc_std: float
    gc_min: float
    gc_max: float

    def to_array(
        self,
        include_kmers: bool = True,
        include_codons: bool = False,
        include_tetramers: bool = False,
    ) -> np.ndarray:
        """Convert features to numpy array for model input.

        Args:
            include_kmers: Include dinucleotide and trinucleotide frequencies
            include_codons: Include codon frequencies (if available)
            include_tetramers: Include tetranucleotide frequencies (256 features)

        Returns:
            Feature array
        """
        features = [
            # Basic composition
            self.gc_content,
            self.at_content,
            self.purine_content,
            self.pyrimidine_content,
            # Nucleotide frequencies
            self.freq_a,
            self.freq_t,
            self.freq_c,
            self.freq_g,
            # Skew
            self.gc_skew,
            self.at_skew,
            # Complexity
            self.sequence_entropy,
            self.linguistic_complexity,
            self.compression_ratio,
            # Homopolymers
            self.max_homopolymer_a,
            self.max_homopolymer_t,
            self.max_homopolymer_c,
            self.max_homopolymer_g,
            # Windowed GC
            self.gc_mean,
            self.gc_std,
            self.gc_min,
            self.gc_max,
        ]

        if include_kmers:
            # Add dinucleotide frequencies in sorted order
            for kmer in sorted(self.dinucleotide_freq.keys()):
                features.append(self.dinucleotide_freq[kmer])
            # Add trinucleotide frequencies
            for kmer in sorted(self.trinucleotide_freq.keys()):
                features.append(self.trinucleotide_freq[kmer])

        if include_tetramers and self.tetranucleotide_freq:
            # Add tetranucleotide frequencies (256 features)
            for kmer in sorted(self.tetranucleotide_freq.keys()):
                features.append(self.tetranucleotide_freq[kmer])

        if include_codons and self.codon_freq:
            for codon in sorted(self.codon_freq.keys()):
                features.append(self.codon_freq[codon])

        return np.array(features, dtype=np.float32)

    @staticmethod
    def featuREDACTED(
        include_kmers: bool = True,
        include_codons: bool = False,
        include_tetramers: bool = False,
    ) -> list[str]:
        """Get feature names in array order."""
        names = [
            "gc_content",
            "at_content",
            "purine_content",
            "pyrimidine_content",
            "freq_a",
            "freq_t",
            "freq_c",
            "freq_g",
            "gc_skew",
            "at_skew",
            "sequence_entropy",
            "linguistic_complexity",
            "compression_ratio",
            "max_homopolymer_a",
            "max_homopolymer_t",
            "max_homopolymer_c",
            "max_homopolymer_g",
            "gc_mean",
            "gc_std",
            "gc_min",
            "gc_max",
        ]

        nucleotides = ["A", "T", "C", "G"]

        if include_kmers:
            for n1 in nucleotides:
                for n2 in nucleotides:
                    names.append(f"di_{n1}{n2}")
            for n1 in nucleotides:
                for n2 in nucleotides:
                    for n3 in nucleotides:
                        names.append(f"tri_{n1}{n2}{n3}")

        if include_tetramers:
            for n1 in nucleotides:
                for n2 in nucleotides:
                    for n3 in nucleotides:
                        for n4 in nucleotides:
                            names.append(f"tetra_{n1}{n2}{n3}{n4}")

        if include_codons:
            for n1 in nucleotides:
                for n2 in nucleotides:
                    for n3 in nucleotides:
                        names.append(f"codon_{n1}{n2}{n3}")

        return names


class SequenceFeatureExtractor:
    """Extracts features from DNA/RNA sequences."""

    NUCLEOTIDES = {"A", "T", "C", "G"}
    PURINES = {"A", "G"}
    PYRIMIDINES = {"C", "T"}

    def __init__(
        self,
        window_size: int = 100,
        compute_codons: bool = False,
        compute_tetramers: bool = False,
        reading_frame: int = 0,
    ):
        """Initialize feature extractor.

        Args:
            window_size: Window size for windowed statistics
            compute_codons: Whether to compute codon frequencies
            compute_tetramers: Whether to compute tetranucleotide (4-mer) frequencies
            reading_frame: Reading frame for codon analysis (0, 1, or 2)
        """
        self.window_size = window_size
        self.compute_codons = compute_codons
        self.compute_tetramers = compute_tetramers
        self.reading_frame = reading_frame

        # Pre-generate all possible kmers
        self._dinucleotides = self._generate_kmers(2)
        self._trinucleotides = self._generate_kmers(3)
        self._tetranucleotides = self._generate_kmers(4) if compute_tetramers else None
        self._codons = self._trinucleotides if compute_codons else None

    def _generate_kmers(self, k: int) -> list[str]:
        """Generate all possible k-mers."""
        if k == 1:
            return list("ATCG")
        smaller = self._generate_kmers(k - 1)
        return [kmer + nuc for kmer in smaller for nuc in "ATCG"]

    def extract(self, sequence: str) -> SequenceFeatures:
        """Extract all features from a sequence.

        Args:
            sequence: DNA/RNA sequence string

        Returns:
            SequenceFeatures object with all computed features
        """
        # Clean sequence
        seq = sequence.upper().replace("U", "T")
        seq_clean = "".join(c for c in seq if c in self.NUCLEOTIDES)
        length = len(seq_clean)

        if length == 0:
            return self._empty_features()

        # Count nucleotides
        counts = Counter(seq_clean)
        count_a = counts.get("A", 0)
        count_t = counts.get("T", 0)
        count_c = counts.get("C", 0)
        count_g = counts.get("G", 0)

        # Basic composition
        gc_content = (count_g + count_c) / length
        at_content = (count_a + count_t) / length
        purine_content = (count_a + count_g) / length
        pyrimidine_content = (count_c + count_t) / length

        # Nucleotide frequencies
        freq_a = count_a / length
        freq_t = count_t / length
        freq_c = count_c / length
        freq_g = count_g / length

        # Skew metrics
        gc_skew = (count_g - count_c) / (count_g + count_c) if (count_g + count_c) > 0 else 0
        at_skew = (count_a - count_t) / (count_a + count_t) if (count_a + count_t) > 0 else 0

        # K-mer frequencies
        dinuc_freq = self._compute_kmer_freq(seq_clean, 2, self._dinucleotides)
        trinuc_freq = self._compute_kmer_freq(seq_clean, 3, self._trinucleotides)
        tetranuc_freq = None
        if self.compute_tetramers:
            tetranuc_freq = self._compute_kmer_freq(seq_clean, 4, self._tetranucleotides)

        # Complexity metrics
        sequence_entropy = self._compute_entropy(seq_clean)
        linguistic_complexity = self._compute_linguistic_complexity(seq_clean)
        compression_ratio = self._estimate_compression_ratio(seq_clean)

        # Homopolymer runs
        max_hp_a = self._max_homopolymer(seq_clean, "A")
        max_hp_t = self._max_homopolymer(seq_clean, "T")
        max_hp_c = self._max_homopolymer(seq_clean, "C")
        max_hp_g = self._max_homopolymer(seq_clean, "G")

        # Windowed GC statistics
        gc_stats = self._compute_windowed_gc(seq_clean)

        # Codon frequencies (optional)
        codon_freq = None
        if self.compute_codons:
            codon_freq = self._compute_codon_freq(seq_clean)

        return SequenceFeatures(
            gc_content=gc_content,
            at_content=at_content,
            purine_content=purine_content,
            pyrimidine_content=pyrimidine_content,
            freq_a=freq_a,
            freq_t=freq_t,
            freq_c=freq_c,
            freq_g=freq_g,
            gc_skew=gc_skew,
            at_skew=at_skew,
            dinucleotide_freq=dinuc_freq,
            trinucleotide_freq=trinuc_freq,
            tetranucleotide_freq=tetranuc_freq,
            sequence_entropy=sequence_entropy,
            linguistic_complexity=linguistic_complexity,
            compression_ratio=compression_ratio,
            max_homopolymer_a=max_hp_a,
            max_homopolymer_t=max_hp_t,
            max_homopolymer_c=max_hp_c,
            max_homopolymer_g=max_hp_g,
            codon_freq=codon_freq,
            gc_mean=gc_stats["mean"],
            gc_std=gc_stats["std"],
            gc_min=gc_stats["min"],
            gc_max=gc_stats["max"],
        )

    def _compute_kmer_freq(
        self, sequence: str, k: int, all_kmers: list[str]
    ) -> dict[str, float]:
        """Compute normalized k-mer frequencies."""
        counts = Counter(sequence[i : i + k] for i in range(len(sequence) - k + 1))
        total = sum(counts.values())

        if total == 0:
            return {kmer: 0.0 for kmer in all_kmers}

        return {kmer: counts.get(kmer, 0) / total for kmer in all_kmers}

    def _compute_entropy(self, sequence: str) -> float:
        """Compute Shannon entropy of the sequence."""
        if len(sequence) == 0:
            return 0.0

        counts = Counter(sequence)
        total = len(sequence)
        entropy = 0.0

        for count in counts.values():
            if count > 0:
                p = count / total
                entropy -= p * math.log2(p)

        # Normalize to [0, 1] (max entropy for 4 symbols is 2 bits)
        return entropy / 2.0

    def _compute_linguistic_complexity(self, sequence: str, k: int = 3) -> float:
        """Compute linguistic complexity as ratio of unique k-mers to possible k-mers."""
        if len(sequence) < k:
            return 0.0

        unique_kmers = set(sequence[i : i + k] for i in range(len(sequence) - k + 1))
        possible_kmers = min(len(sequence) - k + 1, 4**k)

        return len(unique_kmers) / possible_kmers if possible_kmers > 0 else 0.0

    def _estimate_compression_ratio(self, sequence: str, window: int = 12) -> float:
        """Estimate compression ratio using repeat counting.

        Higher values indicate more repetitive/compressible sequences.
        """
        if len(sequence) < window:
            return 0.0

        # Count repeated windows
        windows = [sequence[i : i + window] for i in range(len(sequence) - window + 1)]
        window_counts = Counter(windows)

        # Ratio of repeated windows
        repeated = sum(1 for count in window_counts.values() if count > 1)
        return repeated / len(windows) if windows else 0.0

    def _max_homopolymer(self, sequence: str, nucleotide: str) -> int:
        """Find maximum homopolymer run length for a nucleotide."""
        max_run = 0
        current_run = 0

        for c in sequence:
            if c == nucleotide:
                current_run += 1
                max_run = max(max_run, current_run)
            else:
                current_run = 0

        return max_run

    def _compute_windowed_gc(self, sequence: str) -> dict[str, float]:
        """Compute GC content statistics across windows."""
        if len(sequence) < self.window_size:
            gc = sum(1 for c in sequence if c in "GC") / len(sequence) if sequence else 0
            return {"mean": gc, "std": 0.0, "min": gc, "max": gc}

        gc_values = []
        for i in range(0, len(sequence) - self.window_size + 1, self.window_size // 2):
            window = sequence[i : i + self.window_size]
            gc = sum(1 for c in window if c in "GC") / len(window)
            gc_values.append(gc)

        gc_array = np.array(gc_values)
        return {
            "mean": float(np.mean(gc_array)),
            "std": float(np.std(gc_array)),
            "min": float(np.min(gc_array)),
            "max": float(np.max(gc_array)),
        }

    def _compute_codon_freq(self, sequence: str) -> dict[str, float]:
        """Compute codon frequencies in the specified reading frame."""
        start = self.reading_frame
        codons = [
            sequence[i : i + 3]
            for i in range(start, len(sequence) - 2, 3)
            if len(sequence[i : i + 3]) == 3
        ]

        # Filter valid codons
        valid_codons = [c for c in codons if all(n in self.NUCLEOTIDES for n in c)]
        total = len(valid_codons)

        if total == 0:
            return {codon: 0.0 for codon in self._codons}

        counts = Counter(valid_codons)
        return {codon: counts.get(codon, 0) / total for codon in self._codons}

    def _empty_features(self) -> SequenceFeatures:
        """Return empty features for invalid sequences."""
        return SequenceFeatures(
            gc_content=0.0,
            at_content=0.0,
            purine_content=0.0,
            pyrimidine_content=0.0,
            freq_a=0.0,
            freq_t=0.0,
            freq_c=0.0,
            freq_g=0.0,
            gc_skew=0.0,
            at_skew=0.0,
            dinucleotide_freq={k: 0.0 for k in self._dinucleotides},
            trinucleotide_freq={k: 0.0 for k in self._trinucleotides},
            tetranucleotide_freq=(
                {k: 0.0 for k in self._tetranucleotides}
                if self._tetranucleotides
                else None
            ),
            sequence_entropy=0.0,
            linguistic_complexity=0.0,
            compression_ratio=0.0,
            max_homopolymer_a=0,
            max_homopolymer_t=0,
            max_homopolymer_c=0,
            max_homopolymer_g=0,
            codon_freq={k: 0.0 for k in self._codons} if self._codons else None,
            gc_mean=0.0,
            gc_std=0.0,
            gc_min=0.0,
            gc_max=0.0,
        )

    def extract_batch(self, sequences: list[str]) -> np.ndarray:
        """Extract features from multiple sequences.

        Args:
            sequences: List of DNA sequences

        Returns:
            Array of shape (n_sequences, n_features)
        """
        features = [
            self.extract(seq).to_array(
                include_codons=self.compute_codons,
                include_tetramers=self.compute_tetramers,
            )
            for seq in sequences
        ]
        return np.stack(features)
