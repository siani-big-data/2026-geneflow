"""Taxonomy Classifier - Hierarchical DNA sequence classification model.

Combines hierarchical multi-level classification with sequence feature integration.
Uses a shared CNN encoder with feature fusion and multiple classification heads,
one for each taxonomic level (kingdom, phylum, class, order, family, genus).
"""

from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

import torch
import torch.nn as nn
import torch.nn.functional as F

from ..base import BaseModel, ModelConfig


# Taxonomic levels in hierarchical order
TAXONOMY_LEVELS = ["kingdom", "phylum", "class", "order", "family", "genus"]

# Nucleotide to index mapping
NUCLEOTIDE_MAP = {
    "A": 0, "T": 1, "C": 2, "G": 3, "N": 4,
    "a": 0, "t": 1, "c": 2, "g": 3, "n": 4,
}


def encode_sequence(sequence: str, max_length: int = 2000) -> torch.Tensor:
    """Encode DNA sequence to tensor of indices.

    Args:
        sequence: DNA sequence string (A, T, C, G, N)
        max_length: Maximum sequence length (truncate or pad)

    Returns:
        Tensor of shape (max_length,) with nucleotide indices
    """
    if len(sequence) > max_length:
        start = (len(sequence) - max_length) // 2
        sequence = sequence[start : start + max_length]

    indices = [NUCLEOTIDE_MAP.get(nuc, 4) for nuc in sequence]

    while len(indices) < max_length:
        indices.append(4)

    return torch.tensor(indices, dtype=torch.long)


@dataclass
class TaxonomyConfig(ModelConfig):
    """Configuration for TaxonomyClassifier."""

    name: str = "TaxonomyClassifier"
    version: str = "1.0.0"

    # Sequence model
    vocab_size: int = 5  # A, T, C, G, N
    embedding_dim: int = 128
    hidden_channels: int = 256
    num_conv_layers: int = 6
    kernel_size: int = 7
    max_seq_length: int = 2000
    dropout: float = 0.3

    # Feature model
    num_features: int = 101  # 21 basic + 16 dinuc + 64 trinuc
    featuREDACTED: int = 256
    num_featuREDACTED: int = 3

    # Fusion
    fusion_dim: int = 512

    # Classification heads
    head_hidden_dim: int = 256
    num_classes_per_level: dict[str, int] = field(default_factory=dict)
    class_labels_per_level: dict[str, list[str]] = field(default_factory=dict)

    # Hierarchical loss
    hierarchy_loss_weight: float = 0.1
    level_weights: dict[str, float] = field(default_factory=dict)

    # Training
    learning_rate: float = 1e-3
    weight_decay: float = 1e-4
    batch_size: int = 32
    max_epochs: int = 100
    warmup_epochs: int = 5


class ConvBlock(nn.Module):
    """Convolutional block with residual connection."""

    def __init__(
        self,
        in_channels: int,
        out_channels: int,
        kernel_size: int,
        dropout: float = 0.1,
    ):
        super().__init__()
        padding = kernel_size // 2

        self.conv1 = nn.Conv1d(in_channels, out_channels, kernel_size, padding=padding)
        self.conv2 = nn.Conv1d(out_channels, out_channels, kernel_size, padding=padding)
        self.bn1 = nn.BatchNorm1d(out_channels)
        self.bn2 = nn.BatchNorm1d(out_channels)
        self.dropout = nn.Dropout(dropout)

        self.residual = (
            nn.Conv1d(in_channels, out_channels, 1)
            if in_channels != out_channels
            else nn.Identity()
        )

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        residual = self.residual(x)

        x = self.conv1(x)
        x = self.bn1(x)
        x = F.gelu(x)
        x = self.dropout(x)

        x = self.conv2(x)
        x = self.bn2(x)
        x = x + residual
        x = F.gelu(x)

        return x


class SequenceEncoder(nn.Module):
    """Enhanced sequence encoder with attention."""

    def __init__(self, config: TaxonomyConfig):
        super().__init__()

        self.embedding = nn.Embedding(
            config.vocab_size,
            config.embedding_dim,
            padding_idx=4,
        )

        layers = []
        in_channels = config.embedding_dim

        for i in range(config.num_conv_layers):
            out_channels = config.hidden_channels * (2 ** min(i, 2))
            layers.append(
                ConvBlock(in_channels, out_channels, config.kernel_size, config.dropout)
            )
            in_channels = out_channels
            if (i + 1) % 2 == 0:
                layers.append(nn.MaxPool1d(2))

        self.conv_layers = nn.Sequential(*layers)
        self.output_dim = in_channels

        self.attention = nn.MultiheadAttention(
            embed_dim=in_channels,
            num_heads=8,
            dropout=config.dropout,
            batch_first=True,
        )
        self.attention_norm = nn.LayerNorm(in_channels)

        self.pool = nn.AdaptiveAvgPool1d(1)

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        x = self.embedding(x)
        x = x.transpose(1, 2)

        x = self.conv_layers(x)

        x_att = x.transpose(1, 2)
        att_out, _ = self.attention(x_att, x_att, x_att)
        x_att = self.attention_norm(x_att + att_out)
        x = x_att.transpose(1, 2)

        x = self.pool(x).squeeze(-1)

        return x


class FeatureEncoder(nn.Module):
    """Feature encoder with residual connections."""

    def __init__(self, config: TaxonomyConfig):
        super().__init__()

        self.input_norm = nn.BatchNorm1d(config.num_features)

        layers = []
        in_dim = config.num_features

        for i in range(config.num_featuREDACTED):
            out_dim = config.featuREDACTED
            layers.append(nn.Linear(in_dim, out_dim))
            layers.append(nn.BatchNorm1d(out_dim))
            layers.append(nn.GELU())
            layers.append(nn.Dropout(config.dropout))
            in_dim = out_dim

        self.layers = nn.Sequential(*layers)
        self.output_dim = config.featuREDACTED

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        x = self.input_norm(x)
        return self.layers(x)


class HierarchicalClassificationHead(nn.Module):
    """Classification head for a single taxonomic level with context from parent."""

    def __init__(
        self,
        in_features: int,
        hidden_dim: int,
        num_classes: int,
        parent_classes: int = 0,
        dropout: float = 0.2,
    ):
        super().__init__()

        total_in = in_features + parent_classes

        self.head = nn.Sequential(
            nn.Linear(total_in, hidden_dim),
            nn.BatchNorm1d(hidden_dim),
            nn.GELU(),
            nn.Dropout(dropout),
            nn.Linear(hidden_dim, hidden_dim // 2),
            nn.GELU(),
            nn.Dropout(dropout),
            nn.Linear(hidden_dim // 2, num_classes),
        )

        self.parent_classes = parent_classes

    def forward(
        self,
        x: torch.Tensor,
        parent_probs: torch.Tensor | None = None,
    ) -> torch.Tensor:
        if parent_probs is not None and self.parent_classes > 0:
            x = torch.cat([x, parent_probs], dim=-1)
        return self.head(x)


class TaxonomyClassifier(BaseModel):
    """Hierarchical multi-level taxonomy classifier with feature integration.

    Architecture:
    1. Sequence encoder (CNN + attention)
    2. Feature encoder (MLP)
    3. Fusion layer
    4. Hierarchical classification heads with parent conditioning

    The model predicts at multiple taxonomic levels, using information
    from broader levels to improve finer-grained predictions.
    """

    def __init__(self, config: TaxonomyConfig):
        super().__init__(config)
        self.config: TaxonomyConfig = config

        if not config.num_classes_per_level:
            raise ValueError("num_classes_per_level must be provided")

        # Encoders
        self.sequence_encoder = SequenceEncoder(config)
        self.featuREDACTED = FeatureEncoder(config)

        # Fusion
        fusion_input = self.sequence_encoder.output_dim + self.featuREDACTED.output_dim
        self.fusion = nn.Sequential(
            nn.Linear(fusion_input, config.fusion_dim),
            nn.BatchNorm1d(config.fusion_dim),
            nn.GELU(),
            nn.Dropout(config.dropout),
            nn.Linear(config.fusion_dim, config.fusion_dim),
            nn.BatchNorm1d(config.fusion_dim),
            nn.GELU(),
        )

        # Determine active levels
        self.active_levels = [
            level for level in TAXONOMY_LEVELS
            if level in config.num_classes_per_level
        ]

        # Classification heads (with parent conditioning)
        self.heads = nn.ModuleDict()
        prev_num_classes = 0

        for level in self.active_levels:
            num_classes = config.num_classes_per_level[level]
            self.heads[level] = HierarchicalClassificationHead(
                in_features=config.fusion_dim,
                hidden_dim=config.head_hidden_dim,
                num_classes=num_classes,
                parent_classes=prev_num_classes,
                dropout=config.dropout,
            )
            prev_num_classes = num_classes

        self.to_device()

    def encode(self, sequence: torch.Tensor, features: torch.Tensor | None = None) -> torch.Tensor:
        """Encode inputs to fused representation."""
        seq_repr = self.sequence_encoder(sequence)

        if features is not None:
            feat_repr = self.featuREDACTED(features)
        else:
            feat_repr = torch.zeros(
                sequence.size(0),
                self.featuREDACTED.output_dim,
                device=sequence.device,
            )

        combined = torch.cat([seq_repr, feat_repr], dim=-1)
        return self.fusion(combined)

    def forward(
        self,
        sequence: torch.Tensor,
        features: torch.Tensor | None = None,
    ) -> dict[str, torch.Tensor]:
        """Forward pass.

        Args:
            sequence: (batch, seq_length) nucleotide indices
            features: (batch, num_features) optional feature vector

        Returns:
            Dictionary mapping level names to logits tensors
        """
        representation = self.encode(sequence, features)

        outputs = {}
        parent_probs = None

        for level in self.active_levels:
            logits = self.heads[level](representation, parent_probs)
            outputs[level] = logits

            parent_probs = F.softmax(logits, dim=-1).detach()

        return outputs

    def compute_loss(
        self,
        outputs: dict[str, torch.Tensor],
        targets: dict[str, torch.Tensor],
        class_weights: dict[str, torch.Tensor] | None = None,
    ) -> tuple[torch.Tensor, dict[str, float]]:
        """Compute hierarchical loss with level weighting."""
        losses = {}
        total_loss = torch.tensor(0.0, device=self.device)

        default_weights = {
            "kingdom": 0.3,
            "phylum": 0.5,
            "class": 0.6,
            "order": 0.7,
            "family": 0.8,
            "genus": 1.5,
        }
        level_weights = self.config.level_weights or default_weights

        for level in self.active_levels:
            if level not in targets:
                continue

            target = targets[level]
            mask = target >= 0

            if not mask.any():
                continue

            logits = outputs[level][mask]
            labels = target[mask]

            if class_weights and level in class_weights:
                weight = class_weights[level].to(self.device)
                loss = F.cross_entropy(logits, labels, weight=weight)
            else:
                loss = F.cross_entropy(logits, labels)

            level_weight = level_weights.get(level, 1.0)
            weighted_loss = loss * level_weight

            losses[level] = loss.item()
            total_loss = total_loss + weighted_loss

        if self.config.hierarchy_loss_weight > 0:
            consistency = self._compute_consistency_loss(outputs)
            if consistency is not None:
                losses["consistency"] = consistency.item()
                total_loss = total_loss + self.config.hierarchy_loss_weight * consistency

        return total_loss, losses

    def _compute_consistency_loss(self, outputs: dict[str, torch.Tensor]) -> torch.Tensor | None:
        """Penalize inconsistent predictions across hierarchy."""
        if len(self.active_levels) < 2:
            return None

        consistency_loss = torch.tensor(0.0, device=self.device)
        n_pairs = 0

        for i in range(len(self.active_levels) - 1):
            parent = self.active_levels[i]
            child = self.active_levels[i + 1]

            parent_probs = F.softmax(outputs[parent], dim=-1)
            child_probs = F.softmax(outputs[child], dim=-1)

            parent_entropy = -(parent_probs * (parent_probs + 1e-10).log()).sum(-1)
            child_entropy = -(child_probs * (child_probs + 1e-10).log()).sum(-1)

            diff = (parent_entropy - child_entropy).abs()
            consistency_loss = consistency_loss + diff.mean()
            n_pairs += 1

        return consistency_loss / n_pairs if n_pairs > 0 else None

    def predict(
        self,
        sequences: list[str] | str,
        features: torch.Tensor | None = None,
    ) -> dict[str, Any]:
        """Predict taxonomy at all levels."""
        if isinstance(sequences, str):
            sequences = [sequences]

        self.eval()

        encoded = torch.stack(
            [encode_sequence(s, self.config.max_seq_length) for s in sequences]
        ).to(self.device)

        if features is not None:
            features = features.to(self.device)

        with torch.no_grad():
            outputs = self.forward(encoded, features)

        predictions = {}
        for level in self.active_levels:
            logits = outputs[level]
            probs = F.softmax(logits, dim=-1)
            confidence, pred_idx = probs.max(dim=-1)

            labels = self.config.class_labels_per_level.get(level, [])
            class_labels = [labels[i] for i in pred_idx.tolist()] if labels else None

            predictions[level] = {
                "indices": pred_idx.cpu().tolist(),
                "labels": class_labels,
                "confidence": confidence.cpu().tolist(),
                "probabilities": probs.cpu().tolist(),
            }

        return {"predictions": predictions, "count": len(sequences)}

    def predict_hierarchy(self, sequences: list[str] | str, features: torch.Tensor | None = None) -> list[dict]:
        """Get full taxonomy prediction for each sequence."""
        result = self.predict(sequences, features)
        preds = result["predictions"]
        n = result["count"]

        hierarchies = []
        for i in range(n):
            h = {}
            for level in self.active_levels:
                if preds[level]["labels"]:
                    h[level] = preds[level]["labels"][i]
                else:
                    h[level] = f"class_{preds[level]['indices'][i]}"
                h[f"{level}_confidence"] = preds[level]["confidence"][i]
            hierarchies.append(h)

        return hierarchies

    def save(self, path: str | Path) -> None:
        """Save checkpoint."""
        path = Path(path)
        path.parent.mkdir(parents=True, exist_ok=True)

        torch.save({
            "config": {
                "name": self.config.name,
                "version": self.config.version,
                "vocab_size": self.config.vocab_size,
                "embedding_dim": self.config.embedding_dim,
                "hidden_channels": self.config.hidden_channels,
                "num_conv_layers": self.config.num_conv_layers,
                "kernel_size": self.config.kernel_size,
                "max_seq_length": self.config.max_seq_length,
                "dropout": self.config.dropout,
                "num_features": self.config.num_features,
                "featuREDACTED": self.config.featuREDACTED,
                "num_featuREDACTED": self.config.num_featuREDACTED,
                "fusion_dim": self.config.fusion_dim,
                "head_hidden_dim": self.config.head_hidden_dim,
                "num_classes_per_level": self.config.num_classes_per_level,
                "class_labels_per_level": self.config.class_labels_per_level,
                "hierarchy_loss_weight": self.config.hierarchy_loss_weight,
                "level_weights": self.config.level_weights,
            },
            "state_dict": self.state_dict(),
            "active_levels": self.active_levels,
            "is_trained": self._is_trained,
        }, path)

    @classmethod
    def load_from_checkpoint(cls, path: str | Path) -> "TaxonomyClassifier":
        """Load from checkpoint."""
        ckpt = torch.load(path, map_location="cpu", weights_only=False)
        config = TaxonomyConfig(**ckpt["config"])
        model = cls(config)
        model.load_state_dict(ckpt["state_dict"])
        model._is_trained = ckpt.get("is_trained", True)
        return model
