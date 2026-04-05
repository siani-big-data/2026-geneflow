"""Custom loss functions for ML training.

Includes specialized losses for handling class imbalance and improving
model calibration.
"""

import torch
import torch.nn as nn
import torch.nn.functional as F


class FocalLoss(nn.Module):
    """Focal Loss for addressing class imbalance.

    Focal loss down-weights well-classified examples and focuses on hard,
    misclassified examples. This is particularly useful when dealing with
    highly imbalanced datasets where rare classes are easily dominated by
    abundant ones.

    Reference:
        Lin et al., "Focal Loss for Dense Object Detection", ICCV 2017
        https://arxiv.org/abs/1708.02002

    The loss is computed as:
        FL(p_t) = -alpha_t * (1 - p_t)^gamma * log(p_t)

    Where:
        - p_t is the model's estimated probability for the correct class
        - gamma is the focusing parameter (higher = more focus on hard examples)
        - alpha_t is the optional class weight

    Args:
        gamma: Focusing parameter. gamma=0 is equivalent to cross-entropy.
               gamma=2 is commonly used and works well in practice.
        weight: Optional tensor of class weights for handling imbalance.
        reduction: Specifies the reduction to apply to output:
                   'none' | 'mean' | 'sum'. Default: 'mean'
        label_smoothing: Optional label smoothing factor. Default: 0.0

    Example:
        >>> criterion = FocalLoss(gamma=2.0)
        >>> logits = torch.randn(32, 10)  # batch_size=32, num_classes=10
        >>> targets = torch.randint(0, 10, (32,))
        >>> loss = criterion(logits, targets)
    """

    def __init__(
        self,
        gamma: float = 2.0,
        weight: torch.Tensor | None = None,
        reduction: str = "mean",
        label_smoothing: float = 0.0,
    ):
        super().__init__()
        self.gamma = gamma
        self.weight = weight
        self.reduction = reduction
        self.label_smoothing = label_smoothing

    def forward(self, inputs: torch.Tensor, targets: torch.Tensor) -> torch.Tensor:
        """Compute focal loss.

        Args:
            inputs: Predicted logits of shape (N, C) where N is batch size
                    and C is number of classes.
            targets: Ground truth class indices of shape (N,).

        Returns:
            Focal loss value (scalar if reduction='mean' or 'sum',
            tensor of shape (N,) if reduction='none').
        """
        # Compute cross-entropy loss without reduction
        ce_loss = F.cross_entropy(
            inputs,
            targets,
            weight=self.weight,
            reduction="none",
            label_smoothing=self.label_smoothing,
        )

        # Compute pt (probability of correct class)
        pt = torch.exp(-ce_loss)

        # Compute focal weight: (1 - pt)^gamma
        focal_weight = (1 - pt) ** self.gamma

        # Apply focal weight to cross-entropy loss
        focal_loss = focal_weight * ce_loss

        # Apply reduction
        if self.reduction == "mean":
            return focal_loss.mean()
        elif self.reduction == "sum":
            return focal_loss.sum()
        else:  # 'none'
            return focal_loss


class LabelSmoothingCrossEntropy(nn.Module):
    """Cross-entropy loss with label smoothing.

    Label smoothing helps prevent the model from becoming over-confident
    and improves generalization by encouraging the model to be less certain.

    Args:
        smoothing: Label smoothing factor. 0.0 = no smoothing (hard labels),
                   0.1 = 10% smoothing (typical value).
        weight: Optional class weights.
        reduction: Specifies the reduction: 'none' | 'mean' | 'sum'.
    """

    def __init__(
        self,
        smoothing: float = 0.1,
        weight: torch.Tensor | None = None,
        reduction: str = "mean",
    ):
        super().__init__()
        self.smoothing = smoothing
        self.weight = weight
        self.reduction = reduction

    def forward(self, inputs: torch.Tensor, targets: torch.Tensor) -> torch.Tensor:
        """Compute label-smoothed cross-entropy loss."""
        return F.cross_entropy(
            inputs,
            targets,
            weight=self.weight,
            reduction=self.reduction,
            label_smoothing=self.smoothing,
        )


class HierarchicalFocalLoss(nn.Module):
    """Focal loss designed for hierarchical multi-label classification.

    This loss computes focal loss for each level in a hierarchy and combines
    them with level-specific weights.

    Args:
        gamma: Focusing parameter for focal loss.
        level_weights: Dictionary mapping level names to their loss weights.
        class_weights: Dictionary mapping level names to class weight tensors.
    """

    def __init__(
        self,
        gamma: float = 2.0,
        level_weights: dict[str, float] | None = None,
        class_weights: dict[str, torch.Tensor] | None = None,
    ):
        super().__init__()
        self.gamma = gamma
        self.level_weights = level_weights or {}
        self.class_weights = class_weights or {}
        self._focal_losses: dict[str, FocalLoss] = {}

    def _get_focal_loss(self, level: str, device: torch.device) -> FocalLoss:
        """Get or create FocalLoss instance for a level."""
        if level not in self._focal_losses:
            weight = self.class_weights.get(level)
            if weight is not None:
                weight = weight.to(device)
            self._focal_losses[level] = FocalLoss(
                gamma=self.gamma,
                weight=weight,
                reduction="mean",
            )
        return self._focal_losses[level]

    def forward(
        self,
        outputs: dict[str, torch.Tensor],
        targets: dict[str, torch.Tensor],
    ) -> tuple[torch.Tensor, dict[str, float]]:
        """Compute hierarchical focal loss.

        Args:
            outputs: Dictionary mapping level names to logit tensors.
            targets: Dictionary mapping level names to target tensors.

        Returns:
            Tuple of (total_loss, losses_dict) where losses_dict contains
            per-level loss values.
        """
        total_loss = torch.tensor(0.0, device=next(iter(outputs.values())).device)
        losses = {}

        for level, logits in outputs.items():
            if level not in targets:
                continue

            target = targets[level]
            mask = target >= 0

            if not mask.any():
                continue

            masked_logits = logits[mask]
            masked_targets = target[mask]

            focal_loss = self._get_focal_loss(level, logits.device)
            loss = focal_loss(masked_logits, masked_targets)

            level_weight = self.level_weights.get(level, 1.0)
            weighted_loss = loss * level_weight

            losses[level] = loss.item()
            total_loss = total_loss + weighted_loss

        return total_loss, losses
