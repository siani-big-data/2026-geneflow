"""Quality Predictor Model.

Predicts Phred quality scores from chromatogram signals.
Architecture: 1D CNN with dilated convolutions for multi-scale feature extraction.

Input: 4-channel signal (A, T, C, G) at each position
Output: Quality score (0-60) per position
"""

from dataclasses import dataclass, field

import numpy as np
import torch
import torch.nn as nn
import torch.nn.functional as F

from ..base import BaseModel, ModelConfig


@dataclass
class QualityPredictorConfig(ModelConfig):
    """Configuration for Quality Predictor."""

    name: str = "quality_predictor"
    version: str = "1.0.0"
    input_channels: int = 4  # A, T, C, G signals
    hidden_channels: int = 64
    num_layers: int = 6
    kernel_size: int = 7
    dropout: float = 0.1
    use_residual: bool = True
    use_batch_norm: bool = True
    max_quality: float = 60.0  # Max Phred score
    extra: dict = field(default_factory=dict)


class DilatedConvBlock(nn.Module):
    """Dilated convolution block with optional residual connection."""

    def __init__(
        self,
        in_channels: int,
        out_channels: int,
        kernel_size: int,
        dilation: int,
        dropout: float = 0.1,
        use_residual: bool = True,
        use_batch_norm: bool = True,
    ):
        super().__init__()
        self.use_residual = use_residual and (in_channels == out_channels)

        padding = (kernel_size - 1) * dilation // 2

        self.conv = nn.Conv1d(
            in_channels,
            out_channels,
            kernel_size,
            padding=padding,
            dilation=dilation,
        )
        self.bn = nn.BatchNorm1d(out_channels) if use_batch_norm else nn.Identity()
        self.dropout = nn.Dropout(dropout)
        self.activation = nn.GELU()

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        residual = x

        out = self.conv(x)
        out = self.bn(out)
        out = self.activation(out)
        out = self.dropout(out)

        if self.use_residual:
            out = out + residual

        return out


class QualityPredictor(BaseModel):
    """CNN model for predicting quality scores from chromatogram signals.

    Architecture:
    - Input projection: 4 channels -> hidden_channels
    - Stack of dilated conv blocks with increasing dilation
    - Output projection: hidden_channels -> 1 (quality score)
    """

    def __init__(self, config: QualityPredictorConfig | None = None):
        config = config or QualityPredictorConfig()
        super().__init__(config)
        self.cfg = config

        # Input projection
        self.input_proj = nn.Sequential(
            nn.Conv1d(config.input_channels, config.hidden_channels, kernel_size=1),
            nn.BatchNorm1d(config.hidden_channels) if config.use_batch_norm else nn.Identity(),
            nn.GELU(),
        )

        # Dilated conv blocks
        self.blocks = nn.ModuleList()
        for i in range(config.num_layers):
            dilation = 2 ** (i % 4)  # 1, 2, 4, 8, 1, 2, ...
            self.blocks.append(
                DilatedConvBlock(
                    in_channels=config.hidden_channels,
                    out_channels=config.hidden_channels,
                    kernel_size=config.kernel_size,
                    dilation=dilation,
                    dropout=config.dropout,
                    use_residual=config.use_residual,
                    use_batch_norm=config.use_batch_norm,
                )
            )

        # Output projection
        self.output_proj = nn.Sequential(
            nn.Conv1d(config.hidden_channels, config.hidden_channels // 2, kernel_size=1),
            nn.GELU(),
            nn.Conv1d(config.hidden_channels // 2, 1, kernel_size=1),
        )

        # Initialize weights
        self._init_weights()

    def _init_weights(self):
        """Initialize weights."""
        for m in self.modules():
            if isinstance(m, nn.Conv1d):
                nn.init.kaiming_normal_(m.weight, mode="fan_out", nonlinearity="relu")
                if m.bias is not None:
                    nn.init.zeros_(m.bias)
            elif isinstance(m, nn.BatchNorm1d):
                nn.init.ones_(m.weight)
                nn.init.zeros_(m.bias)

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        """Forward pass.

        Args:
            x: Input tensor of shape (batch, 4, seq_len)

        Returns:
            Quality scores of shape (batch, seq_len)
        """
        # Input projection
        h = self.input_proj(x)  # (batch, hidden, seq_len)

        # Dilated conv blocks
        for block in self.blocks:
            h = block(h)

        # Output projection
        out = self.output_proj(h)  # (batch, 1, seq_len)
        out = out.squeeze(1)  # (batch, seq_len)

        # Clamp to valid quality range
        out = torch.clamp(out, 0.0, self.cfg.max_quality)

        return out

    def predict(self, signals: np.ndarray | torch.Tensor) -> np.ndarray:
        """Predict quality scores from signals.

        Args:
            signals: Signal array of shape (4, seq_len) or (batch, 4, seq_len)

        Returns:
            Quality scores of shape (seq_len,) or (batch, seq_len)
        """
        self.eval()

        # Convert to tensor
        if isinstance(signals, np.ndarray):
            signals = torch.tensor(signals, dtype=torch.float32)

        # Add batch dimension if needed
        if signals.dim() == 2:
            signals = signals.unsqueeze(0)
            squeeze_output = True
        else:
            squeeze_output = False

        # Move to device
        signals = signals.to(self.device)

        # Predict
        with torch.no_grad():
            quality = self.forward(signals)

        # Convert to numpy
        quality = quality.cpu().numpy()

        if squeeze_output:
            quality = quality.squeeze(0)

        return quality

    def predict_with_confidence(
        self, signals: np.ndarray | torch.Tensor, n_samples: int = 10
    ) -> tuple[np.ndarray, np.ndarray]:
        """Predict with uncertainty estimation using MC Dropout.

        Args:
            signals: Signal array
            n_samples: Number of forward passes for uncertainty

        Returns:
            Tuple of (mean_quality, std_quality)
        """
        self.train()  # Enable dropout

        if isinstance(signals, np.ndarray):
            signals = torch.tensor(signals, dtype=torch.float32)

        if signals.dim() == 2:
            signals = signals.unsqueeze(0)

        signals = signals.to(self.device)

        predictions = []
        with torch.no_grad():
            for _ in range(n_samples):
                pred = self.forward(signals)
                predictions.append(pred.cpu().numpy())

        predictions = np.stack(predictions)
        mean_quality = predictions.mean(axis=0).squeeze()
        std_quality = predictions.std(axis=0).squeeze()

        self.eval()
        return mean_quality, std_quality


class QualityLoss(nn.Module):
    """Loss function for quality prediction.

    Combines MSE loss with gradient penalty for smoothness.
    """

    def __init__(self, mse_weight: float = 1.0, smooth_weight: float = 0.1):
        super().__init__()
        self.mse_weight = mse_weight
        self.smooth_weight = smooth_weight

    def forward(
        self,
        predictions: torch.Tensor,
        targets: torch.Tensor,
        mask: torch.Tensor | None = None,
    ) -> torch.Tensor:
        """Compute loss.

        Args:
            predictions: Predicted quality scores (batch, seq_len)
            targets: Target quality scores (batch, seq_len)
            mask: Valid position mask (batch, seq_len)
        """
        if mask is not None:
            predictions = predictions[mask]
            targets = targets[mask]

        # MSE loss
        mse_loss = F.mse_loss(predictions, targets)

        # Smoothness loss (penalize large jumps)
        if self.smooth_weight > 0 and predictions.dim() > 1:
            diff = predictions[:, 1:] - predictions[:, :-1]
            smooth_loss = (diff**2).mean()
        else:
            smooth_loss = torch.tensor(0.0, device=predictions.device)

        return self.mse_weight * mse_loss + self.smooth_weight * smooth_loss
