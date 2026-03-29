"""Tests for ML datasets."""

import tempfile
from pathlib import Path

import numpy as np
import pytest
import torch

from src.ml.datasets.sequence_dataset import SequenceDataset


class TestSequenceDataset:
    """Tests for SequenceDataset."""

    @pytest.fixture
    def sequences(self):
        return ["ATGCGATCGATCG", "TTTTAAAACCCCGGGG", "ATATATATATAT"]

    def test_init_with_sequences(self, sequences):
        dataset = SequenceDataset(sequences=sequences)
        assert len(dataset) == 3

    def test_getitem(self, sequences):
        dataset = SequenceDataset(sequences=sequences, max_length=20)
        item = dataset[0]

        assert "indices" in item
        assert "mask" in item
        assert "one_hot" in item
        assert item["indices"].shape == (20,)
        assert item["one_hot"].shape == (20, 4)

    def test_encoding(self, sequences):
        dataset = SequenceDataset(sequences=sequences)
        indices = dataset.encode_sequence("ATCG")

        assert list(indices) == [0, 1, 2, 3]

    def test_one_hot(self, sequences):
        dataset = SequenceDataset(sequences=sequences)
        indices = np.array([0, 1, 2, 3])
        one_hot = dataset.to_one_hot(indices)

        expected = np.array(
            [
                [1, 0, 0, 0],
                [0, 1, 0, 0],
                [0, 0, 1, 0],
                [0, 0, 0, 1],
            ],
            dtype=np.float32,
        )
        np.testing.assert_array_equal(one_hot, expected)

    def test_with_labels(self):
        sequences = ["ATGC", "TTTT"]
        labels = [0.5, 0.8]
        dataset = SequenceDataset(sequences=sequences, labels=labels)

        item = dataset[0]
        assert "label" in item
        assert item["label"] == 0.5

    def test_fasta_loading(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            fasta_path = Path(tmpdir) / "test.fasta"
            with open(fasta_path, "w") as f:
                f.write(">seq1\nATGCGATCGA\n>seq2\nTTTTAAAA\n")

            dataset = SequenceDataset(fasta_file=fasta_path)
            assert len(dataset) == 2

    def test_padding(self):
        dataset = SequenceDataset(sequences=["ATGC"], max_length=10)
        item = dataset[0]

        # First 4 positions should have mask=True
        assert item["mask"][:4].all()
        assert not item["mask"][4:].any()

    def test_truncation(self):
        long_seq = "A" * 100
        dataset = SequenceDataset(sequences=[long_seq], max_length=50)
        item = dataset[0]

        assert item["indices"].shape == (50,)
        assert item["mask"].all()
