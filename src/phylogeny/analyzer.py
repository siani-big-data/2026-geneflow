"""High-level phylogenetic analysis API."""

import uuid
from dataclasses import dataclass
from typing import Optional

from src.phylogeny.bootstrap import BootstrapAnalyzer, BootstrapResult
from src.phylogeny.distance import DistanceCalculator, DistanceMatrix, DistanceMethod
from src.phylogeny.tree import PhylogeneticTree, TreeBuilder, TreeMethod


@dataclass
class PhylogenyResult:
    """Complete result of phylogenetic analysis."""

    analysis_id: str
    distance_matrix: DistanceMatrix
    tree: PhylogeneticTree
    bootstrap: Optional[BootstrapResult]
    sequence_count: int
    alignment_length: int

    def to_dict(self) -> dict:
        """Serialize to dictionary."""
        return {
            "analysisId": self.analysis_id,
            "distanceMatrix": self.distance_matrix.to_dict(),
            "tree": self.tree.to_dict(),
            "bootstrap": self.bootstrap.to_dict() if self.bootstrap else None,
            "sequenceCount": self.sequence_count,
            "alignmentLength": self.alignment_length,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "PhylogenyResult":
        """Create from dictionary."""
        return cls(
            analysis_id=data["analysisId"],
            distance_matrix=DistanceMatrix.from_dict(data["distanceMatrix"]),
            tree=PhylogeneticTree.from_dict(data["tree"]),
            bootstrap=BootstrapResult.from_dict(data["bootstrap"])
            if data.get("bootstrap")
            else None,
            sequence_count=data["sequenceCount"],
            alignment_length=data["alignmentLength"],
        )


class PhylogenyAnalyzer:
    """
    High-level API for phylogenetic analysis.

    Combines distance calculation, tree building, and optional
    bootstrap analysis into a single workflow.

    Example:
        analyzer = PhylogenyAnalyzer()
        result = analyzer.analyze(
            aligned_sequences=["ATGCAT", "ATGCGT", "ATGCCT"],
            labels=["seq1", "seq2", "seq3"],
            bootstrap_replicates=100
        )
        print(result.tree.newick)
    """

    def __init__(self):
        self._distance_calculator = DistanceCalculator()
        self._tree_builder = TreeBuilder()
        self._bootstrap_analyzer = BootstrapAnalyzer()

    @property
    def name(self) -> str:
        """Analyzer name for registration."""
        return "phylogeny"

    def analyze(
        self,
        aligned_sequences: list[str],
        labels: list[str] | None = None,
        distance_method: DistanceMethod = DistanceMethod.JUKES_CANTOR,
        tree_method: TreeMethod = TreeMethod.NEIGHBOR_JOINING,
        bootstrap_replicates: int = 0,
        analysis_id: str | None = None,
        bootstrap_seed: int | None = None,
    ) -> PhylogenyResult:
        """
        Perform complete phylogenetic analysis.

        Args:
            aligned_sequences: List of aligned sequences (must be same length).
            labels: Optional sequence labels. Defaults to seq_0, seq_1, etc.
            distance_method: Method for calculating evolutionary distances.
            tree_method: Algorithm for building phylogenetic tree.
            bootstrap_replicates: Number of bootstrap replicates (0 to skip).
            analysis_id: Optional analysis ID. Generated if not provided.
            bootstrap_seed: Random seed for bootstrap reproducibility.

        Returns:
            PhylogenyResult containing distance matrix, tree, and bootstrap.

        Raises:
            ValueError: If sequences are invalid or have different lengths.
        """
        if len(aligned_sequences) < 2:
            raise ValueError("At least 2 sequences required for phylogenetic analysis")

        alignment_length = len(aligned_sequences[0])
        for i, seq in enumerate(aligned_sequences):
            if len(seq) != alignment_length:
                raise ValueError(f"Sequence {i} has length {len(seq)}, expected {alignment_length}")

        if labels is None:
            labels = [f"seq_{i}" for i in range(len(aligned_sequences))]
        elif len(labels) != len(aligned_sequences):
            raise ValueError("Number of labels must match number of sequences")

        if analysis_id is None:
            analysis_id = str(uuid.uuid4())

        distance_matrix = self._distance_calculator.calculate(
            aligned_sequences,
            labels,
            distance_method,
        )

        tree = self._tree_builder.build(distance_matrix, tree_method)

        bootstrap: Optional[BootstrapResult] = None
        if bootstrap_replicates > 0:
            bootstrap = self._bootstrap_analyzer.analyze(
                aligned_sequences,
                labels,
                replicates=bootstrap_replicates,
                distance_method=distance_method,
                tree_method=tree_method,
                seed=bootstrap_seed,
            )

        return PhylogenyResult(
            analysis_id=analysis_id,
            distance_matrix=distance_matrix,
            tree=tree,
            bootstrap=bootstrap,
            sequence_count=len(aligned_sequences),
            alignment_length=alignment_length,
        )
