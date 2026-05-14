"""Phylogenetic tree construction algorithms."""

from dataclasses import dataclass, field
from enum import Enum
from typing import Optional

from src.phylogeny.distance import DistanceMatrix


class TreeMethod(str, Enum):
    """Tree construction methods."""

    UPGMA = "upgma"
    NEIGHBOR_JOINING = "neighbor_joining"


@dataclass
class TreeNode:
    """Node in a phylogenetic tree."""

    name: str
    branch_length: float = 0.0
    children: list["TreeNode"] = field(default_factory=list)
    is_leaf: bool = True

    def to_dict(self) -> dict:
        """Serialize to dictionary."""
        return {
            "name": self.name,
            "branchLength": self.branch_length,
            "children": [c.to_dict() for c in self.children],
            "isLeaf": self.is_leaf,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "TreeNode":
        """Create from dictionary."""
        return cls(
            name=data["name"],
            branch_length=data.get("branchLength", 0.0),
            children=[cls.from_dict(c) for c in data.get("children", [])],
            is_leaf=data.get("isLeaf", True),
        )


@dataclass
class PhylogeneticTree:
    """Complete phylogenetic tree with metadata."""

    root: TreeNode
    method: str
    sequence_count: int
    newick: str

    def to_dict(self) -> dict:
        """Serialize to dictionary."""
        return {
            "root": self.root.to_dict(),
            "method": self.method,
            "sequenceCount": self.sequence_count,
            "newick": self.newick,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "PhylogeneticTree":
        """Create from dictionary."""
        return cls(
            root=TreeNode.from_dict(data["root"]),
            method=data["method"],
            sequence_count=data["sequenceCount"],
            newick=data["newick"],
        )


class TreeBuilder:
    """
    Build phylogenetic trees from distance matrices.

    Supports:
    - UPGMA: Assumes molecular clock (ultrametric tree)
    - Neighbor-Joining: No clock assumption, more accurate for unequal rates
    """

    def build(
        self,
        distance_matrix: DistanceMatrix,
        method: TreeMethod = TreeMethod.NEIGHBOR_JOINING,
    ) -> PhylogeneticTree:
        """
        Build phylogenetic tree from distance matrix.

        Args:
            distance_matrix: Pairwise distance matrix.
            method: Tree building algorithm.

        Returns:
            PhylogeneticTree with root, method, and Newick string.
        """
        if len(distance_matrix.labels) < 2:
            raise ValueError("At least 2 sequences required to build tree")

        if method == TreeMethod.UPGMA:
            root = self._build_upgma(distance_matrix)
        elif method == TreeMethod.NEIGHBOR_JOINING:
            root = self._build_neighbor_joining(distance_matrix)
        else:
            raise ValueError(f"Unknown tree method: {method}")

        newick = self._to_newick(root) + ";"

        return PhylogeneticTree(
            root=root,
            method=method.value,
            sequence_count=len(distance_matrix.labels),
            newick=newick,
        )

    def _build_upgma(self, distance_matrix: DistanceMatrix) -> TreeNode:
        """
        Build tree using UPGMA (Unweighted Pair Group Method with Arithmetic Mean).

        Assumes a molecular clock (constant rate of evolution).
        Produces an ultrametric tree (all leaves equidistant from root).
        """
        clusters: list[tuple[TreeNode, int, float]] = []
        for label in distance_matrix.labels:
            node = TreeNode(name=label, branch_length=0.0, is_leaf=True)
            clusters.append((node, 1, 0.0))

        matrix = [row[:] for row in distance_matrix.matrix]
        active = list(range(len(clusters)))

        while len(active) > 1:
            min_dist = float("inf")
            min_i, min_j = 0, 1

            for idx_i, i in enumerate(active):
                for idx_j, j in enumerate(active[idx_i + 1 :], idx_i + 1):
                    if matrix[i][j] < min_dist:
                        min_dist = matrix[i][j]
                        min_i, min_j = i, j

            node_i, size_i, height_i = clusters[min_i]
            node_j, size_j, height_j = clusters[min_j]

            new_height = min_dist / 2
            node_i.branch_length = new_height - height_i
            node_j.branch_length = new_height - height_j

            new_node = TreeNode(
                name=f"({node_i.name},{node_j.name})",
                branch_length=0.0,
                children=[node_i, node_j],
                is_leaf=False,
            )
            new_size = size_i + size_j

            for k in active:
                if k != min_i and k != min_j:
                    new_dist = (matrix[min_i][k] * size_i + matrix[min_j][k] * size_j) / new_size
                    matrix[min_i][k] = new_dist
                    matrix[k][min_i] = new_dist

            clusters[min_i] = (new_node, new_size, new_height)
            active.remove(min_j)

        return clusters[active[0]][0]

    def _build_neighbor_joining(self, distance_matrix: DistanceMatrix) -> TreeNode:
        """
        Build tree using Neighbor-Joining algorithm.

        Does not assume a molecular clock.
        More accurate when evolutionary rates vary among lineages.
        """
        n = len(distance_matrix.labels)

        if n == 2:
            dist = distance_matrix.matrix[0][1]
            leaf1 = TreeNode(
                name=distance_matrix.labels[0],
                branch_length=dist / 2,
                is_leaf=True,
            )
            leaf2 = TreeNode(
                name=distance_matrix.labels[1],
                branch_length=dist / 2,
                is_leaf=True,
            )
            return TreeNode(
                name=f"({leaf1.name},{leaf2.name})",
                branch_length=0.0,
                children=[leaf1, leaf2],
                is_leaf=False,
            )

        nodes: list[Optional[TreeNode]] = [
            TreeNode(name=label, is_leaf=True) for label in distance_matrix.labels
        ]

        matrix = [row[:] for row in distance_matrix.matrix]
        active = list(range(n))

        while len(active) > 2:
            r = len(active)

            row_sums = {}
            for i in active:
                row_sums[i] = sum(matrix[i][j] for j in active)

            min_q = float("inf")
            min_i, min_j = active[0], active[1]

            for idx_i, i in enumerate(active):
                for j in active[idx_i + 1 :]:
                    q = (r - 2) * matrix[i][j] - row_sums[i] - row_sums[j]
                    if q < min_q:
                        min_q = q
                        min_i, min_j = i, j

            delta = (row_sums[min_i] - row_sums[min_j]) / (r - 2) if r > 2 else 0
            branch_i = (matrix[min_i][min_j] + delta) / 2
            branch_j = (matrix[min_i][min_j] - delta) / 2

            branch_i = max(0.0, branch_i)
            branch_j = max(0.0, branch_j)

            node_i = nodes[min_i]
            node_j = nodes[min_j]
            if node_i is not None:
                node_i.branch_length = branch_i
            if node_j is not None:
                node_j.branch_length = branch_j

            new_node = TreeNode(
                name=f"({node_i.name if node_i else ''},{node_j.name if node_j else ''})",
                branch_length=0.0,
                children=[node_i, node_j] if node_i and node_j else [],
                is_leaf=False,
            )

            for k in active:
                if k != min_i and k != min_j:
                    new_dist = (matrix[min_i][k] + matrix[min_j][k] - matrix[min_i][min_j]) / 2
                    matrix[min_i][k] = new_dist
                    matrix[k][min_i] = new_dist

            nodes[min_i] = new_node
            nodes[min_j] = None
            active.remove(min_j)

        if len(active) == 2:
            i, j = active
            node_i = nodes[i]
            node_j = nodes[j]
            dist = matrix[i][j]

            if node_i is not None:
                node_i.branch_length = dist / 2
            if node_j is not None:
                node_j.branch_length = dist / 2

            root = TreeNode(
                name=f"({node_i.name if node_i else ''},{node_j.name if node_j else ''})",
                branch_length=0.0,
                children=[node_i, node_j] if node_i and node_j else [],
                is_leaf=False,
            )
            return root

        return nodes[active[0]] if nodes[active[0]] else TreeNode(name="empty")

    def _to_newick(self, node: TreeNode) -> str:
        """
        Convert tree to Newick format string.

        Format: ((A:0.1,B:0.2):0.3,C:0.4)
        Internal nodes are represented by nested parentheses.
        """
        if node.is_leaf:
            if node.branch_length > 0:
                return f"{node.name}:{node.branch_length:.6f}"
            return node.name

        children_str = ",".join(self._to_newick(child) for child in node.children)

        if node.branch_length > 0:
            return f"({children_str}):{node.branch_length:.6f}"
        return f"({children_str})"
