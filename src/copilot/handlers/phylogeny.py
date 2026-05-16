"""Phylogenetic analysis: distance matrices, trees and bootstrap support.

Phase 3: ports the distance and tree-building logic from
``geneflow-analysis/src/phylogeny`` while delegating the tree construction
to BioPython's :mod:`Bio.Phylo.TreeConstruction`.

Public tools
------------
- ``compute_distance_matrix``: pairwise p-distance / Jukes-Cantor / Kimura-2P
- ``build_phylogenetic_tree``: NJ or UPGMA from labelled sequences
- ``bootstrap_tree``: column-resampling bootstrap with bipartition support

Distance metrics are computed directly (gaps and N's excluded). The tree
builder relies on :class:`Bio.Phylo.TreeConstruction.DistanceTreeConstructor`
so we always emit valid Newick.
"""

from __futuREDACTED import annotations

import io
import math
import random
from collections import defaultdict
from typing import Any

import structlog
from Bio import Phylo
from Bio.Phylo.BaseTree import Clade, Tree
from Bio.Phylo.TreeConstruction import DistanceMatrix as BPDistanceMatrix
from Bio.Phylo.TreeConstruction import DistanceTreeConstructor

logger = structlog.get_logger()

_PURINES = frozenset("AG")
_PYRIMIDINES = frozenset("CT")
_AMBIG = frozenset("-N")


# ---------------------------------------------------------------------------
# Distance metrics
# ---------------------------------------------------------------------------


def _p_distance(seq1: str, seq2: str) -> float:
    diff = comp = 0
    for a, b in zip(seq1, seq2):
        if a in _AMBIG or b in _AMBIG:
            continue
        comp += 1
        if a != b:
            diff += 1
    return diff / comp if comp else 0.0


def _jukes_cantor(seq1: str, seq2: str) -> float:
    p = _p_distance(seq1, seq2)
    if p >= 0.75:
        return float("inf")
    try:
        return -0.75 * math.log(1 - (4 * p / 3))
    except ValueError:
        return float("inf")


def _is_transition(a: str, b: str) -> bool:
    return (a in _PURINES and b in _PURINES) or (
        a in _PYRIMIDINES and b in _PYRIMIDINES
    )


def _kimura_2p(seq1: str, seq2: str) -> float:
    ts = tv = comp = 0
    for a, b in zip(seq1, seq2):
        if a in _AMBIG or b in _AMBIG:
            continue
        comp += 1
        if a != b:
            if _is_transition(a, b):
                ts += 1
            else:
                tv += 1
    if comp == 0:
        return 0.0
    P = ts / comp
    Q = tv / comp
    t1 = 1 - 2 * P - Q
    t2 = 1 - 2 * Q
    if t1 <= 0 or t2 <= 0:
        return float("inf")
    try:
        return -0.5 * math.log(t1 * math.sqrt(t2))
    except ValueError:
        return float("inf")


_METHODS = {
    "p_distance": _p_distance,
    "jukes_cantor": _jukes_cantor,
    "kimura_2p": _kimura_2p,
}


def _build_matrix(
    sequences: list[str], method: str
) -> tuple[list[list[float]], int]:
    """Return ``(matrix, comparable_inf_cells)``.

    The returned matrix is full (n×n, symmetric, zero diagonal).
    ``inf`` values (saturation) are capped to a large finite number so the
    tree builder doesn't choke.
    """
    fn = _METHODS[method]
    n = len(sequences)
    upper = [s.upper() for s in sequences]
    matrix = [[0.0] * n for _ in range(n)]
    inf_count = 0
    for i in range(n):
        for j in range(i + 1, n):
            d = fn(upper[i], upper[j])
            if math.isinf(d):
                inf_count += 1
                d = 10.0  # cap for downstream tree construction
            matrix[i][j] = matrix[j][i] = d
    return matrix, inf_count


def _to_biopython_dm(
    labels: list[str], matrix: list[list[float]]
) -> BPDistanceMatrix:
    """Convert a full symmetric matrix to BioPython's lower-triangular format."""
    lower: list[list[float]] = []
    for i in range(len(labels)):
        row = [matrix[i][j] for j in range(i + 1)]
        lower.append(row)
    return BPDistanceMatrix(names=list(labels), matrix=lower)


def _tree_to_newick(tree: Tree) -> str:
    buf = io.StringIO()
    Phylo.write(tree, buf, "newick")
    return buf.getvalue().strip()


def _clade_to_dict(clade: Clade) -> dict[str, Any]:
    return {
        "name": clade.name or "",
        "branchLength": float(clade.branch_length) if clade.branch_length else 0.0,
        "confidence": (
            float(clade.confidence) if clade.confidence is not None else None
        ),
        "isLeaf": clade.is_terminal(),
        "children": [_clade_to_dict(c) for c in clade.clades],
    }


# ---------------------------------------------------------------------------
# Public handlers
# ---------------------------------------------------------------------------


async def compute_distance_matrix(params: dict[str, Any]) -> dict[str, Any]:
    """Compute a pairwise evolutionary distance matrix.

    Args:
        params: ``{ aligned_sequences, labels?, method? }``
            method: ``"p_distance" | "jukes_cantor" | "kimura_2p"``
                    (default: ``"jukes_cantor"``)

    Returns:
        ``{ labels, matrix, method, saturatedPairs }``. ``matrix`` is a full
        n×n symmetric matrix; ``saturatedPairs`` counts pairs where the metric
        diverged (capped to a large finite value before tree building).
    """
    aligned = params.get("aligned_sequences") or []
    if len(aligned) < 2:
        return {"error": "At least 2 aligned sequences are required"}

    length = len(aligned[0])
    if any(len(s) != length for s in aligned):
        return {"error": "All aligned sequences must have the same length"}

    method = params.get("method", "jukes_cantor")
    if method not in _METHODS:
        return {"error": f"Unknown method '{method}'. Use one of: {list(_METHODS)}"}

    labels = params.get("labels") or [f"seq_{i}" for i in range(len(aligned))]
    if len(labels) != len(aligned):
        return {"error": "labels length must match aligned_sequences length"}

    matrix, inf_count = _build_matrix(aligned, method)
    rounded = [[round(v, 6) for v in row] for row in matrix]
    return {
        "labels": labels,
        "matrix": rounded,
        "method": method,
        "saturatedPairs": inf_count,
    }


async def build_phylogenetic_tree(params: dict[str, Any]) -> dict[str, Any]:
    """Build a phylogenetic tree from labelled aligned sequences.

    Args:
        params: ``{ aligned_sequences, labels?, distance_method?, tree_method? }``
            distance_method: see :func:`compute_distance_matrix`
            tree_method: ``"nj" | "upgma"`` (default: ``"nj"``)

    Returns:
        ``{ method, distanceMethod, sequenceCount, newick, tree, saturatedPairs }``
        where ``tree`` is a nested clade dict.
    """
    aligned = params.get("aligned_sequences") or []
    if len(aligned) < 2:
        return {"error": "At least 2 aligned sequences are required"}

    length = len(aligned[0])
    if any(len(s) != length for s in aligned):
        return {"error": "All aligned sequences must have the same length"}

    dist_method = params.get("distance_method", "jukes_cantor")
    if dist_method not in _METHODS:
        return {"error": f"Unknown distance_method '{dist_method}'"}

    tree_method = params.get("tree_method", "nj").lower()
    if tree_method not in ("nj", "upgma"):
        return {"error": "tree_method must be 'nj' or 'upgma'"}

    labels = params.get("labels") or [f"seq_{i}" for i in range(len(aligned))]
    if len(labels) != len(aligned):
        return {"error": "labels length must match aligned_sequences length"}

    matrix, inf_count = _build_matrix(aligned, dist_method)
    bp_dm = _to_biopython_dm(labels, matrix)
    constructor = DistanceTreeConstructor()
    try:
        tree = (
            constructor.nj(bp_dm) if tree_method == "nj" else constructor.upgma(bp_dm)
        )
    except Exception as e:  # pragma: no cover
        return {"error": f"Tree construction failed: {e}"}

    newick = _tree_to_newick(tree)
    return {
        "method": tree_method,
        "distanceMethod": dist_method,
        "sequenceCount": len(labels),
        "newick": newick,
        "tree": _clade_to_dict(tree.root),
        "saturatedPairs": inf_count,
    }


# ---------------------------------------------------------------------------
# Bootstrap
# ---------------------------------------------------------------------------


def _column_resample(sequences: list[str], length: int, rng: random.Random) -> list[str]:
    idx = [rng.randint(0, length - 1) for _ in range(length)]
    return ["".join(seq[k] for k in idx) for seq in sequences]


def _collect_bipartitions(clade: Clade, all_taxa: frozenset[str]) -> list[frozenset[str]]:
    """Return the non-trivial bipartitions induced by every internal clade.

    A bipartition is canonicalised by always picking the half whose minimal
    label sorts first, so it can be compared across replicate trees.
    """
    out: list[frozenset[str]] = []

    def recurse(node: Clade) -> frozenset[str]:
        if node.is_terminal():
            return frozenset([node.name]) if node.name else frozenset()
        leaves: set[str] = set()
        for child in node.clades:
            leaves |= recurse(child)
        leaf_set = frozenset(leaves)
        if 1 < len(leaf_set) < len(all_taxa):
            comp = all_taxa - leaf_set
            # canonical form: the half with the smallest min element
            canon = leaf_set if min(leaf_set) < min(comp) else comp
            out.append(canon)
        return leaf_set

    recurse(clade)
    return out


async def bootstrap_tree(params: dict[str, Any]) -> dict[str, Any]:
    """Estimate clade support via column-resampling bootstrap.

    Args:
        params: ``{ aligned_sequences, labels?, replicates?, distance_method?,
                    tree_method?, seed? }``
            replicates: 10..500 (default 100)
            seed: optional int for reproducibility

    Returns:
        ``{ originalTree, supportValues, replicates }``
        ``supportValues`` maps the sorted-tuple-of-taxa of each non-trivial
        bipartition (joined with ``","``) to a percentage 0–100.
    """
    aligned = params.get("aligned_sequences") or []
    if len(aligned) < 3:
        return {"error": "Bootstrap needs at least 3 aligned sequences"}

    length = len(aligned[0])
    if any(len(s) != length for s in aligned):
        return {"error": "All aligned sequences must have the same length"}

    dist_method = params.get("distance_method", "jukes_cantor")
    if dist_method not in _METHODS:
        return {"error": f"Unknown distance_method '{dist_method}'"}

    tree_method = params.get("tree_method", "nj").lower()
    if tree_method not in ("nj", "upgma"):
        return {"error": "tree_method must be 'nj' or 'upgma'"}

    labels = params.get("labels") or [f"seq_{i}" for i in range(len(aligned))]
    if len(labels) != len(aligned):
        return {"error": "labels length must match aligned_sequences length"}

    replicates = max(10, min(500, int(params.get("replicates", 100))))
    seed = params.get("seed")
    rng = random.Random(seed) if seed is not None else random.Random()

    upper = [s.upper() for s in aligned]
    all_taxa = frozenset(labels)
    constructor = DistanceTreeConstructor()

    # Original tree
    base_matrix, inf_count = _build_matrix(upper, dist_method)
    base_dm = _to_biopython_dm(labels, base_matrix)
    base_tree = (
        constructor.nj(base_dm) if tree_method == "nj" else constructor.upgma(base_dm)
    )
    base_biparts = _collect_bipartitions(base_tree.root, all_taxa)

    # Bootstrap replicates
    counts: dict[frozenset[str], int] = defaultdict(int)
    succeeded = 0
    for _ in range(replicates):
        resampled = _column_resample(upper, length, rng)
        try:
            m, _ = _build_matrix(resampled, dist_method)
            dm = _to_biopython_dm(labels, m)
            tree = (
                constructor.nj(dm) if tree_method == "nj" else constructor.upgma(dm)
            )
        except Exception:
            continue
        succeeded += 1
        for bp in _collect_bipartitions(tree.root, all_taxa):
            counts[bp] += 1

    support = {
        ",".join(sorted(bp)): round(counts.get(bp, 0) / succeeded * 100, 1)
        if succeeded
        else 0.0
        for bp in base_biparts
    }

    return {
        "originalTree": {
            "newick": _tree_to_newick(base_tree),
            "tree": _clade_to_dict(base_tree.root),
            "method": tree_method,
            "distanceMethod": dist_method,
        },
        "supportValues": support,
        "replicates": replicates,
        "successfulReplicates": succeeded,
        "saturatedPairs": inf_count,
    }
