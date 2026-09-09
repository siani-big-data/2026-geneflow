"""Training report generation with metrics, plots, and model analysis."""

import json
from dataclasses import dataclass, field
from datetime import datetime
from pathlib import Path
from typing import TYPE_CHECKING

import numpy as np
import torch
import torch.nn as nn

if TYPE_CHECKING:
    pass


@dataclass
class ModelInfo:
    """Model architecture and resource information."""

    name: str
    total_parameters: int
    trainable_parameters: int
    non_trainable_parameters: int
    memory_mb: float
    memory_params_mb: float
    input_shape: tuple
    output_shape: tuple
    device: str
    dtype: str
    layers_summary: list = field(default_factory=list)

    def to_dict(self) -> dict:
        return {
            "name": self.name,
            "total_parameters": self.total_parameters,
            "trainable_parameters": self.trainable_parameters,
            "non_trainable_parameters": self.non_trainable_parameters,
            "memory_mb": round(self.memory_mb, 2),
            "memory_params_mb": round(self.memory_params_mb, 2),
            "input_shape": self.input_shape,
            "output_shape": self.output_shape,
            "device": self.device,
            "dtype": self.dtype,
            "layers_summary": self.layers_summary,
        }


@dataclass
class TrainingReport:
    """Complete training report."""

    model_info: ModelInfo
    training_config: dict
    epoch_metrics: list
    final_metrics: dict
    best_metrics: dict
    training_time_seconds: float
    timestamp: str = field(default_factory=lambda: datetime.now().isoformat())

    def to_dict(self) -> dict:
        return {
            "timestamp": self.timestamp,
            "model_info": self.model_info.to_dict(),
            "training_config": self.training_config,
            "epoch_metrics": self.epoch_metrics,
            "final_metrics": self.final_metrics,
            "best_metrics": self.best_metrics,
            "training_time_seconds": round(self.training_time_seconds, 2),
        }

    def save(self, path: Path | str) -> None:
        """Save report to JSON file."""
        path = Path(path)
        path.parent.mkdir(parents=True, exist_ok=True)
        with open(path, "w") as f:
            json.dump(self.to_dict(), f, indent=2)

    def print_summary(self) -> None:
        """Print summary to console."""
        print("\n" + "=" * 60)
        print("TRAINING REPORT")
        print("=" * 60)

        # Model info
        print("\n[MODEL INFO]")
        print(f"  Name: {self.model_info.name}")
        print(f"  Total parameters: {self.model_info.total_parameters:,}")
        print(f"  Trainable: {self.model_info.trainable_parameters:,}")
        print(f"  Memory (params): {self.model_info.memory_params_mb:.2f} MB")
        print(f"  Device: {self.model_info.device}")

        # Training config
        print("\n[TRAINING CONFIG]")
        for key, value in self.training_config.items():
            print(f"  {key}: {value}")

        # Best metrics
        print("\n[BEST METRICS]")
        for key, value in self.best_metrics.items():
            if isinstance(value, float):
                print(f"  {key}: {value:.4f}")
            else:
                print(f"  {key}: {value}")

        # Final metrics
        print("\n[FINAL METRICS]")
        for key, value in self.final_metrics.items():
            if isinstance(value, float):
                print(f"  {key}: {value:.4f}")
            else:
                print(f"  {key}: {value}")

        print(f"\n[TRAINING TIME] {self.training_time_seconds:.1f} seconds")
        print("=" * 60)


def analyze_model(model: nn.Module, input_shape: tuple = (1, 4, 1000)) -> ModelInfo:
    """Analyze model architecture and compute resource usage.

    Args:
        model: PyTorch model
        input_shape: Example input shape (batch, channels, length)

    Returns:
        ModelInfo with detailed analysis
    """
    # Count parameters
    total_params = sum(p.numel() for p in model.parameters())
    trainable_params = sum(p.numel() for p in model.parameters() if p.requires_grad)
    non_trainable_params = total_params - trainable_params

    # Memory for parameters (assuming float32)
    memory_params_mb = total_params * 4 / (1024 * 1024)

    # Estimate total memory (params + gradients + optimizer states)
    # Rough estimate: 4x params for Adam (params, grads, m, v)
    memory_mb = memory_params_mb * 4

    # Get device and dtype
    try:
        first_param = next(model.parameters())
        device = str(first_param.device)
        dtype = str(first_param.dtype)
    except StopIteration:
        device = "cpu"
        dtype = "float32"

    # Layer summary
    layers_summary = []
    for name, module in model.named_modules():
        if len(list(module.children())) == 0:  # Leaf modules only
            params = sum(p.numel() for p in module.parameters())
            if params > 0:
                layers_summary.append({
                    "name": name,
                    "type": module.__class__.__name__,
                    "parameters": params,
                })

    # Get output shape
    model.eval()
    try:
        with torch.no_grad():
            dummy_input = torch.randn(*input_shape)
            if hasattr(model, "device"):
                dummy_input = dummy_input.to(model.device)
            output = model(dummy_input)
            output_shape = tuple(output.shape)
    except Exception:
        output_shape = ("unknown",)

    return ModelInfo(
        name=model.__class__.__name__,
        total_parameters=total_params,
        trainable_parameters=trainable_params,
        non_trainable_parameters=non_trainable_params,
        memory_mb=memory_mb,
        memory_params_mb=memory_params_mb,
        input_shape=input_shape,
        output_shape=output_shape,
        device=device,
        dtype=dtype,
        layers_summary=layers_summary,
    )


def generate_training_plots(
    epoch_metrics: list,
    save_dir: Path | str,
    show: bool = False,
) -> list[Path]:
    """Generate training plots.

    Args:
        epoch_metrics: List of dicts with metrics per epoch
        save_dir: Directory to save plots
        show: Whether to display plots

    Returns:
        List of saved plot paths
    """
    try:
        import matplotlib.pyplot as plt
    except ImportError:
        print("Warning: matplotlib not installed, skipping plots")
        return []

    save_dir = Path(save_dir)
    save_dir.mkdir(parents=True, exist_ok=True)
    saved_plots = []

    if not epoch_metrics:
        return saved_plots

    epochs = [m["epoch"] for m in epoch_metrics]

    # Set style
    if "seaborn-v0_8-whitegrid" in plt.style.available:
        plt.style.use("seaborn-v0_8-whitegrid")

    # 1. Loss plot
    fig, ax = plt.subplots(figsize=(10, 6))
    if "train_loss" in epoch_metrics[0]:
        train_loss = [m["train_loss"] for m in epoch_metrics]
        ax.plot(epochs, train_loss, label="Train Loss", linewidth=2, color="#2196F3")
    if "val_loss" in epoch_metrics[0]:
        val_loss = [m.get("val_loss") for m in epoch_metrics]
        val_loss = [v for v in val_loss if v is not None]
        if val_loss:
            ax.plot(
                epochs[:len(val_loss)], val_loss,
                label="Val Loss", linewidth=2, color="#FF5722"
            )

    ax.set_xlabel("Epoch", fontsize=12)
    ax.set_ylabel("Loss", fontsize=12)
    ax.set_title("Training & Validation Loss", fontsize=14, fontweight="bold")
    ax.legend(fontsize=11)
    ax.grid(True, alpha=0.3)

    loss_path = save_dir / "loss_curve.png"
    fig.savefig(loss_path, dpi=150, bbox_inches="tight")
    saved_plots.append(loss_path)
    if show:
        plt.show()
    plt.close(fig)

    # 2. Accuracy plot (if available)
    if "train_accuracy" in epoch_metrics[0] or "val_accuracy" in epoch_metrics[0]:
        fig, ax = plt.subplots(figsize=(10, 6))

        if "train_accuracy" in epoch_metrics[0]:
            train_acc = [m["train_accuracy"] * 100 for m in epoch_metrics]
            ax.plot(epochs, train_acc, label="Train Accuracy", linewidth=2, color="#4CAF50")

        if "val_accuracy" in epoch_metrics[0]:
            val_acc = [m.get("val_accuracy", 0) * 100 for m in epoch_metrics]
            val_acc_filtered = [(e, v) for e, v in zip(epochs, val_acc) if v > 0]
            if val_acc_filtered:
                e, v = zip(*val_acc_filtered)
                ax.plot(e, v, label="Val Accuracy", linewidth=2, color="#9C27B0")

        ax.set_xlabel("Epoch", fontsize=12)
        ax.set_ylabel("Accuracy (%)", fontsize=12)
        ax.set_title("Training & Validation Accuracy", fontsize=14, fontweight="bold")
        ax.legend(fontsize=11)
        ax.set_ylim(0, 105)
        ax.grid(True, alpha=0.3)

        acc_path = save_dir / "accuracy_curve.png"
        fig.savefig(acc_path, dpi=150, bbox_inches="tight")
        saved_plots.append(acc_path)
        if show:
            plt.show()
        plt.close(fig)

    # 3. Learning rate plot (if available)
    if "lr" in epoch_metrics[0]:
        fig, ax = plt.subplots(figsize=(10, 4))
        lr = [m["lr"] for m in epoch_metrics]
        ax.plot(epochs, lr, linewidth=2, color="#607D8B")
        ax.set_xlabel("Epoch", fontsize=12)
        ax.set_ylabel("Learning Rate", fontsize=12)
        ax.set_title("Learning Rate Schedule", fontsize=14, fontweight="bold")
        ax.set_yscale("log")
        ax.grid(True, alpha=0.3)

        lr_path = save_dir / "learning_rate.png"
        fig.savefig(lr_path, dpi=150, bbox_inches="tight")
        saved_plots.append(lr_path)
        if show:
            plt.show()
        plt.close(fig)

    # 4. Combined metrics plot
    fig, axes = plt.subplots(2, 2, figsize=(14, 10))

    # Loss
    ax = axes[0, 0]
    if "train_loss" in epoch_metrics[0]:
        ax.plot(epochs, [m["train_loss"] for m in epoch_metrics], label="Train", linewidth=2)
    if "val_loss" in epoch_metrics[0]:
        val_loss = [m.get("val_loss") for m in epoch_metrics if m.get("val_loss") is not None]
        if val_loss:
            ax.plot(epochs[:len(val_loss)], val_loss, label="Val", linewidth=2)
    ax.set_title("Loss", fontweight="bold")
    ax.legend()
    ax.grid(True, alpha=0.3)

    # Accuracy
    ax = axes[0, 1]
    if "train_accuracy" in epoch_metrics[0]:
        train_acc = [m["train_accuracy"] * 100 for m in epoch_metrics]
        ax.plot(epochs, train_acc, label="Train", linewidth=2)
    if "val_accuracy" in epoch_metrics[0]:
        val_acc = [m.get("val_accuracy", 0) * 100 for m in epoch_metrics]
        val_acc_filtered = [(e, v) for e, v in zip(epochs, val_acc) if v > 0]
        if val_acc_filtered:
            e, v = zip(*val_acc_filtered)
            ax.plot(e, v, label="Val", linewidth=2)
    ax.set_title("Accuracy (%)", fontweight="bold")
    ax.legend()
    ax.set_ylim(0, 105)
    ax.grid(True, alpha=0.3)

    # MAE (if available)
    ax = axes[1, 0]
    if "train_mae" in epoch_metrics[0] or "val_mae" in epoch_metrics[0]:
        if "train_mae" in epoch_metrics[0]:
            train_mae = [m.get("train_mae", 0) for m in epoch_metrics]
            ax.plot(epochs, train_mae, label="Train", linewidth=2)
        if "val_mae" in epoch_metrics[0]:
            val_mae = [m.get("val_mae") for m in epoch_metrics if m.get("val_mae") is not None]
            if val_mae:
                ax.plot(epochs[:len(val_mae)], val_mae, label="Val", linewidth=2)
        ax.set_title("MAE", fontweight="bold")
        ax.legend()
    else:
        ax.text(0.5, 0.5, "No MAE data", ha="center", va="center", transform=ax.transAxes)
    ax.grid(True, alpha=0.3)

    # Learning rate
    ax = axes[1, 1]
    if "lr" in epoch_metrics[0]:
        ax.plot(epochs, [m["lr"] for m in epoch_metrics], linewidth=2, color="#607D8B")
        ax.set_yscale("log")
    ax.set_title("Learning Rate", fontweight="bold")
    ax.grid(True, alpha=0.3)

    plt.tight_layout()
    combined_path = save_dir / "training_summary.png"
    fig.savefig(combined_path, dpi=150, bbox_inches="tight")
    saved_plots.append(combined_path)
    if show:
        plt.show()
    plt.close(fig)

    return saved_plots


def compute_accuracy_regression(
    predictions: np.ndarray | torch.Tensor,
    targets: np.ndarray | torch.Tensor,
    tolerance: float = 5.0,
) -> float:
    """Compute accuracy for regression as percentage within tolerance.

    For quality_enhanced scores (0-60), we consider a prediction "correct" if it's
    within `tolerance` units of the target.

    Args:
        predictions: Predicted values
        targets: Target values
        tolerance: Maximum allowed error for "correct" prediction

    Returns:
        Accuracy as float between 0 and 1
    """
    if isinstance(predictions, torch.Tensor):
        predictions = predictions.detach().cpu().numpy()
    if isinstance(targets, torch.Tensor):
        targets = targets.detach().cpu().numpy()

    predictions = np.asarray(predictions).flatten()
    targets = np.asarray(targets).flatten()

    # Filter out invalid values
    mask = targets > 0
    if not mask.any():
        return 0.0

    predictions = predictions[mask]
    targets = targets[mask]

    within_tolerance = np.abs(predictions - targets) <= tolerance
    return float(within_tolerance.mean())


def compute_regression_metrics_detailed(
    predictions: np.ndarray | torch.Tensor,
    targets: np.ndarray | torch.Tensor,
) -> dict:
    """Compute detailed regression metrics.

    Args:
        predictions: Predicted values
        targets: Target values

    Returns:
        Dictionary with multiple metrics
    """
    if isinstance(predictions, torch.Tensor):
        predictions = predictions.detach().cpu().numpy()
    if isinstance(targets, torch.Tensor):
        targets = targets.detach().cpu().numpy()

    predictions = np.asarray(predictions).flatten()
    targets = np.asarray(targets).flatten()

    # Filter valid values
    mask = targets > 0
    if not mask.any():
        return {}

    preds = predictions[mask]
    targs = targets[mask]

    diff = preds - targs
    abs_diff = np.abs(diff)

    metrics = {
        "mae": float(abs_diff.mean()),
        "mse": float((diff ** 2).mean()),
        "rmse": float(np.sqrt((diff ** 2).mean())),
        "max_error": float(abs_diff.max()),
        "median_error": float(np.median(abs_diff)),
        "accuracy_1": float((abs_diff <= 1).mean()),  # Within 1 unit
        "accuracy_3": float((abs_diff <= 3).mean()),  # Within 3 units
        "accuracy_5": float((abs_diff <= 5).mean()),  # Within 5 units
        "accuracy_10": float((abs_diff <= 10).mean()),  # Within 10 units
    }

    # R² score
    ss_res = (diff ** 2).sum()
    ss_tot = ((targs - targs.mean()) ** 2).sum()
    if ss_tot > 0:
        metrics["r2"] = float(1 - ss_res / ss_tot)
    else:
        metrics["r2"] = 0.0

    # Pearson correlation
    if len(preds) > 1:
        corr = np.corrcoef(preds, targs)[0, 1]
        metrics["pearson"] = float(corr) if not np.isnan(corr) else 0.0
    else:
        metrics["pearson"] = 0.0

    return metrics
