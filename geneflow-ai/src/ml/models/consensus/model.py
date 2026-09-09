"""
Consensus Predictor Model.

Predicts consensus base from multiple aligned reads at each position.
"""

from dataclasses import dataclass
from pathlib import Path

import numpy as np
import torch
import torch.nn as nn


@dataclass
class ConsensusConfig:
    """Configuration for ConsensusPredictor."""

    max_reads: int = 10  # Max number of aligned reads
    max_length: int = 500
    hidden_dim: int = 64
    num_bases: int = 5  # A, C, G, T, N (gap/unknown)


class ConsensusPredictor(nn.Module):
    """
    Predicts consensus base from multiple aligned reads.

    Input:
        - reads: (batch, num_reads, 5, seq_len) - One-hot encoded reads with quality as 5th channel
        - coverage: (batch, seq_len) - Number of reads at each position

    Output:
        - consensus: (batch, seq_len, 4) - Probability of each base (A, C, G, T)
    """

    def __init__(self, config: ConsensusConfig):
        super().__init__()
        self.config = config

        # Process each read independently first
        self.read_encoder = nn.Sequential(
            nn.Conv1d(5, config.hidden_dim, kernel_size=3, padding=1),
            nn.ReLU(),
            nn.Conv1d(config.hidden_dim, config.hidden_dim, kernel_size=3, padding=1),
            nn.ReLU(),
        )

        # Attention over reads at each position
        self.read_attention = nn.Sequential(
            nn.Linear(config.hidden_dim, config.hidden_dim // 2),
            nn.Tanh(),
            nn.Linear(config.hidden_dim // 2, 1),
        )

        # Final predictor
        self.predictor = nn.Sequential(
            nn.Conv1d(config.hidden_dim + 1, config.hidden_dim, kernel_size=3, padding=1),
            nn.ReLU(),
            nn.Conv1d(config.hidden_dim, 4, kernel_size=1),  # 4 bases
        )

    def forward(
        self,
        reads: torch.Tensor,
        coverage: torch.Tensor,
    ) -> torch.Tensor:
        """
        Forward pass.

        Args:
            reads: (batch, num_reads, 5, seq_len)
            coverage: (batch, seq_len)

        Returns:
            Logits (batch, seq_len, 4)
        """
        batch_size, num_reads, channels, seq_len = reads.shape

        # Encode each read
        reads_flat = reads.view(batch_size * num_reads, channels, seq_len)
        encoded = self.read_encoder(reads_flat)  # (batch*num_reads, hidden, seq_len)
        # Reshape: (batch, num_reads, hidden, seq_len)
        encoded = encoded.view(batch_size, num_reads, -1, seq_len)

        # Permute for attention: (batch, seq_len, num_reads, hidden)
        encoded = encoded.permute(0, 3, 1, 2)

        # Attention weights over reads
        attn_scores = self.read_attention(encoded)  # (batch, seq_len, num_reads, 1)
        attn_weights = torch.softmax(attn_scores, dim=2)

        # Weighted sum over reads
        aggregated = (encoded * attn_weights).sum(dim=2)  # (batch, seq_len, hidden)

        # Permute back: (batch, hidden, seq_len)
        aggregated = aggregated.permute(0, 2, 1)

        # Add coverage info
        coverage_expanded = coverage.unsqueeze(1) / self.config.max_reads  # Normalize
        combined = torch.cat([aggregated, coverage_expanded], dim=1)

        # Predict consensus
        logits = self.predictor(combined)  # (batch, 4, seq_len)
        logits = logits.permute(0, 2, 1)  # (batch, seq_len, 4)

        return logits

    def predict(self, reads: list[tuple[str, np.ndarray]]) -> str:
        """
        Predict consensus from list of (sequence, quality) tuples.

        Returns:
            Consensus sequence
        """
        self.eval()
        if not reads:
            return ""

        with torch.no_grad():
            # Find max length
            max_len = max(len(seq) for seq, _ in reads)
            num_reads = min(len(reads), self.config.max_reads)

            # Encode reads
            reads_tensor = torch.zeros(1, num_reads, 5, max_len)
            coverage = torch.zeros(1, max_len)

            mapping = {'A': 0, 'C': 1, 'G': 2, 'T': 3, 'N': 4, '-': 4}

            for r_idx, (seq, qual) in enumerate(reads[:num_reads]):
                for i, base in enumerate(seq):
                    if i < max_len:
                        if base.upper() in mapping:
                            reads_tensor[0, r_idx, mapping[base.upper()], i] = 1.0
                        # Add quality as 5th channel (normalized)
                        if i < len(qual):
                            reads_tensor[0, r_idx, 4, i] = qual[i] / 60.0
                        coverage[0, i] += 1

            logits = self(reads_tensor, coverage)
            pred_bases = logits.argmax(dim=-1)[0].numpy()

            # Convert to sequence
            base_map = {0: 'A', 1: 'C', 2: 'G', 3: 'T'}
            consensus = ''.join(base_map.get(b, 'N') for b in pred_bases)

            return consensus

    def save(self, path: Path) -> None:
        torch.save({'config': self.config, 'state_dict': self.state_dict()}, path)

    @classmethod
    def load(cls, path: Path) -> 'ConsensusPredictor':
        checkpoint = torch.load(path, map_location='cpu', weights_only=False)
        model = cls(checkpoint['config'])
        model.load_state_dict(checkpoint['state_dict'])
        return model
