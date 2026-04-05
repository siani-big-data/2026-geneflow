"""
Heterozygote Classifier Model.

Classifies positions as homozygous or heterozygous based on sequence context and quality.
"""

from dataclasses import dataclass
from pathlib import Path

import torch
import torch.nn as nn
import numpy as np


@dataclass
class HeterozygoteConfig:
    """Configuration for HeterozygoteClassifier."""

    max_length: int = 500
    input_channels: int = 4  # 4 for ACGT only, 8 for enhanced features
    context_size: int = 11  # Window around each position
    hidden_dim: int = 128  # Increased from 64
    num_layers: int = 5  # Number of conv blocks
    dropout: float = 0.3  # Increased to reduce overfitting
    num_classes: int = 2  # Homo, Hetero


class ResidualBlock(nn.Module):
    """Residual convolutional block."""

    def __init__(self, channels: int, kernel_size: int, dropout: float = 0.1):
        super().__init__()
        padding = kernel_size // 2
        self.conv1 = nn.Conv1d(channels, channels, kernel_size, padding=padding)
        self.bn1 = nn.BatchNorm1d(channels)
        self.conv2 = nn.Conv1d(channels, channels, kernel_size, padding=padding)
        self.bn2 = nn.BatchNorm1d(channels)
        self.dropout = nn.Dropout(dropout)
        self.relu = nn.ReLU()

    def forward(self, x):
        residual = x
        out = self.relu(self.bn1(self.conv1(x)))
        out = self.dropout(out)
        out = self.bn2(self.conv2(out))
        out = out + residual
        return self.relu(out)


class HeterozygoteClassifier(nn.Module):
    """
    Classifies each position as homozygous or heterozygous.

    Input:
        - signals: (batch, 4, seq_len) - Normalized chromatogram signals (A, C, G, T)
          Each position sums to 1. The model must learn to detect when two channels
          have similar high values (heterozygous) vs one dominant channel (homozygous).

    Output:
        - classifications: (batch, seq_len, 2) probabilities
    """

    def __init__(self, config: HeterozygoteConfig):
        super().__init__()
        self.config = config

        # Initial projection from input channels to hidden_dim
        self.input_proj = nn.Sequential(
            nn.Conv1d(config.input_channels, config.hidden_dim, kernel_size=config.context_size, padding=config.context_size // 2),
            nn.BatchNorm1d(config.hidden_dim),
            nn.ReLU(),
        )

        # Stack of residual blocks with varying kernel sizes
        kernel_sizes = [7, 5, 5, 3, 3][:config.num_layers]
        self.res_blocks = nn.ModuleList([
            ResidualBlock(config.hidden_dim, ks, config.dropout)
            for ks in kernel_sizes
        ])

        # Per-position classifier with more capacity
        self.classifier = nn.Sequential(
            nn.Conv1d(config.hidden_dim, config.hidden_dim // 2, kernel_size=1),
            nn.ReLU(),
            nn.Dropout(config.dropout),
            nn.Conv1d(config.hidden_dim // 2, 32, kernel_size=1),
            nn.ReLU(),
            nn.Conv1d(32, config.num_classes, kernel_size=1),
        )

    def forward(
        self,
        signals: torch.Tensor,
        mask: torch.Tensor | None = None,
    ) -> torch.Tensor:
        """
        Forward pass.

        Args:
            signals: Chromatogram signals (batch, 4, seq_len) - A, C, G, T channels
            mask: Valid positions (batch, seq_len)

        Returns:
            Logits (batch, seq_len, num_classes)
        """
        # Project to hidden dimension
        x = self.input_proj(signals)  # (batch, hidden_dim, seq_len)

        # Apply residual blocks
        for block in self.res_blocks:
            x = block(x)

        # Classify each position
        logits = self.classifier(x)  # (batch, num_classes, seq_len)

        # Transpose to (batch, seq_len, num_classes)
        logits = logits.permute(0, 2, 1)

        return logits

    def predict(self, signals: np.ndarray) -> np.ndarray:
        """
        Predict heterozygosity for each position from chromatogram signals.

        Args:
            signals: (channels, seq_len) normalized chromatogram signals

        Returns:
            Array of probabilities of being heterozygous
        """
        self.eval()
        with torch.no_grad():
            signals_tensor = torch.tensor(signals, dtype=torch.float32).unsqueeze(0)

            # Move to same device as model
            device = next(self.parameters()).device
            signals_tensor = signals_tensor.to(device)

            logits = self(signals_tensor)
            probs = torch.softmax(logits, dim=-1)

            # Return probability of heterozygous (class 1)
            return probs[0, :, 1].cpu().numpy()

    def save(self, path: Path) -> None:
        torch.save({'config': self.config, 'state_dict': self.state_dict()}, path)

    @classmethod
    def load(cls, path: Path) -> 'HeterozygoteClassifier':
        checkpoint = torch.load(path, map_location='cpu', weights_only=False)
        model = cls(checkpoint['config'])
        model.load_state_dict(checkpoint['state_dict'])
        return model
