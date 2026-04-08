"""
Trimming Predictor Model.

Predicts optimal trim points (start, end) for DNA sequences based on quality scores.

Uses a simple 1D CNN architecture - appropriate for the task complexity.
"""

from dataclasses import dataclass
from pathlib import Path

import numpy as np
import torch
import torch.nn as nn


@dataclass
class TrimmingConfig:
    """Configuration for TrimmingPredictor."""

    max_length: int = 500
    hidden_dim: int = 64  # Smaller hidden dim
    num_layers: int = 4   # Number of conv layers
    dropout: float = 0.5  # Strong dropout to prevent overfitting
    kernel_size: int = 7  # Conv kernel size


class TrimmingPredictor(nn.Module):
    """
    Predicts optimal trim positions for sequences.

    Input: Quality scores (batch, seq_len)
    Output: Trim positions (batch, 2) -> [start_ratio, end_ratio] in [0, 1]

    Uses a simple 1D CNN - the task is to detect quality drop-off patterns
    at sequence ends, which is a local pattern detection task well-suited for CNNs.
    """

    def __init__(self, config: TrimmingConfig):
        super().__init__()
        self.config = config

        # Build conv layers with increasing receptive field
        layers = []
        in_channels = 1

        for i in range(config.num_layers):
            out_channels = config.hidden_dim
            layers.extend([
                nn.Conv1d(
                    in_channels,
                    out_channels,
                    kernel_size=config.kernel_size,
                    padding=config.kernel_size // 2,
                ),
                nn.BatchNorm1d(out_channels),
                nn.ReLU(),
                nn.Dropout(config.dropout),
            ])
            in_channels = out_channels

        self.conv_layers = nn.Sequential(*layers)

        # Global pooling + local stats from ends
        # We extract: global avg, global max, start region avg, end region avg
        pooled_dim = config.hidden_dim * 4

        # Output head with strong regularization
        self.output_head = nn.Sequential(
            nn.Linear(pooled_dim, config.hidden_dim),
            nn.ReLU(),
            nn.Dropout(config.dropout),
            nn.Linear(config.hidden_dim, 32),
            nn.ReLU(),
            nn.Dropout(config.dropout),
            nn.Linear(32, 2),
            nn.Sigmoid(),  # Output in [0, 1]
        )

    def forward(
        self,
        quality: torch.Tensor,
        mask: torch.Tensor | None = None,
    ) -> torch.Tensor:
        """
        Forward pass.

        Args:
            quality: Quality scores (batch, seq_len) normalized to [0, 1]
            mask: Valid position mask (batch, seq_len), unused but kept for API compat

        Returns:
            Trim ratios (batch, 2) -> [start_ratio, end_ratio]
        """
        batch_size, seq_len = quality.shape

        # Reshape for conv: (batch, 1, seq_len)
        x = quality.unsqueeze(1)

        # Apply conv layers
        x = self.conv_layers(x)  # (batch, hidden_dim, seq_len)

        # Multi-scale pooling to capture both global and local patterns
        # Global statistics
        global_avg = x.mean(dim=2)  # (batch, hidden_dim)
        global_max = x.max(dim=2)[0]  # (batch, hidden_dim)

        # Local statistics from ends (where trim decisions matter)
        end_region = seq_len // 5  # 20% from each end
        start_avg = x[:, :, :end_region].mean(dim=2)  # (batch, hidden_dim)
        end_avg = x[:, :, -end_region:].mean(dim=2)  # (batch, hidden_dim)

        # Concatenate all features
        pooled = torch.cat([global_avg, global_max, start_avg, end_avg], dim=1)

        # Predict trim positions
        trim_ratios = self.output_head(pooled)  # (batch, 2)

        return trim_ratios

    def predict(self, quality: np.ndarray) -> tuple[int, int]:
        """
        Predict trim positions for a single sequence.

        Args:
            quality: Quality scores array

        Returns:
            Tuple of (start_pos, end_pos)
        """
        self.eval()
        with torch.no_grad():
            # Normalize quality to [0, 1]
            q_tensor = torch.tensor(quality, dtype=torch.float32) / 60.0
            q_tensor = q_tensor.unsqueeze(0)  # Add batch dim

            # Move to same device as model
            device = next(self.parameters()).device
            q_tensor = q_tensor.to(device)

            ratios = self(q_tensor)
            start_ratio = ratios[0, 0].item()
            end_ratio = ratios[0, 1].item()

            seq_len = len(quality)
            start_pos = int(start_ratio * seq_len)
            end_pos = int(end_ratio * seq_len)

            return start_pos, end_pos

    def save(self, path: Path) -> None:
        """Save model to file."""
        torch.save({
            'config': self.config,
            'state_dict': self.state_dict(),
        }, path)

    @classmethod
    def load(cls, path: Path) -> 'TrimmingPredictor':
        """Load model from file."""
        checkpoint = torch.load(path, map_location='cpu', weights_only=False)
        model = cls(checkpoint['config'])
        model.load_state_dict(checkpoint['state_dict'])
        return model
