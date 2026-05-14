"""Bootstrap analysis for phylogenetic tree support."""

import random
from collections import defaultdict
from dataclasses import dataclass
from typing import Optional

from src.phylogeny.distance import DistanceCalculator, DistanceMethod
from src.phylogeny.tree import PhylogeneticTree, TreeBuilder, TreeMethod, TreeNode


@dataclass
class BootstrapResult:
    """Result of bootstrap analysis."""

    original_tree: PhylogeneticTree
    support_values: dict[str, float]
    replicates: int
    consensus_tree: Optional[PhylogeneticTree] = None

    def to_dict(self) -> dict:
        """Serialize to dictionary."""
        return {
            "originalTree": self.original_tree.to_dict(),
            "supportValues": self.support_values,
            "replicates": self.replicates,
            "consensusTree": self.consensus_tree.to_dict() if self.consensus_tree else None,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "BootstrapResult":
        """Create from dictionary."""
        return cls(
            original_tree=PhylogeneticTree.from_dict(data["originalTree"]),
            support_values=data["supportValues"],
            replicates=data["replicates"],
            consensus_tree=PhylogeneticTree.from_dict(data["consensusTree"])
            if data.get("consensusTree")
            else None,
        )


class BootstrapAnalyzer:
    """
    Perform bootstrap analysis for phylogenetic tree support.

    Bootstrap resampling assesses the reliability of tree topology
    by generating replicate datasets and counting how often each
    clade appears in the resulting trees.
    """

    def __init__(self):
        self._distance_calculator = DistanceCalculator()
        self._tree_builder = TreeBuilder()

    def analyze(
        self,
        aligned_sequences: list[str],
        labels: list[str],
        replicates: int = 100,
        distance_method: DistanceMethod = DistanceMethod.JUKES_CANTOR,
        tree_method: TreeMethod = TreeMethod.NEIGHBOR_JOINING,
        seed: Optional[int] = None,
    ) -> BootstrapResult:
        """
        Perform bootstrap analysis on aligned sequences.

        Args:
            aligned_sequences: List of aligned sequences (same length).
            labels: Sequence labels.
            replicates: Number of bootstrap replicates.
            distance_method: Method for distance calculation.
            tree_method: Method for tree construction.
            seed: Random seed for reproducibility.

        Returns:
            BootstrapResult with original tree and support values.

        Raises:
            ValueError: If sequences have different lengths or < 2 sequences.
        """
        if len(aligned_sequences) < 2:
            raise ValueError("At least 2 sequences required for bootstrap analysis")

        if seed is not None:
            random.seed(seed)

        original_matrix = self._distance_calculator.calculate(
            aligned_sequences, labels, distance_method
        )
        original_tree = self._tree_builder.build(original_matrix, tree_method)

        original_bipartitions = self._get_bipartitions(original_tree.root, set(labels))

        bipartition_counts: dict[frozenset[str], int] = defaultdict(int)
        alignment_length = len(aligned_sequences[0])

        for _ in range(replicates):
            resampled_seqs = self._resample_columns(aligned_sequences, alignment_length)

            try:
                boot_matrix = self._distance_calculator.calculate(
                    resampled_seqs, labels, distance_method
                )
                boot_tree = self._tree_builder.build(boot_matrix, tree_method)

                boot_bipartitions = self._get_bipartitions(boot_tree.root, set(labels))

                for bipart in boot_bipartitions:
                    bipartition_counts[bipart] += 1
            except (ValueError, ZeroDivisionError):
                continue

        support_values = self._calculate_support(
            original_tree.root,
            original_bipartitions,
            bipartition_counts,
            replicates,
            set(labels),
        )

        return BootstrapResult(
            original_tree=original_tree,
            support_values=support_values,
            replicates=replicates,
            consensus_tree=None,
        )

    def _resample_columns(
        self,
        sequences: list[str],
        length: int,
    ) -> list[str]:
        """Resample alignment columns with replacement."""
        indices = [random.randint(0, length - 1) for _ in range(length)]

        resampled = []
        for seq in sequences:
            resampled_seq = "".join(seq[i] for i in indices)
            resampled.append(resampled_seq)

        return resampled

    def _get_bipartitions(
        self,
        node: TreeNode,
        all_taxa: set[str],
    ) -> list[frozenset[str]]:
        """
        Get all bipartitions (clades) defined by a tree.

        A bipartition is the set of taxa descended from an internal node.
        The smaller half is stored to normalize representation.
        """
        bipartitions: list[frozenset[str]] = []
        self._collect_bipartitions(node, all_taxa, bipartitions)
        return bipartitions

    def _collect_bipartitions(
        self,
        node: TreeNode,
        all_taxa: set[str],
        bipartitions: list[frozenset[str]],
    ) -> frozenset[str]:
        """Recursively collect bipartitions from tree."""
        if node.is_leaf:
            return frozenset([node.name])

        descendant_taxa: set[str] = set()
        for child in node.children:
            child_taxa = self._collect_bipartitions(child, all_taxa, bipartitions)
            descendant_taxa.update(child_taxa)

        taxa_set = frozenset(descendant_taxa)

        if 1 < len(taxa_set) < len(all_taxa):
            complement = frozenset(all_taxa - descendant_taxa)
            if len(taxa_set) <= len(complement):
                bipartitions.append(taxa_set)
            else:
                bipartitions.append(complement)

        return taxa_set

    def _calculate_support(
        self,
        root: TreeNode,
        original_bipartitions: list[frozenset[str]],
        bipartition_counts: dict[frozenset[str], int],
        replicates: int,
        all_taxa: set[str],
    ) -> dict[str, float]:
        """Calculate bootstrap support values for tree nodes."""
        support_values: dict[str, float] = {}

        self._assign_support(root, bipartition_counts, replicates, all_taxa, support_values)

        return support_values

    def _assign_support(
        self,
        node: TreeNode,
        bipartition_counts: dict[frozenset[str], int],
        replicates: int,
        all_taxa: set[str],
        support_values: dict[str, float],
    ) -> frozenset[str]:
        """Recursively assign support values to nodes."""
        if node.is_leaf:
            support_values[node.name] = 100.0
            return frozenset([node.name])

        descendant_taxa: set[str] = set()
        for child in node.children:
            child_taxa = self._assign_support(
                child, bipartition_counts, replicates, all_taxa, support_values
            )
            descendant_taxa.update(child_taxa)

        taxa_set = frozenset(descendant_taxa)

        if 1 < len(taxa_set) < len(all_taxa):
            complement = frozenset(all_taxa - descendant_taxa)
            if len(taxa_set) <= len(complement):
                bipart = taxa_set
            else:
                bipart = complement

            count = bipartition_counts.get(bipart, 0)
            support = (count / replicates) * 100 if replicates > 0 else 0.0
            support_values[node.name] = support
        else:
            support_values[node.name] = 100.0

        return taxa_set
