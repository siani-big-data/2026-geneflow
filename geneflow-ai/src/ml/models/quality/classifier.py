"""Quality Classifier Model.

Classifies quality into bins (Q10, Q20, Q30, Q40, Q50) instead of
predicting exact Phred scores.

Two architectures available:
- QualityClassifier: Pointwise MLP (legacy, no spatial context)
- QualityClassifierCNN: CNN with dilated convolutions (recommended)
"""

from dataclasses import dataclass, field

import numpy as np
import torch
import torch.nn as nn
import torch.nn.functional as F

from ..base import BaseModel, ModelConfig


@dataclass
class QualityClassifierConfig(ModelConfig):
    """Configuration for Quality Classifier."""

    name: str = "quality_classifier"
    version: str = "1.0.0"
    input_channels: int = 7  # 4 ACGT + 3 peak features
    hidden_channels: int = 128
    num_layers: int = 4
    num_classes: int = 5  # Q10, Q20, Q30, Q40, Q50+
    dropout: float = 0.3
    use_batch_norm: bool = True
    extra: dict = field(default_factory=dict)


class ConvBlock(nn.Module):
    """Convolutional block with optional batch norm."""

    def __init__(
        self,
        in_channels: int,
        out_channels: int,
        kernel_size: int = 1,
        dropout: float = 0.1,
        use_batch_norm: bool = True,
    ):
        super().__init__()
        self.conv = nn.Conv1d(in_channels, out_channels, kernel_size, padding=kernel_size // 2)
        self.bn = nn.BatchNorm1d(out_channels) if use_batch_norm else nn.Identity()
        self.dropout = nn.Dropout(dropout)
        self.activation = nn.GELU()

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        out = self.conv(x)
        out = self.bn(out)
        out = self.activation(out)
        out = self.dropout(out)
        return out


class QualityClassifier(BaseModel):
    """Pointwise classifier for quality bins.

    Architecture: MLP with 1x1 convolutions (pointwise)
    - Each position classified independently
    - No spatial context (to avoid overfitting)
    - Output: class probabilities per position
    """

    def __init__(self, config: QualityClassifierConfig | None = None):
        config = config or QualityClassifierConfig()
        super().__init__(config)
        self.cfg = config

        # Build layers
        layers = []
        in_ch = config.input_channels

        for i in range(config.num_layers):
            out_ch = config.hidden_channels
            layers.append(ConvBlock(
                in_ch, out_ch,
                kernel_size=1,  # Pointwise
                dropout=config.dropout,
                use_batch_norm=config.use_batch_norm,
            ))
            in_ch = out_ch

        self.encoder = nn.Sequential(*layers)

        # Output head
        self.classifier = nn.Sequential(
            nn.Conv1d(config.hidden_channels, config.hidden_channels // 2, kernel_size=1),
            nn.GELU(),
            nn.Dropout(config.dropout),
            nn.Conv1d(config.hidden_channels // 2, config.num_classes, kernel_size=1),
        )

        self._init_weights()

    def _init_weights(self):
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
            x: Input tensor of shape (batch, channels, seq_len)

        Returns:
            Logits of shape (batch, num_classes, seq_len)
        """
        h = self.encoder(x)
        logits = self.classifier(h)  # (batch, num_classes, seq_len)
        return logits

    def predict(self, signals: np.ndarray | torch.Tensor) -> np.ndarray:
        """Predict class labels.

        Args:
            signals: Signal array of shape (channels, seq_len) or (batch, channels, seq_len)

        Returns:
            Class indices of shape (seq_len,) or (batch, seq_len)
        """
        self.eval()

        if isinstance(signals, np.ndarray):
            signals = torch.tensor(signals, dtype=torch.float32)

        if signals.dim() == 2:
            signals = signals.unsqueeze(0)
            squeeze_output = True
        else:
            squeeze_output = False

        signals = signals.to(self.device)

        with torch.no_grad():
            logits = self.forward(signals)
            classes = logits.argmax(dim=1)

        classes = classes.cpu().numpy()

        if squeeze_output:
            classes = classes.squeeze(0)

        return classes

    def predict_proba(self, signals: np.ndarray | torch.Tensor) -> np.ndarray:
        """Predict class probabilities.

        Args:
            signals: Signal array

        Returns:
            Class probabilities of shape (num_classes, seq_len) or (batch, num_classes, seq_len)
        """
        self.eval()

        if isinstance(signals, np.ndarray):
            signals = torch.tensor(signals, dtype=torch.float32)

        if signals.dim() == 2:
            signals = signals.unsqueeze(0)
            squeeze_output = True
        else:
            squeeze_output = False

        signals = signals.to(self.device)

        with torch.no_grad():
            logits = self.forward(signals)
            probs = F.softmax(logits, dim=1)

        probs = probs.cpu().numpy()

        if squeeze_output:
            probs = probs.squeeze(0)

        return probs


class FocalLoss(nn.Module):
    """Focal Loss for imbalanced classification.

    Reduces loss for well-classified examples, focuses on hard cases.
    """

    def __init__(
        self,
        alpha: torch.Tensor | None = None,
        gamma: float = 2.0,
        reduction: str = "mean",
    ):
        super().__init__()
        self.alpha = alpha  # Class weights
        self.gamma = gamma
        self.reduction = reduction

    def forward(
        self,
        inputs: torch.Tensor,
        targets: torch.Tensor,
        mask: torch.Tensor | None = None,
    ) -> torch.Tensor:
        """Compute focal loss.

        Args:
            inputs: Logits (batch, num_classes, seq_len)
            targets: Class indices (batch, seq_len)
            mask: Valid positions (batch, seq_len)
        """
        # Reshape for cross entropy: (N, C) and (N,)
        batch, num_classes, seq_len = inputs.shape
        inputs_flat = inputs.permute(0, 2, 1).reshape(-1, num_classes)  # (batch*seq, classes)
        targets_flat = targets.reshape(-1)  # (batch*seq,)

        # Compute cross entropy
        ce_loss = F.cross_entropy(inputs_flat, targets_flat, reduction="none")

        # Get predicted probabilities for correct class
        pt = torch.exp(-ce_loss)

        # Focal weight
        focal_weight = (1 - pt) ** self.gamma

        # Apply class weights
        if self.alpha is not None:
            alpha = self.alpha.to(inputs.device)
            alpha_t = alpha[targets_flat]
            focal_weight = focal_weight * alpha_t

        loss = focal_weight * ce_loss

        # Apply mask
        if mask is not None:
            mask_flat = mask.reshape(-1)
            loss = loss * mask_flat
            if self.reduction == "mean":
                return loss.sum() / (mask_flat.sum() + 1e-6)

        if self.reduction == "mean":
            return loss.mean()
        elif self.reduction == "sum":
            return loss.sum()
        return loss


# =============================================================================
# CNN Classifier with Spatial Context (Recommended)
# =============================================================================

@dataclass
class QualityClassifierCNNConfig(ModelConfig):
    """Configuration for CNN-based Quality Classifier."""

    name: str = "quality_classifier"
    version: str = "1.0.0"
    input_channels: int = 13  # All features from quality_context
    hidden_channels: int = 128
    num_layers: int = 6  # Number of residual blocks
    num_classes: int = 5  # Q10, Q20, Q30, Q40, Q50+
    kernel_size: int = 7  # Convolution kernel size
    dropout: float = 0.2
    use_batch_norm: bool = True
    use_dilations: bool = True  # Dilated convolutions for multi-scale context
    extra: dict = field(default_factory=dict)


class ResidualConvBlock(nn.Module):
    """Residual convolutional block with spatial context."""

    def __init__(
        self,
        channels: int,
        kernel_size: int = 7,
        dilation: int = 1,
        dropout: float = 0.1,
        use_batch_norm: bool = True,
    ):
        super().__init__()
        padding = (kernel_size - 1) * dilation // 2

        self.conv1 = nn.Conv1d(
            channels, channels, kernel_size,
            padding=padding, dilation=dilation, bias=not use_batch_norm
        )
        self.bn1 = nn.BatchNorm1d(channels) if use_batch_norm else nn.Identity()

        self.conv2 = nn.Conv1d(
            channels, channels, kernel_size,
            padding=padding, dilation=dilation, bias=not use_batch_norm
        )
        self.bn2 = nn.BatchNorm1d(channels) if use_batch_norm else nn.Identity()

        self.dropout = nn.Dropout(dropout)
        self.activation = nn.GELU()

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        residual = x

        out = self.conv1(x)
        out = self.bn1(out)
        out = self.activation(out)
        out = self.dropout(out)

        out = self.conv2(out)
        out = self.bn2(out)

        out = out + residual  # Skip connection
        out = self.activation(out)

        return out


class QualityClassifierCNN(BaseModel):
    """CNN classifier with spatial context for quality bins.

    Architecture: Dilated Residual CNN
    - Uses convolutions with kernel_size > 1 for spatial context
    - Dilated convolutions capture multi-scale patterns (1, 2, 4, ...)
    - Residual connections for stable training
    - Quality at position i depends on neighboring signal patterns

    This is the recommended architecture for quality classification.
    """

    def __init__(self, config: QualityClassifierCNNConfig | None = None):
        config = config or QualityClassifierCNNConfig()
        super().__init__(config)
        self.cfg = config

        # Input projection
        self.input_proj = nn.Sequential(
            nn.Conv1d(config.input_channels, config.hidden_channels, kernel_size=1),
            nn.BatchNorm1d(config.hidden_channels) if config.use_batch_norm else nn.Identity(),
            nn.GELU(),
        )

        # Residual blocks with increasing dilation
        self.blocks = nn.ModuleList()
        for i in range(config.num_layers):
            if config.use_dilations:
                # Cycle through dilations: 1, 2, 4, 1, 2, 4, ...
                dilation = 2 ** (i % 3)
            else:
                dilation = 1

            self.blocks.append(ResidualConvBlock(
                channels=config.hidden_channels,
                kernel_size=config.kernel_size,
                dilation=dilation,
                dropout=config.dropout,
                use_batch_norm=config.use_batch_norm,
            ))

        # Classification head
        self.classifier = nn.Sequential(
            nn.Conv1d(config.hidden_channels, config.hidden_channels // 2, kernel_size=1),
            nn.GELU(),
            nn.Dropout(config.dropout),
            nn.Conv1d(config.hidden_channels // 2, config.num_classes, kernel_size=1),
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

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        """Forward pass.

        Args:
            x: Input tensor of shape (batch, channels, seq_len)

        Returns:
            Logits of shape (batch, num_classes, seq_len)
        """
        # Project to hidden dimension
        h = self.input_proj(x)  # (batch, hidden, seq_len)

        # Apply residual blocks
        for block in self.blocks:
            h = block(h)

        # Classification head
        logits = self.classifier(h)  # (batch, num_classes, seq_len)

        return logits

    def predict(self, signals: np.ndarray | torch.Tensor) -> np.ndarray:
        """Predict class labels.

        Args:
            signals: Signal array of shape (channels, seq_len) or (batch, channels, seq_len)

        Returns:
            Class indices of shape (seq_len,) or (batch, seq_len)
        """
        self.eval()

        if isinstance(signals, np.ndarray):
            signals = torch.tensor(signals, dtype=torch.float32)

        if signals.dim() == 2:
            signals = signals.unsqueeze(0)
            squeeze_output = True
        else:
            squeeze_output = False

        signals = signals.to(self.device)

        with torch.no_grad():
            logits = self.forward(signals)
            classes = logits.argmax(dim=1)

        classes = classes.cpu().numpy()

        if squeeze_output:
            classes = classes.squeeze(0)

        return classes

    def predict_proba(self, signals: np.ndarray | torch.Tensor) -> np.ndarray:
        """Predict class probabilities.

        Args:
            signals: Signal array

        Returns:
            Class probabilities of shape (num_classes, seq_len) or (batch, num_classes, seq_len)
        """
        self.eval()

        if isinstance(signals, np.ndarray):
            signals = torch.tensor(signals, dtype=torch.float32)

        if signals.dim() == 2:
            signals = signals.unsqueeze(0)
            squeeze_output = True
        else:
            squeeze_output = False

        signals = signals.to(self.device)

        with torch.no_grad():
            logits = self.forward(signals)
            probs = F.softmax(logits, dim=1)

        probs = probs.cpu().numpy()

        if squeeze_output:
            probs = probs.squeeze(0)

        return probs
