"""Tests for phylogenetic analysis module."""

import math
from unittest.mock import AsyncMock, MagicMock

import pytest

from src.phylogeny import (
    BootstrapAnalyzer,
    BootstrapResult,
    DistanceCalculator,
    DistanceMatrix,
    DistanceMethod,
    PhylogeneticTree,
    PhylogenyAnalyzer,
    PhylogenyResult,
    TreeBuilder,
    TreeMethod,
    TreeNode,
)
from src.models import PhylogenyJob
from src.events.events import PhylogenyCompleted, PhylogenyFailed


# =============================================================================
# Test Data
# =============================================================================


# Simple aligned sequences for testing
ALIGNED_SEQUENCES = [
    "ATGCATGCAT",
    "ATGCGTGCAT",
    "ATGCCTGCAT",
]
LABELS = ["seq1", "seq2", "seq3"]

# Identical sequences
IDENTICAL_SEQUENCES = [
    "ATGCATGCAT",
    "ATGCATGCAT",
]

# Very different sequences
DIVERGENT_SEQUENCES = [
    "AAAAAAAAAA",
    "TTTTTTTTTT",
]

# Sequences with gaps
GAPPED_SEQUENCES = [
    "ATG-ATGCAT",
    "ATGCATGCAT",
    "ATG-CTGCAT",
]


# =============================================================================
# DistanceCalculator Tests
# =============================================================================


class TestDistanceCalculator:
    """Tests for DistanceCalculator."""

    def test_p_distance_identical(self):
        """Identical sequences should have 0 p-distance."""
        calc = DistanceCalculator()
        result = calc.calculate(IDENTICAL_SEQUENCES, ["a", "b"], DistanceMethod.P_DISTANCE)

        assert result.matrix[0][1] == 0.0
        assert result.matrix[1][0] == 0.0
        assert result.method == "p_distance"

    def test_p_distance_different(self):
        """Different sequences should have positive p-distance."""
        calc = DistanceCalculator()
        result = calc.calculate(ALIGNED_SEQUENCES, LABELS, DistanceMethod.P_DISTANCE)

        # seq1 vs seq2: 1 difference in 10 positions = 0.1 (position 5: A vs G)
        assert abs(result.matrix[0][1] - 0.1) < 0.001
        # seq1 vs seq3: 1 difference in 10 positions = 0.1 (position 5: A vs C)
        assert abs(result.matrix[0][2] - 0.1) < 0.001
        # seq2 vs seq3: 1 difference in 10 positions = 0.1 (position 5: G vs C)
        assert abs(result.matrix[1][2] - 0.1) < 0.001

    def test_p_distance_divergent(self):
        """Completely different sequences should have p-distance of 1.0."""
        calc = DistanceCalculator()
        result = calc.calculate(DIVERGENT_SEQUENCES, ["a", "b"], DistanceMethod.P_DISTANCE)

        assert result.matrix[0][1] == 1.0

    def test_jukes_cantor_identical(self):
        """Identical sequences should have 0 Jukes-Cantor distance."""
        calc = DistanceCalculator()
        result = calc.calculate(
            IDENTICAL_SEQUENCES, ["a", "b"], DistanceMethod.JUKES_CANTOR
        )

        assert result.matrix[0][1] == 0.0
        assert result.method == "jukes_cantor"

    def test_jukes_cantor_correction(self):
        """Jukes-Cantor should be greater than p-distance for divergent sequences."""
        calc = DistanceCalculator()
        p_result = calc.calculate(ALIGNED_SEQUENCES, LABELS, DistanceMethod.P_DISTANCE)
        jc_result = calc.calculate(ALIGNED_SEQUENCES, LABELS, DistanceMethod.JUKES_CANTOR)

        # JC correction should give larger distances
        assert jc_result.matrix[0][1] > p_result.matrix[0][1]
        assert jc_result.matrix[1][2] > p_result.matrix[1][2]

    def test_jukes_cantor_saturation(self):
        """Very divergent sequences should return infinity."""
        calc = DistanceCalculator()
        result = calc.calculate(DIVERGENT_SEQUENCES, ["a", "b"], DistanceMethod.JUKES_CANTOR)

        # p-distance of 1.0 exceeds 0.75 saturation limit
        assert result.matrix[0][1] == float("inf")

    def test_kimura_transitions_transversions(self):
        """Kimura 2-parameter should distinguish transitions and transversions."""
        calc = DistanceCalculator()

        # Transition: A <-> G (both purines)
        transition_seqs = ["AAAAA", "GAAAA"]
        # Transversion: A <-> T (purine <-> pyrimidine)
        transversion_seqs = ["AAAAA", "TAAAA"]

        trans_result = calc.calculate(
            transition_seqs, ["a", "b"], DistanceMethod.KIMURA_2P
        )
        transv_result = calc.calculate(
            transversion_seqs, ["a", "b"], DistanceMethod.KIMURA_2P
        )

        # Both have same p-distance (0.2) but K2P treats them differently
        assert trans_result.matrix[0][1] > 0
        assert transv_result.matrix[0][1] > 0
        assert trans_result.method == "kimura_2p"

    def test_handles_gaps(self):
        """Gaps should be excluded from distance calculation."""
        calc = DistanceCalculator()
        result = calc.calculate(GAPPED_SEQUENCES, LABELS, DistanceMethod.P_DISTANCE)

        # Gaps reduce comparable sites
        assert result.matrix[0][1] >= 0
        assert result.matrix[0][1] <= 1.0

    def test_distance_matrix_symmetric(self):
        """Distance matrix should be symmetric."""
        calc = DistanceCalculator()
        result = calc.calculate(ALIGNED_SEQUENCES, LABELS, DistanceMethod.P_DISTANCE)

        for i in range(len(LABELS)):
            for j in range(len(LABELS)):
                assert result.matrix[i][j] == result.matrix[j][i]

    def test_distance_matrix_diagonal_zero(self):
        """Diagonal of distance matrix should be zero."""
        calc = DistanceCalculator()
        result = calc.calculate(ALIGNED_SEQUENCES, LABELS, DistanceMethod.P_DISTANCE)

        for i in range(len(LABELS)):
            assert result.matrix[i][i] == 0.0

    def test_requires_at_least_2_sequences(self):
        """Should raise error for fewer than 2 sequences."""
        calc = DistanceCalculator()
        with pytest.raises(ValueError, match="At least 2 sequences"):
            calc.calculate(["ATGCAT"], ["seq1"], DistanceMethod.P_DISTANCE)

    def test_requires_same_length(self):
        """Should raise error for sequences of different lengths."""
        calc = DistanceCalculator()
        with pytest.raises(ValueError, match="has length"):
            calc.calculate(["ATGCAT", "ATG"], ["a", "b"], DistanceMethod.P_DISTANCE)

    def test_auto_labels(self):
        """Should generate labels if not provided."""
        calc = DistanceCalculator()
        result = calc.calculate(ALIGNED_SEQUENCES, method=DistanceMethod.P_DISTANCE)

        assert result.labels == ["seq_0", "seq_1", "seq_2"]

    def test_distance_matrix_get_distance(self):
        """DistanceMatrix.get_distance should return correct values."""
        calc = DistanceCalculator()
        result = calc.calculate(ALIGNED_SEQUENCES, LABELS, DistanceMethod.P_DISTANCE)

        assert result.get_distance("seq1", "seq2") == result.matrix[0][1]
        assert result.get_distance("seq2", "seq3") == result.matrix[1][2]

    def test_distance_matrix_serialization(self):
        """DistanceMatrix should serialize and deserialize correctly."""
        calc = DistanceCalculator()
        result = calc.calculate(ALIGNED_SEQUENCES, LABELS, DistanceMethod.P_DISTANCE)

        data = result.to_dict()
        restored = DistanceMatrix.from_dict(data)

        assert restored.labels == result.labels
        assert restored.matrix == result.matrix
        assert restored.method == result.method


# =============================================================================
# TreeBuilder Tests
# =============================================================================


class TestTreeBuilder:
    """Tests for TreeBuilder."""

    def test_upgma_basic(self):
        """UPGMA should build a valid tree."""
        calc = DistanceCalculator()
        builder = TreeBuilder()

        matrix = calc.calculate(ALIGNED_SEQUENCES, LABELS, DistanceMethod.P_DISTANCE)
        tree = builder.build(matrix, TreeMethod.UPGMA)

        assert tree.method == "upgma"
        assert tree.sequence_count == 3
        assert tree.root is not None
        assert not tree.root.is_leaf

    def test_neighbor_joining_basic(self):
        """Neighbor-Joining should build a valid tree."""
        calc = DistanceCalculator()
        builder = TreeBuilder()

        matrix = calc.calculate(ALIGNED_SEQUENCES, LABELS, DistanceMethod.P_DISTANCE)
        tree = builder.build(matrix, TreeMethod.NEIGHBOR_JOINING)

        assert tree.method == "neighbor_joining"
        assert tree.sequence_count == 3
        assert tree.root is not None
        assert not tree.root.is_leaf

    def test_newick_format(self):
        """Tree should have valid Newick format."""
        calc = DistanceCalculator()
        builder = TreeBuilder()

        matrix = calc.calculate(ALIGNED_SEQUENCES, LABELS, DistanceMethod.P_DISTANCE)
        tree = builder.build(matrix, TreeMethod.NEIGHBOR_JOINING)

        # Newick should end with semicolon
        assert tree.newick.endswith(";")
        # Should contain all sequence names
        for label in LABELS:
            assert label in tree.newick
        # Should have nested parentheses
        assert "(" in tree.newick
        assert ")" in tree.newick

    def test_two_sequences(self):
        """Should handle two-sequence case."""
        calc = DistanceCalculator()
        builder = TreeBuilder()

        matrix = calc.calculate(IDENTICAL_SEQUENCES, ["a", "b"], DistanceMethod.P_DISTANCE)
        tree = builder.build(matrix, TreeMethod.NEIGHBOR_JOINING)

        assert tree.sequence_count == 2
        assert "a" in tree.newick
        assert "b" in tree.newick

    def test_branch_lengths_positive(self):
        """Branch lengths should be non-negative."""
        calc = DistanceCalculator()
        builder = TreeBuilder()

        matrix = calc.calculate(ALIGNED_SEQUENCES, LABELS, DistanceMethod.P_DISTANCE)
        tree = builder.build(matrix, TreeMethod.NEIGHBOR_JOINING)

        def check_branch_lengths(node):
            assert node.branch_length >= 0
            for child in node.children:
                check_branch_lengths(child)

        check_branch_lengths(tree.root)

    def test_tree_has_correct_leaves(self):
        """Tree should have all input sequences as leaves."""
        calc = DistanceCalculator()
        builder = TreeBuilder()

        matrix = calc.calculate(ALIGNED_SEQUENCES, LABELS, DistanceMethod.P_DISTANCE)
        tree = builder.build(matrix, TreeMethod.UPGMA)

        def collect_leaves(node):
            if node.is_leaf:
                return [node.name]
            leaves = []
            for child in node.children:
                leaves.extend(collect_leaves(child))
            return leaves

        leaves = collect_leaves(tree.root)
        assert sorted(leaves) == sorted(LABELS)

    def test_tree_node_serialization(self):
        """TreeNode should serialize and deserialize correctly."""
        node = TreeNode(
            name="test",
            branch_length=0.5,
            children=[TreeNode(name="child", branch_length=0.2, is_leaf=True)],
            is_leaf=False,
        )

        data = node.to_dict()
        restored = TreeNode.from_dict(data)

        assert restored.name == node.name
        assert restored.branch_length == node.branch_length
        assert len(restored.children) == 1
        assert restored.children[0].name == "child"

    def test_phylogenetic_tree_serialization(self):
        """PhylogeneticTree should serialize and deserialize correctly."""
        calc = DistanceCalculator()
        builder = TreeBuilder()

        matrix = calc.calculate(ALIGNED_SEQUENCES, LABELS, DistanceMethod.P_DISTANCE)
        tree = builder.build(matrix, TreeMethod.NEIGHBOR_JOINING)

        data = tree.to_dict()
        restored = PhylogeneticTree.from_dict(data)

        assert restored.method == tree.method
        assert restored.sequence_count == tree.sequence_count
        assert restored.newick == tree.newick


# =============================================================================
# BootstrapAnalyzer Tests
# =============================================================================


class TestBootstrapAnalyzer:
    """Tests for BootstrapAnalyzer."""

    def test_bootstrap_100_replicates(self):
        """Bootstrap with 100 replicates should complete."""
        analyzer = BootstrapAnalyzer()
        result = analyzer.analyze(
            ALIGNED_SEQUENCES,
            LABELS,
            replicates=100,
            seed=42,  # For reproducibility
        )

        assert result.replicates == 100
        assert result.original_tree is not None
        assert len(result.support_values) > 0

    def test_support_values_range(self):
        """Support values should be between 0 and 100."""
        analyzer = BootstrapAnalyzer()
        result = analyzer.analyze(
            ALIGNED_SEQUENCES,
            LABELS,
            replicates=50,
            seed=42,
        )

        for value in result.support_values.values():
            assert 0 <= value <= 100

    def test_leaf_support_100(self):
        """Leaf nodes should always have 100% support."""
        analyzer = BootstrapAnalyzer()
        result = analyzer.analyze(
            ALIGNED_SEQUENCES,
            LABELS,
            replicates=50,
            seed=42,
        )

        for label in LABELS:
            assert result.support_values.get(label) == 100.0

    def test_bootstrap_reproducible_with_seed(self):
        """Same seed should give same results."""
        analyzer = BootstrapAnalyzer()

        result1 = analyzer.analyze(ALIGNED_SEQUENCES, LABELS, replicates=50, seed=42)
        result2 = analyzer.analyze(ALIGNED_SEQUENCES, LABELS, replicates=50, seed=42)

        assert result1.support_values == result2.support_values

    def test_bootstrap_result_serialization(self):
        """BootstrapResult should serialize and deserialize correctly."""
        analyzer = BootstrapAnalyzer()
        result = analyzer.analyze(
            ALIGNED_SEQUENCES,
            LABELS,
            replicates=10,
            seed=42,
        )

        data = result.to_dict()
        restored = BootstrapResult.from_dict(data)

        assert restored.replicates == result.replicates
        assert restored.support_values == result.support_values


# =============================================================================
# PhylogenyAnalyzer Tests
# =============================================================================


class TestPhylogenyAnalyzer:
    """Tests for PhylogenyAnalyzer (high-level API)."""

    def test_full_analysis(self):
        """Full analysis should return complete result."""
        analyzer = PhylogenyAnalyzer()
        result = analyzer.analyze(
            ALIGNED_SEQUENCES,
            LABELS,
            distance_method=DistanceMethod.JUKES_CANTOR,
            tree_method=TreeMethod.NEIGHBOR_JOINING,
            bootstrap_replicates=50,
            bootstrap_seed=42,
        )

        assert result.analysis_id is not None
        assert result.distance_matrix is not None
        assert result.tree is not None
        assert result.bootstrap is not None
        assert result.sequence_count == 3
        assert result.alignment_length == 10

    def test_without_bootstrap(self):
        """Analysis without bootstrap should work."""
        analyzer = PhylogenyAnalyzer()
        result = analyzer.analyze(
            ALIGNED_SEQUENCES,
            LABELS,
            bootstrap_replicates=0,
        )

        assert result.bootstrap is None
        assert result.tree is not None

    def test_custom_analysis_id(self):
        """Should use provided analysis ID."""
        analyzer = PhylogenyAnalyzer()
        result = analyzer.analyze(
            ALIGNED_SEQUENCES,
            LABELS,
            analysis_id="custom-123",
        )

        assert result.analysis_id == "custom-123"

    def test_all_distance_methods(self):
        """Should work with all distance methods."""
        analyzer = PhylogenyAnalyzer()

        for method in DistanceMethod:
            result = analyzer.analyze(
                ALIGNED_SEQUENCES,
                LABELS,
                distance_method=method,
            )
            assert result.distance_matrix.method == method.value

    def test_all_tree_methods(self):
        """Should work with all tree methods."""
        analyzer = PhylogenyAnalyzer()

        for method in TreeMethod:
            result = analyzer.analyze(
                ALIGNED_SEQUENCES,
                LABELS,
                tree_method=method,
            )
            assert result.tree.method == method.value

    def test_phylogeny_result_serialization(self):
        """PhylogenyResult should serialize and deserialize correctly."""
        analyzer = PhylogenyAnalyzer()
        result = analyzer.analyze(
            ALIGNED_SEQUENCES,
            LABELS,
            bootstrap_replicates=10,
            bootstrap_seed=42,
        )

        data = result.to_dict()
        restored = PhylogenyResult.from_dict(data)

        assert restored.analysis_id == result.analysis_id
        assert restored.sequence_count == result.sequence_count
        assert restored.alignment_length == result.alignment_length

    def test_analyzer_name(self):
        """Analyzer should have correct name."""
        analyzer = PhylogenyAnalyzer()
        assert analyzer.name == "phylogeny"


# =============================================================================
# PhylogenyJob Tests
# =============================================================================


class TestPhylogenyJob:
    """Tests for PhylogenyJob model."""

    def test_job_serialization(self):
        """PhylogenyJob should serialize and deserialize correctly."""
        job = PhylogenyJob(
            analysisId="analysis-123",
            alignmentId="alignment-456",
            alignedSequences=ALIGNED_SEQUENCES,
            labels=LABELS,
            distanceMethod="kimura_2p",
            treeMethod="upgma",
            bootstrapReplicates=100,
            options={"custom": "option"},
        )

        data = job.to_dict()
        restored = PhylogenyJob.from_dict(data)

        assert restored.analysisId == job.analysisId
        assert restored.alignmentId == job.alignmentId
        assert restored.alignedSequences == job.alignedSequences
        assert restored.labels == job.labels
        assert restored.distanceMethod == job.distanceMethod
        assert restored.treeMethod == job.treeMethod
        assert restored.bootstrapReplicates == job.bootstrapReplicates
        assert restored.options == job.options

    def test_job_defaults(self):
        """PhylogenyJob should have sensible defaults."""
        job = PhylogenyJob(
            analysisId="test",
            alignmentId="test",
            alignedSequences=ALIGNED_SEQUENCES,
            labels=LABELS,
        )

        assert job.distanceMethod == "jukes_cantor"
        assert job.treeMethod == "neighbor_joining"
        assert job.bootstrapReplicates == 0
        assert job.options == {}


# =============================================================================
# Event Tests
# =============================================================================


class TestPhylogenyEvents:
    """Tests for phylogeny events."""

    def test_phylogeny_completed_event(self):
        """PhylogenyCompleted should serialize correctly."""
        event = PhylogenyCompleted(
            analysisId="analysis-123",
            alignmentId="alignment-456",
            sequenceCount=5,
            treeMethod="neighbor_joining",
            distanceMethod="jukes_cantor",
            hasBootstrap=True,
            bootstrapReplicates=100,
            correlationId="corr-789",
        )

        assert event.category == "phylogeny"
        data = event._get_data()
        assert data["analysisId"] == "analysis-123"
        assert data["sequenceCount"] == 5
        assert data["hasBootstrap"] is True

    def test_phylogeny_failed_event(self):
        """PhylogenyFailed should serialize correctly."""
        event = PhylogenyFailed(
            analysisId="analysis-123",
            error="Something went wrong",
            errorType="ValueError",
        )

        assert event.category == "phylogeny"
        data = event._get_data()
        assert data["analysisId"] == "analysis-123"
        assert data["error"] == "Something went wrong"
        assert data["errorType"] == "ValueError"


# =============================================================================
# PhylogenyWorker Tests
# =============================================================================


class TestPhylogenyWorker:
    """Tests for PhylogenyWorker."""

    @pytest.fixture
    def mock_redis(self):
        """Create mock Redis client."""
        return MagicMock()

    @pytest.fixture
    def mock_publisher(self):
        """Create mock publisher."""
        publisher = MagicMock()
        publisher.publish = AsyncMock()
        return publisher

    @pytest.fixture
    def mock_settings(self):
        """Create mock settings."""
        settings = MagicMock()
        settings.jobs_stream_prefix = "geneflow:jobs"
        return settings

    @pytest.mark.asyncio
    async def test_process_job(self, mock_redis, mock_publisher, mock_settings):
        """Worker should process job and publish completion event."""
        from src.workers.phylogeny import PhylogenyWorker

        worker = PhylogenyWorker(mock_redis, mock_publisher, mock_settings)

        job_data = {
            "analysisId": "analysis-123",
            "alignmentId": "alignment-456",
            "alignedSequences": ALIGNED_SEQUENCES,
            "labels": LABELS,
            "distanceMethod": "jukes_cantor",
            "treeMethod": "neighbor_joining",
            "bootstrapReplicates": 0,
        }

        await worker.process_job("job-1", job_data)

        # Verify event was published
        mock_publisher.publish.assert_called_once()
        event = mock_publisher.publish.call_args[0][0]
        assert isinstance(event, PhylogenyCompleted)
        assert event.analysisId == "analysis-123"
        assert event.sequenceCount == 3

    @pytest.mark.asyncio
    async def test_process_job_with_bootstrap(
        self, mock_redis, mock_publisher, mock_settings
    ):
        """Worker should handle bootstrap analysis."""
        from src.workers.phylogeny import PhylogenyWorker

        worker = PhylogenyWorker(mock_redis, mock_publisher, mock_settings)

        job_data = {
            "analysisId": "analysis-123",
            "alignmentId": "alignment-456",
            "alignedSequences": ALIGNED_SEQUENCES,
            "labels": LABELS,
            "distanceMethod": "jukes_cantor",
            "treeMethod": "neighbor_joining",
            "bootstrapReplicates": 50,
        }

        await worker.process_job("job-1", job_data)

        event = mock_publisher.publish.call_args[0][0]
        assert event.hasBootstrap is True
        assert event.bootstrapReplicates == 50

    @pytest.mark.asyncio
    async def test_process_job_failure(
        self, mock_redis, mock_publisher, mock_settings
    ):
        """Worker should publish failure event on error."""
        from src.workers.phylogeny import PhylogenyWorker

        worker = PhylogenyWorker(mock_redis, mock_publisher, mock_settings)

        # Invalid job data (different length sequences)
        job_data = {
            "analysisId": "analysis-123",
            "alignmentId": "alignment-456",
            "alignedSequences": ["ATGCAT", "ATG"],  # Different lengths
            "labels": ["a", "b"],
            "distanceMethod": "jukes_cantor",
            "treeMethod": "neighbor_joining",
            "bootstrapReplicates": 0,
        }

        with pytest.raises(ValueError):
            await worker.process_job("job-1", job_data)

        # Verify failure event was published
        event = mock_publisher.publish.call_args[0][0]
        assert isinstance(event, PhylogenyFailed)
        assert event.analysisId == "analysis-123"

    def test_worker_name(self, mock_redis, mock_publisher, mock_settings):
        """Worker should have correct name."""
        from src.workers.phylogeny import PhylogenyWorker

        worker = PhylogenyWorker(mock_redis, mock_publisher, mock_settings)
        assert worker.name == "PhylogenyWorker"

    def test_stream_name(self, mock_redis, mock_publisher, mock_settings):
        """Worker should use correct stream name."""
        from src.workers.phylogeny import PhylogenyWorker

        worker = PhylogenyWorker(mock_redis, mock_publisher, mock_settings)
        assert worker.stream_name == "geneflow:jobs:phylogeny"
