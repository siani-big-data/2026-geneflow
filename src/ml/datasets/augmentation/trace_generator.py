"""Synthetic trace generator for data augmentation.

Generates realistic chromatogram traces from DNA sequences for training
the QualityPredictor model without requiring real .ab1 files.
"""

import gzip
import json
import logging
import random
from dataclasses import dataclass
from pathlib import Path
from typing import Iterator

import numpy as np

logger = logging.getLogger(__name__)


@dataclass
class TraceGeneratorConfig:
    """Configuration for trace generation."""
    # Peak parameters
    peak_height_mean: float = 1000.0
    peak_height_std: float = 200.0
    peak_width: float = 5.0  # Standard deviation of Gaussian peak

    # Spacing
    base_spacing: int = 12  # Points between peaks

    # Noise
    baseline_noise_std: float = 30.0
    signal_noise_std: float = 50.0

    # Quality degradation
    edge_degradation_length: int = 50  # Bases at edges with lower quality_enhanced
    edge_quality_factor: float = 0.5  # Multiply quality_enhanced at edges

    # Heterozygote simulation
    heterozygote_rate: float = 0.01  # 1% of positions
    secondary_peak_ratio: float = 0.6  # Height of secondary peak

    # Variations per sequence
    variations_per_sequence: int = 3

    # Quality score parameters
    min_quality: int = 5
    max_quality: int = 60


@dataclass
class SyntheticTrace:
    """A synthetic chromatogram trace."""
    sequence: str
    quality_scores: list[int]
    trace_a: np.ndarray
    trace_c: np.ndarray
    trace_g: np.ndarray
    trace_t: np.ndarray
    peak_locations: list[int]

    # Metadata
    source_file: str = ""
    species_name: str = ""
    taxon_id: int = 0
    variation_id: int = 0

    # Computed metrics
    mean_quality: float = 0.0
    q20_percentage: float = 0.0
    q30_percentage: float = 0.0

    def compute_metrics(self):
        """Compute quality_enhanced metrics."""
        if self.quality_scores:
            self.mean_quality = np.mean(self.quality_scores)
            q_len = len(self.quality_scores)
            self.q20_percentage = sum(1 for q in self.quality_scores if q >= 20) / q_len * 100
            self.q30_percentage = sum(1 for q in self.quality_scores if q >= 30) / q_len * 100

    def to_dict(self) -> dict:
        """Convert to dictionary for saving."""
        return {
            "sequence": self.sequence,
            "quality_scores": self.quality_scores,
            "peak_locations": self.peak_locations,
            "source_file": self.source_file,
            "species_name": self.species_name,
            "taxon_id": self.taxon_id,
            "variation_id": self.variation_id,
            "mean_quality": self.mean_quality,
            "q20_percentage": self.q20_percentage,
            "q30_percentage": self.q30_percentage,
            "trace_length": len(self.trace_a),
        }

    def get_signals(self) -> np.ndarray:
        """Get 4-channel signal array (4, length)."""
        return np.stack([self.trace_a, self.trace_c, self.trace_g, self.trace_t])


class TraceGenerator:
    """Generate synthetic chromatogram traces from DNA sequences.

    Creates realistic trace data with:
    - Gaussian-shaped peaks for each base
    - Realistic noise patterns
    - Quality degradation at sequence edges
    - Optional heterozygote_training simulation
    - Multiple variations per input sequence
    """

    BASE_INDEX = {"A": 0, "C": 1, "G": 2, "T": 3}

    def __init__(self, config: TraceGeneratorConfig | None = None, seed: int | None = None):
        self.config = config or TraceGeneratorConfig()
        self.rng = np.random.default_rng(seed)
        if seed is not None:
            random.seed(seed)

    def generate_trace(
        self,
        sequence: str,
        variation_id: int = 0,
        noise_level: float = 1.0,
        quality_factor: float = 1.0,
    ) -> SyntheticTrace:
        """Generate a synthetic trace from a DNA sequence.

        Args:
            sequence: DNA sequence (A, C, G, T)
            variation_id: ID for this variation
            noise_level: Multiplier for noise (0.5 = low noise, 2.0 = high noise)
            quality_factor: Multiplier for quality_enhanced
                (0.5 = low quality_enhanced, 1.5 = high quality_enhanced)

        Returns:
            SyntheticTrace with chromatogram data
        """
        sequence = sequence.upper().replace("N", "A")  # Replace N with random base
        seq_len = len(sequence)

        # Calculate trace length
        trace_length = seq_len * self.config.base_spacing + 2 * self.config.base_spacing

        # Initialize traces
        trace_a = np.zeros(trace_length)
        trace_c = np.zeros(trace_length)
        trace_g = np.zeros(trace_length)
        trace_t = np.zeros(trace_length)
        traces = [trace_a, trace_c, trace_g, trace_t]

        peak_locations = []
        quality_scores = []

        # Generate peaks for each base
        for i, base in enumerate(sequence):
            if base not in self.BASE_INDEX:
                continue

            # Peak position
            peak_pos = (i + 1) * self.config.base_spacing
            peak_locations.append(peak_pos)

            # Calculate quality_enhanced degradation at edges
            edge_factor = 1.0
            edge_distance = min(i, seq_len - 1 - i)
            if edge_distance < self.config.edge_degradation_length:
                edge_factor = (
                    self.config.edge_quality_factor
                    + (1 - self.config.edge_quality_factor)
                    * (edge_distance / self.config.edge_degradation_length)
                )

            # Peak height with variation
            base_height = self.config.peak_height_mean * quality_factor * edge_factor
            height = self.rng.normal(base_height, self.config.peak_height_std * noise_level)
            height = max(100, height)  # Minimum height

            # Generate Gaussian peak
            x = np.arange(trace_length)
            peak = height * np.exp(-0.5 * ((x - peak_pos) / self.config.peak_width) ** 2)

            # Add to correct trace
            base_idx = self.BASE_INDEX[base]
            traces[base_idx] += peak

            # Simulate heterozygote_training at some positions
            if self.rng.random() < self.config.heterozygote_rate:
                # Add secondary peak for different base
                other_bases = [b for b in self.BASE_INDEX if b != base]
                secondary_base = random.choice(other_bases)
                secondary_height = height * self.config.secondary_peak_ratio
                secondary_peak = secondary_height * np.exp(
                -0.5 * ((x - peak_pos) / self.config.peak_width) ** 2
            )
                traces[self.BASE_INDEX[secondary_base]] += secondary_peak

            # Calculate quality_enhanced score based on peak characteristics
            # Phred quality_enhanced: Q = -10 * log10(error_probability)
            # We simulate this based on SNR and position
            snr = height / (self.config.baseline_noise_std * noise_level + 5)
            # Convert SNR to quality_enhanced score (empirical formula to match real Sanger data)
            # Real Sanger sequencing typically gives Q20-Q50 for good bases
            base_quality = 20 + 12 * np.log10(max(snr / 10, 0.1))
            quality = int(np.clip(
                base_quality * edge_factor * quality_factor,
                self.config.min_quality,
                self.config.max_quality
            ))
            quality_scores.append(quality)

        # Add baseline noise to all traces
        for trace in traces:
            noise = self.rng.normal(0, self.config.baseline_noise_std * noise_level, trace_length)
            trace += noise
            trace += self.rng.normal(0, self.config.signal_noise_std * noise_level, trace_length)

            # Ensure non-negative
            trace[:] = np.maximum(trace, 0)

        # Create trace object
        synthetic = SyntheticTrace(
            sequence=sequence,
            quality_scores=quality_scores,
            trace_a=trace_a,
            trace_c=trace_c,
            trace_g=trace_g,
            trace_t=trace_t,
            peak_locations=peak_locations,
            variation_id=variation_id,
        )
        synthetic.compute_metrics()

        return synthetic

    def generate_variations(
        self,
        sequence: str,
        n_variations: int | None = None,
    ) -> list[SyntheticTrace]:
        """Generate multiple variations of a trace.

        Args:
            sequence: DNA sequence
            n_variations: Number of variations (default from config)

        Returns:
            List of SyntheticTrace objects
        """
        n_variations = n_variations or self.config.variations_per_sequence
        variations = []

        # Variation parameters
        noise_levels = [0.5, 1.0, 1.5, 2.0, 2.5]
        quality_factors = [0.7, 0.85, 1.0, 1.15, 1.3]

        for i in range(n_variations):
            noise_level = random.choice(noise_levels)
            quality_factor = random.choice(quality_factors)

            trace = self.generate_trace(
                sequence=sequence,
                variation_id=i,
                noise_level=noise_level,
                quality_factor=quality_factor,
            )
            variations.append(trace)

        return variations

    def process_fasta_file(
        self,
        fasta_path: Path,
        max_seq_length: int = 1000,
        min_seq_length: int = 100,
    ) -> Iterator[SyntheticTrace]:
        """Process a FASTA file and generate traces.

        Args:
            fasta_path: Path to .fasta.gz file
            max_seq_length: Maximum sequence length to process
            min_seq_length: Minimum sequence length to process

        Yields:
            SyntheticTrace objects
        """
        # Load metadata if available
        meta_path = fasta_path.with_suffix("").with_suffix(".json")
        metadata = {}
        if meta_path.exists():
            try:
                with open(meta_path) as f:
                    metadata = json.load(f)
            except Exception:
                pass

        species_name = metadata.get("species_name", "")
        taxon_id = metadata.get("taxon_id", 0)

        # Read FASTA
        try:
            with gzip.open(fasta_path, "rt") as f:
                current_seq = []

                for line in f:
                    line = line.strip()
                    if line.startswith(">"):
                        # Process previous sequence
                        if current_seq:
                            seq = "".join(current_seq)
                            if min_seq_length <= len(seq) <= max_seq_length:
                                for trace in self.generate_variations(seq):
                                    trace.source_file = str(fasta_path)
                                    trace.species_name = species_name
                                    trace.taxon_id = taxon_id
                                    yield trace

                        line[1:]
                        current_seq = []
                    else:
                        current_seq.append(line)

                # Process last sequence
                if current_seq:
                    seq = "".join(current_seq)
                    if min_seq_length <= len(seq) <= max_seq_length:
                        for trace in self.generate_variations(seq):
                            trace.source_file = str(fasta_path)
                            trace.species_name = species_name
                            trace.taxon_id = taxon_id
                            yield trace

        except Exception as e:
            logger.warning(f"Error processing {fasta_path}: {e}")

    def process_datalake(
        self,
        raw_dir: Path,
        output_dir: Path,
        max_species: int | None = None,
        max_seq_length: int = 1000,
        min_seq_length: int = 100,
    ) -> dict:
        """Process entire datalake and generate trace dataset.

        Args:
            raw_dir: Path to datalake/raw
            output_dir: Output directory for traces
            max_species: Maximum species to process
            max_seq_length: Maximum sequence length
            min_seq_length: Minimum sequence length

        Returns:
            Statistics dictionary
        """
        output_dir.mkdir(parents=True, exist_ok=True)

        # Find all FASTA files
        fasta_files = list(raw_dir.rglob("*.fasta.gz"))
        if max_species:
            fasta_files = fasta_files[:max_species]

        logger.info(f"Processing {len(fasta_files)} species...")

        stats = {
            "total_species": 0,
            "total_traces": 0,
            "total_sequences": 0,
            "quality_distribution": [],
        }

        # Process files
        traces_data = []

        for i, fasta_path in enumerate(fasta_files):
            try:
                for trace in self.process_fasta_file(fasta_path, max_seq_length, min_seq_length):
                    traces_data.append(trace.to_dict())
                    stats["total_traces"] += 1
                    stats["quality_distribution"].append(trace.mean_quality)

                stats["total_species"] += 1

                if (i + 1) % 100 == 0:
                    logger.info(
                        f"Processed {i + 1}/{len(fasta_files)} species, "
                        f"{stats['total_traces']} traces"
                    )

            except Exception as e:
                logger.warning(f"Error processing {fasta_path}: {e}")

        # Save traces
        output_file = output_dir / "synthetic_traces.json.gz"
        with gzip.open(output_file, "wt") as f:
            json.dump(traces_data, f)

        logger.info(f"Saved {len(traces_data)} traces to {output_file}")

        # Save statistics
        if stats["quality_distribution"]:
            stats["mean_quality"] = float(np.mean(stats["quality_distribution"]))
            stats["quality_std"] = float(np.std(stats["quality_distribution"]))
        else:
            stats["mean_quality"] = 0
            stats["quality_std"] = 0
        del stats["quality_distribution"]  # Don't save full distribution

        stats_file = output_dir / "generation_stats.json"
        with open(stats_file, "w") as f:
            json.dump(stats, f, indent=2)

        return stats
