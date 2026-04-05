"""Enhanced Quality Predictor with auxiliary signal features.

This model extends the base Quality Predictor to incorporate
additional signal features like SNR, peak clarity, and entropy
for improved quality_enhanced score prediction.
"""

from dataclasses import dataclass, field

import numpy as np
import torch
import torch.nn as nn
import torch.nn.functional as F

from ..base import BaseModel, ModelConfig


@dataclass
class EnhancedQualityConfig(ModelConfig):
    """Configuration for Enhanced Quality Predictor."""

    name: str = "enhanced_quality_predictor"
    version: str = "1.0.0"

    # Base signal channels
    input_channels: int = 4  # A, T, C, G signals

    # Auxiliary feature channels
    aux_channels: int = 8  # SNR, clarity, entropy, confidence, height, gradient, noise, smoothness

    # Architecture
    hidden_channels: int = 64
    num_layers: int = 6
    kernel_size: int = 7
    dropout: float = 0.1
    use_residual: bool = True
    use_batch_norm: bool = True

    # Global context
    use_global_context: bool = True
    global_features_dim: int = 5  # mean, max, std, range, baseline

    # Output
    max_quality: float = 60.0

    extra: dict = field(default_factory=dict)


class DilatedConvBlock(nn.Module):
    """Dilated convolution block with optional residual."""

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
            in_channels, out_channels, kernel_size,
            padding=padding, dilation=dilation,
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


class GlobalContextModule(nn.Module):
    """Incorporates global statistics into per-position predictions."""

    def __init__(self, hidden_channels: int, global_dim: int):
        super().__init__()

        self.fc = nn.Sequential(
            nn.Linear(global_dim, hidden_channels),
            nn.GELU(),
            nn.Linear(hidden_channels, hidden_channels),
        )

    def forward(
        self, x: torch.Tensor, global_stats: torch.Tensor
    ) -> torch.Tensor:
        """Add global context to sequence features.

        Args:
            x: (batch, hidden, seq_len) local features
            global_stats: (batch, global_dim) global statistics

        Returns:
            (batch, hidden, seq_len) enriched features
        """
        # Project global stats
        context = self.fc(global_stats)  # (batch, hidden)

        # Broadcast and add to all positions
        context = context.unsqueeze(-1)  # (batch, hidden, 1)
        return x + context


class EnhancedQualityPredictor(BaseModel):
    """Enhanced CNN model for quality_enhanced prediction.

    This model extends the base quality_enhanced predictor by:
    1. Accepting auxiliary feature channels (SNR, clarity, etc.)
    2. Incorporating global signal statistics
    3. Using deeper feature fusion

    Input shapes:
    - signals: (batch, 4, seq_len) - Base ATCG signals
    - aux_features: (batch, 8, seq_len) - Auxiliary features
    - global_stats: (batch, 5) - Global statistics

    Output: (batch, seq_len) - Quality scores
    """

    def __init__(self, config: EnhancedQualityConfig | None = None):
        config = config or EnhancedQualityConfig()
        super().__init__(config)
        self.cfg = config

        total_input_channels = config.input_channels + config.aux_channels

        # Input projection
        self.input_proj = nn.Sequential(
            nn.Conv1d(total_input_channels, config.hidden_channels, kernel_size=1),
            nn.BatchNorm1d(config.hidden_channels) if config.use_batch_norm else nn.Identity(),
            nn.GELU(),
        )

        # Dilated conv blocks
        self.blocks = nn.ModuleList()
        for i in range(config.num_layers):
            dilation = 2 ** (i % 4)
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

        # Global context module
        if config.use_global_context:
            self.global_context = GlobalContextModule(
                config.hidden_channels,
                config.global_features_dim,
            )
        else:
            self.global_context = None

        # Output projection
        self.output_proj = nn.Sequential(
            nn.Conv1d(config.hidden_channels, config.hidden_channels // 2, kernel_size=1),
            nn.GELU(),
            nn.Conv1d(config.hidden_channels // 2, 1, kernel_size=1),
        )

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
            elif isinstance(m, nn.Linear):
                nn.init.xavier_normal_(m.weight)
                if m.bias is not None:
                    nn.init.zeros_(m.bias)

    def forward(
        self,
        signals: torch.Tensor,
        aux_features: torch.Tensor | None = None,
        global_stats: torch.Tensor | None = None,
    ) -> torch.Tensor:
        """Forward pass.

        Args:
            signals: (batch, 4, seq_len) Base signal channels
            aux_features: (batch, 8, seq_len) Auxiliary features (optional)
            global_stats: (batch, 5) Global statistics (optional)

        Returns:
            Quality scores of shape (batch, seq_len)
        """
        # Concatenate signals and aux features
        if aux_features is not None:
            x = torch.cat([signals, aux_features], dim=1)
        else:
            # Pad with zeros if no aux features
            batch, _, seq_len = signals.shape
            zeros = torch.zeros(
                batch, self.cfg.aux_channels, seq_len,
                device=signals.device, dtype=signals.dtype,
            )
            x = torch.cat([signals, zeros], dim=1)

        # Input projection
        h = self.input_proj(x)

        # Dilated conv blocks
        for block in self.blocks:
            h = block(h)

        # Add global context
        if self.global_context is not None and global_stats is not None:
            h = self.global_context(h, global_stats)

        # Output projection
        out = self.output_proj(h)
        out = out.squeeze(1)

        # Clamp to valid range
        out = torch.clamp(out, 0.0, self.cfg.max_quality)

        return out

    def predict(
        self,
        signals: np.ndarray | torch.Tensor,
        aux_features: np.ndarray | torch.Tensor | None = None,
        global_stats: np.ndarray | torch.Tensor | None = None,
    ) -> np.ndarray:
        """Predict quality_enhanced scores.

        Args:
            signals: Signal array (4, seq_len) or (batch, 4, seq_len)
            aux_features: Aux features (8, seq_len) or (batch, 8, seq_len)
            global_stats: Global stats (5,) or (batch, 5)

        Returns:
            Quality scores
        """
        self.eval()

        # Convert to tensors
        if isinstance(signals, np.ndarray):
            signals = torch.tensor(signals, dtype=torch.float32)
        if aux_features is not None and isinstance(aux_features, np.ndarray):
            aux_features = torch.tensor(aux_features, dtype=torch.float32)
        if global_stats is not None and isinstance(global_stats, np.ndarray):
            global_stats = torch.tensor(global_stats, dtype=torch.float32)

        # Add batch dimension
        squeeze_output = False
        if signals.dim() == 2:
            signals = signals.unsqueeze(0)
            squeeze_output = True
            if aux_features is not None:
                aux_features = aux_features.unsqueeze(0)
            if global_stats is not None:
                global_stats = global_stats.unsqueeze(0)

        # Move to device
        signals = signals.to(self.device)
        if aux_features is not None:
            aux_features = aux_features.to(self.device)
        if global_stats is not None:
            global_stats = global_stats.to(self.device)

        # Predict
        with torch.no_grad():
            quality = self.forward(signals, aux_features, global_stats)

        quality = quality.cpu().numpy()

        if squeeze_output:
            quality = quality.squeeze(0)

        return quality


class EnhancedQualityLoss(nn.Module):
    """Loss function for enhanced quality_enhanced prediction.

    Combines:
    - MSE loss for quality_enhanced prediction
    - Smoothness penalty for temporal consistency
    - SNR-weighted loss to focus on reliable positions
    """

    def __init__(
        self,
        mse_weight: float = 1.0,
        smooth_weight: float = 0.1,
        snr_weight: float = 0.1,
    ):
        super().__init__()
        self.mse_weight = mse_weight
        self.smooth_weight = smooth_weight
        self.snr_weight = snr_weight

    def forward(
        self,
        predictions: torch.Tensor,
        targets: torch.Tensor,
        mask: torch.Tensor | None = None,
        snr: torch.Tensor | None = None,
    ) -> torch.Tensor:
        """Compute loss.

        Args:
            predictions: (batch, seq_len) predicted quality_enhanced
            targets: (batch, seq_len) target quality_enhanced
            mask: (batch, seq_len) valid position mask
            snr: (batch, seq_len) signal-to-noise ratio for weighting
        """
        # Apply mask
        if mask is not None:
            predictions = predictions[mask]
            targets = targets[mask]
            if snr is not None:
                snr = snr[mask]

        # Base MSE loss
        mse_loss = F.mse_loss(predictions, targets)

        # Smoothness loss
        smooth_loss = torch.tensor(0.0, device=predictions.device)
        if self.smooth_weight > 0 and predictions.dim() > 0:
            if len(predictions) > 1:
                diff = predictions[1:] - predictions[:-1]
                smooth_loss = (diff ** 2).mean()

        # SNR-weighted loss
        snr_loss = torch.tensor(0.0, device=predictions.device)
        if self.snr_weight > 0 and snr is not None:
            # Higher weight for high-SNR positions
            snr_weights = torch.sigmoid(snr - 2.0)  # Centered around SNR=2
            snr_weights = snr_weights / (snr_weights.mean() + 1e-6)
            weighted_mse = snr_weights * (predictions - targets) ** 2
            snr_loss = weighted_mse.mean()

        total_loss = (
            self.mse_weight * mse_loss
            + self.smooth_weight * smooth_loss
            + self.snr_weight * snr_loss
        )

        return total_loss
