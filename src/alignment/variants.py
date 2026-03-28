"""Variant detection from aligned sequences."""

from dataclasses import dataclass, field
from enum import Enum


class VariantType(str, Enum):
    """Type of sequence variant."""
    SNP = "snp"  # Single nucleotide polymorphism
    INSERTION = "insertion"
    DELETION = "deletion"
    MNP = "mnp"  # Multiple nucleotide polymorphism


@dataclass
class Variant:
    """Represents a detected variant."""
    position: int  # 0-based position in alignment
    type: VariantType
    reference: str  # Reference base(s)
    alternate: str  # Alternate base(s)
    frequency: float  # Frequency of alternate allele
    coverage: int  # Number of sequences at position
    sequenceIndices: list[int] = field(default_factory=list)  # Which sequences have variant


@dataclass
class VariantReport:
    """Report of all variants found in alignment."""
    variants: list[Variant]
    totalPositions: int
    variantPositions: int
    snpCount: int
    insertionCount: int
    deletionCount: int


class VariantDetector:
    """
    Detect variants in aligned sequences.

    Compares sequences to a reference (first sequence or consensus)
    to identify SNPs, insertions, and deletions.
    """

    def detect(
        self,
        aligned_sequences: list[str],
        reference_index: int = 0,
        min_frequency: float = 0.0,
        min_coverage: int = 1,
    ) -> VariantReport:
        """
        Detect variants in aligned sequences.

        Args:
            aligned_sequences: List of aligned sequences (same length)
            reference_index: Index of reference sequence (default: first)
            min_frequency: Minimum variant frequency to report (0.0-1.0)
            min_coverage: Minimum coverage required

        Returns:
            VariantReport with all detected variants
        """
        if not aligned_sequences or len(aligned_sequences) < 2:
            return VariantReport(
                variants=[],
                totalPositions=0,
                variantPositions=0,
                snpCount=0,
                insertionCount=0,
                deletionCount=0,
            )

        # Validate sequences
        length = len(aligned_sequences[0])
        for seq in aligned_sequences:
            if len(seq) != length:
                raise ValueError("All aligned sequences must have the same length")

        if reference_index >= len(aligned_sequences):
            raise ValueError(f"Reference index {reference_index} out of range")

        reference = aligned_sequences[reference_index].upper()
        variants = []
        variant_positions = set()

        for i in range(length):
            ref_base = reference[i]

            # Get all bases at this position (excluding reference)
            other_bases = []
            for j, seq in enumerate(aligned_sequences):
                if j != reference_index:
                    other_bases.append((j, seq[i].upper()))

            coverage = len(other_bases)
            if coverage < min_coverage:
                continue

            # Count alternate alleles
            alt_counts = {}
            for seq_idx, base in other_bases:
                if base != ref_base:
                    if base not in alt_counts:
                        alt_counts[base] = {"count": 0, "indices": []}
                    alt_counts[base]["count"] += 1
                    alt_counts[base]["indices"].append(seq_idx)

            # Report variants
            for alt_base, data in alt_counts.items():
                frequency = data["count"] / coverage

                if frequency >= min_frequency:
                    variant_type = self._determine_type(ref_base, alt_base)
                    variant = Variant(
                        position=i,
                        type=variant_type,
                        reference=ref_base,
                        alternate=alt_base,
                        frequency=round(frequency, 3),
                        coverage=coverage,
                        sequenceIndices=data["indices"],
                    )
                    variants.append(variant)
                    variant_positions.add(i)

        # Count by type
        snp_count = sum(1 for v in variants if v.type == VariantType.SNP)
        ins_count = sum(1 for v in variants if v.type == VariantType.INSERTION)
        del_count = sum(1 for v in variants if v.type == VariantType.DELETION)

        return VariantReport(
            variants=variants,
            totalPositions=length,
            variantPositions=len(variant_positions),
            snpCount=snp_count,
            insertionCount=ins_count,
            deletionCount=del_count,
        )

    def _determine_type(self, ref: str, alt: str) -> VariantType:
        """Determine variant type from reference and alternate."""
        if ref == "-" and alt != "-":
            return VariantType.INSERTION
        elif ref != "-" and alt == "-":
            return VariantType.DELETION
        elif len(ref) == 1 and len(alt) == 1:
            return VariantType.SNP
        else:
            return VariantType.MNP

    def detect_vs_consensus(
        self,
        aligned_sequences: list[str],
        consensus: str,
        min_frequency: float = 0.0,
    ) -> VariantReport:
        """
        Detect variants comparing all sequences to a consensus.

        Args:
            aligned_sequences: List of aligned sequences
            consensus: Consensus sequence to compare against
            min_frequency: Minimum variant frequency

        Returns:
            VariantReport
        """
        if not aligned_sequences:
            return VariantReport(
                variants=[],
                totalPositions=0,
                variantPositions=0,
                snpCount=0,
                insertionCount=0,
                deletionCount=0,
            )

        length = len(consensus)
        variants = []
        variant_positions = set()

        for i in range(length):
            ref_base = consensus[i].upper()

            # Get all bases at this position
            all_bases = []
            for j, seq in enumerate(aligned_sequences):
                if i < len(seq):
                    all_bases.append((j, seq[i].upper()))

            coverage = len(all_bases)
            if coverage == 0:
                continue

            # Count variants
            alt_counts = {}
            for seq_idx, base in all_bases:
                if base != ref_base:
                    if base not in alt_counts:
                        alt_counts[base] = {"count": 0, "indices": []}
                    alt_counts[base]["count"] += 1
                    alt_counts[base]["indices"].append(seq_idx)

            for alt_base, data in alt_counts.items():
                frequency = data["count"] / coverage

                if frequency >= min_frequency:
                    variant_type = self._determine_type(ref_base, alt_base)
                    variant = Variant(
                        position=i,
                        type=variant_type,
                        reference=ref_base,
                        alternate=alt_base,
                        frequency=round(frequency, 3),
                        coverage=coverage,
                        sequenceIndices=data["indices"],
                    )
                    variants.append(variant)
                    variant_positions.add(i)

        snp_count = sum(1 for v in variants if v.type == VariantType.SNP)
        ins_count = sum(1 for v in variants if v.type == VariantType.INSERTION)
        del_count = sum(1 for v in variants if v.type == VariantType.DELETION)

        return VariantReport(
            variants=variants,
            totalPositions=length,
            variantPositions=len(variant_positions),
            snpCount=snp_count,
            insertionCount=ins_count,
            deletionCount=del_count,
        )

    def summarize_variants(self, report: VariantReport) -> dict:
        """
        Create summary statistics from variant report.

        Args:
            report: VariantReport to summarize

        Returns:
            Dictionary with summary statistics
        """
        if not report.variants:
            return {
                "totalVariants": 0,
                "variantRate": 0.0,
                "snpRate": 0.0,
                "transitions": 0,
                "transversions": 0,
                "tiTvRatio": None,
            }

        # Count transitions vs transversions for SNPs
        transitions = 0  # A<->G, C<->T (purine-purine or pyrimidine-pyrimidine)
        transversions = 0  # Others

        transition_pairs = {("A", "G"), ("G", "A"), ("C", "T"), ("T", "C")}

        for variant in report.variants:
            if variant.type == VariantType.SNP:
                pair = (variant.reference, variant.alternate)
                if pair in transition_pairs:
                    transitions += 1
                else:
                    transversions += 1

        ti_tv_ratio = None
        if transversions > 0:
            ti_tv_ratio = round(transitions / transversions, 3)

        variant_rate = 0.0
        if report.totalPositions > 0:
            variant_rate = round(report.variantPositions / report.totalPositions, 4)

        return {
            "totalVariants": len(report.variants),
            "variantRate": variant_rate,
            "snpRate": round(report.snpCount / report.totalPositions, 4) if report.totalPositions > 0 else 0.0,
            "transitions": transitions,
            "transversions": transversions,
            "tiTvRatio": ti_tv_ratio,
        }
