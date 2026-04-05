"""Generic training loop for PyTorch models."""

import time
from dataclasses import dataclass
from pathlib import Path
from typing import Callable

import numpy as np
import torch
import torch.nn as nn
from torch.optim import AdamW
from torch.optim.lr_scheduler import CosineAnnealingLR, ReduceLROnPlateau
from torch.utils.data import DataLoader

from ..models.base import BaseModel
from .callbacks import TrainingCallback
from .metrics import MetricsTracker
from .report import (
    TrainingReport,
    analyze_model,
    compute_accuracy_regression,
    compute_regression_metrics_detailed,
    generate_training_plots,
)


@dataclass
class TrainingConfig:
    """Configuration for training."""

    epochs: int = 100
    batch_size: int = 32
    learning_rate: float = 1e-4
    weight_decay: float = 0.01
    warmup_steps: int = 100
    gradient_clip: float = 1.0
    scheduler: str = "cosine"  # "cosine", "plateau", "none"
    log_every: int = 10
    eval_every: int = 1
    save_dir: str = "checkpoints"
    mixed_precision: bool = True
    num_workers: int = 4
    pin_memory: bool = True
    # Metrics config
    task_type: str = "regression"  # "regression" or "classification"
    accuracy_tolerance: float = 5.0  # For regression: within X units = correct
    generate_plots: bool = True
    generate_report: bool = True
    # Data key mapping (for flexible batch format)
    input_key: str = "signals"  # Key for input data in batch
    target_key: str = "quality_enhanced"  # Key for target data in batch


class Trainer:
    """Generic trainer for PyTorch models."""

    def __init__(
        self,
        model: BaseModel,
        config: TrainingConfig,
        loss_fn: nn.Module | Callable,
        callbacks: list[TrainingCallback] | None = None,
    ):
        self.model = model.to_device()
        self.config = config
        self.loss_fn = loss_fn
        self.callbacks = callbacks or []
        self.metrics = MetricsTracker()

        # Optimizer
        self.optimizer = AdamW(
            model.parameters(),
            lr=config.learning_rate,
            weight_decay=config.weight_decay,
        )

        # Scheduler
        self.scheduler = self._create_scheduler()

        # Mixed precision
        self.scaler = torch.amp.GradScaler() if config.mixed_precision else None

        # State
        self.current_epoch = 0
        self.global_step = 0
        self.best_val_loss = float("inf")
        self.epoch_history = []  # Track all metrics per epoch
        self.training_start_time = None
        self.training_end_time = None

    def _create_scheduler(self):
        """Create learning rate scheduler."""
        if self.config.scheduler == "cosine":
            return CosineAnnealingLR(
                self.optimizer,
                T_max=self.config.epochs,
                eta_min=self.config.learning_rate / 100,
            )
        elif self.config.scheduler == "plateau":
            return ReduceLROnPlateau(
                self.optimizer,
                mode="min",
                factor=0.5,
                patience=5,
            )
        return None

    def train(
        self,
        train_loader: DataLoader,
        val_loader: DataLoader | None = None,
    ) -> dict:
        """Run training loop."""
        self.training_start_time = time.time()

        # Callbacks: on_train_start
        for cb in self.callbacks:
            cb.on_train_start(self)

        try:
            for epoch in range(self.config.epochs):
                self.current_epoch = epoch

                # Callbacks: on_epoch_start
                for cb in self.callbacks:
                    cb.on_epoch_start(self, epoch)

                # Train epoch
                train_metrics = self._train_epoch(train_loader)
                self.metrics.log_epoch("train", epoch, train_metrics)

                # Validation
                val_metrics = {}
                if val_loader and (epoch + 1) % self.config.eval_every == 0:
                    val_metrics = self._validate(val_loader)
                    self.metrics.log_epoch("val", epoch, val_metrics)

                    # Update best
                    val_loss = val_metrics.get("loss", float("inf"))
                    if val_loss < self.best_val_loss:
                        self.best_val_loss = val_loss

                # Store epoch history for report
                epoch_record = {
                    "epoch": epoch + 1,
                    "train_loss": train_metrics.get("loss"),
                    "train_accuracy": train_metrics.get("accuracy"),
                    "train_mae": train_metrics.get("mae"),
                    "val_loss": val_metrics.get("loss"),
                    "val_accuracy": val_metrics.get("accuracy"),
                    "val_mae": val_metrics.get("mae"),
                    "lr": train_metrics.get("lr"),
                }
                self.epoch_history.append(epoch_record)

                # Scheduler step
                if self.scheduler:
                    if isinstance(self.scheduler, ReduceLROnPlateau):
                        self.scheduler.step(val_metrics.get("loss", train_metrics["loss"]))
                    else:
                        self.scheduler.step()

                # Callbacks: on_epoch_end
                stop_training = False
                for cb in self.callbacks:
                    if cb.on_epoch_end(self, epoch, {**train_metrics, **val_metrics}):
                        stop_training = True

                if stop_training:
                    print(f"Early stopping at epoch {epoch + 1}")
                    break

        finally:
            self.training_end_time = time.time()
            # Callbacks: on_train_end
            for cb in self.callbacks:
                cb.on_train_end(self)

        self.model._is_trained = True

        # Generate report if configured
        if self.config.generate_report:
            self._generate_final_report()

        return self.metrics.get_summary()

    def _train_epoch(self, loader: DataLoader) -> dict:
        """Train for one epoch."""
        self.model.train()
        total_loss = 0.0
        num_batches = 0
        all_preds = []
        all_targets = []
        start_time = time.time()

        input_key = self.config.input_key
        target_key = self.config.target_key

        for batch_idx, batch in enumerate(loader):
            # Move to device
            batch = self._to_device(batch)

            # Forward pass
            self.optimizer.zero_grad()

            if self.scaler:
                with torch.amp.autocast(device_type="cuda"):
                    loss = self._compute_loss(batch)
                    # Get predictions for accuracy
                    with torch.no_grad():
                        preds = self.model(batch[input_key])
                self.scaler.scale(loss).backward()
                self.scaler.unscale_(self.optimizer)
                torch.nn.utils.clip_grad_norm_(self.model.parameters(), self.config.gradient_clip)
                self.scaler.step(self.optimizer)
                self.scaler.update()
            else:
                loss = self._compute_loss(batch)
                with torch.no_grad():
                    preds = self.model(batch[input_key])
                loss.backward()
                torch.nn.utils.clip_grad_norm_(self.model.parameters(), self.config.gradient_clip)
                self.optimizer.step()

            total_loss += loss.item()
            num_batches += 1
            self.global_step += 1

            # Collect for accuracy calculation
            all_preds.append(preds.detach().cpu())
            all_targets.append(batch[target_key].detach().cpu())

            # Callbacks: on_batch_end
            for cb in self.callbacks:
                cb.on_batch_end(self, batch_idx, {"loss": loss.item()})

            # Log
            if (batch_idx + 1) % self.config.log_every == 0:
                avg_loss = total_loss / num_batches
                lr = self.optimizer.param_groups[0]["lr"]
                print(
                    f"  Batch {batch_idx + 1}/{len(loader)} | Loss: {avg_loss:.4f} | LR: {lr:.2e}"
                )

        elapsed = time.time() - start_time

        # Compute metrics based on task type
        preds_tensor = torch.cat(all_preds, dim=0)
        targets_tensor = torch.cat(all_targets, dim=0)

        if self.config.task_type == "classification":
            # Classification: accuracy = correct predictions / total
            preds_classes = preds_tensor.argmax(dim=-1) if preds_tensor.dim() > 1 else preds_tensor
            accuracy = (preds_classes == targets_tensor).float().mean().item()
            mae = 0.0  # Not applicable for classification
        else:
            # Regression: accuracy = within tolerance
            preds = preds_tensor.numpy()
            targets = targets_tensor.numpy()
            accuracy = compute_accuracy_regression(preds, targets, self.config.accuracy_tolerance)
            # Compute MAE
            mask = targets > 0
            mae = float(np.abs(preds[mask] - targets[mask]).mean()) if mask.any() else 0.0

        return {
            "loss": total_loss / num_batches,
            "accuracy": accuracy,
            "mae": mae,
            "time": elapsed,
            "lr": self.optimizer.param_groups[0]["lr"],
        }

    @torch.no_grad()
    def _validate(self, loader: DataLoader) -> dict:
        """Run validation."""
        self.model.eval()
        total_loss = 0.0
        num_batches = 0
        all_preds = []
        all_targets = []

        input_key = self.config.input_key
        target_key = self.config.target_key

        for batch in loader:
            batch = self._to_device(batch)
            loss = self._compute_loss(batch)
            total_loss += loss.item()
            num_batches += 1

            # Collect predictions for metrics
            with torch.amp.autocast(device_type="cuda", enabled=bool(self.scaler)):
                preds = self.model(batch[input_key])
            all_preds.append(preds.cpu())
            all_targets.append(batch[target_key].cpu())

        # Compute metrics
        preds_tensor = torch.cat(all_preds, dim=0)
        targets_tensor = torch.cat(all_targets, dim=0)

        metrics = {
            "loss": total_loss / num_batches,
        }

        if self.config.task_type == "classification":
            # Classification: accuracy = correct predictions / total
            preds_classes = preds_tensor.argmax(dim=-1) if preds_tensor.dim() > 1 else preds_tensor
            accuracy = (preds_classes == targets_tensor).float().mean().item()
            metrics["accuracy"] = accuracy
        else:
            # Regression: accuracy = within tolerance
            preds = preds_tensor.numpy()
            targets = targets_tensor.numpy()
            accuracy = compute_accuracy_regression(preds, targets, self.config.accuracy_tolerance)
            metrics["accuracy"] = accuracy
            # MAE for regression
            mask = targets > 0
            if mask.any():
                mae = float(np.abs(preds[mask] - targets[mask]).mean())
                metrics["mae"] = mae

        return metrics

    def _compute_loss(self, batch: dict) -> torch.Tensor:
        """Compute loss for a batch."""
        input_key = self.config.input_key
        target_key = self.config.target_key

        outputs = self.model(batch[input_key])
        targets = batch[target_key]
        mask = batch.get("mask", None)

        if mask is not None:
            outputs = outputs[mask]
            targets = targets[mask]

        return self.loss_fn(outputs, targets)

    def _to_device(self, batch: dict) -> dict:
        """Move batch to device."""
        return {
            k: v.to(self.model.device) if isinstance(v, torch.Tensor) else v
            for k, v in batch.items()
        }

    def save_checkpoint(self, path: str | Path) -> None:
        """Save training checkpoint."""
        path = Path(path)
        path.parent.mkdir(parents=True, exist_ok=True)

        checkpoint = {
            "epoch": self.current_epoch,
            "global_step": self.global_step,
            "model_state": self.model.state_dict(),
            "optimizer_state": self.optimizer.state_dict(),
            "scheduler_state": self.scheduler.state_dict() if self.scheduler else None,
            "best_val_loss": self.best_val_loss,
            "config": self.config.__dict__,
        }
        torch.save(checkpoint, path)

    def load_checkpoint(self, path: str | Path) -> None:
        """Load training checkpoint."""
        checkpoint = torch.load(path, map_location=self.model.device, weights_only=False)

        self.current_epoch = checkpoint["epoch"]
        self.global_step = checkpoint["global_step"]
        self.best_val_loss = checkpoint["best_val_loss"]
        self.model.load_state_dict(checkpoint["model_state"])
        self.optimizer.load_state_dict(checkpoint["optimizer_state"])
        if self.scheduler and checkpoint["scheduler_state"]:
            self.scheduler.load_state_dict(checkpoint["scheduler_state"])

    def _generate_final_report(self) -> None:
        """Generate final training report with plots and metrics."""
        save_dir = Path(self.config.save_dir)
        save_dir.mkdir(parents=True, exist_ok=True)

        print("\n" + "=" * 60)
        print("GENERATING TRAINING REPORT")
        print("=" * 60)

        # Analyze model
        model_info = analyze_model(self.model)

        # Compute training time
        training_time = 0.0
        if self.training_start_time and self.training_end_time:
            training_time = self.training_end_time - self.training_start_time

        # Get best metrics
        best_metrics = {}
        if self.epoch_history:
            val_losses = [e["val_loss"] for e in self.epoch_history if e.get("val_loss") is not None]
            val_accs = [e["val_accuracy"] for e in self.epoch_history if e.get("val_accuracy") is not None]
            train_losses = [e["train_loss"] for e in self.epoch_history if e.get("train_loss") is not None]

            if val_losses:
                best_idx = int(np.argmin(val_losses))
                best_metrics["best_val_loss"] = val_losses[best_idx]
                best_metrics["best_val_loss_epoch"] = best_idx + 1
            if val_accs:
                best_idx = int(np.argmax(val_accs))
                best_metrics["best_val_accuracy"] = val_accs[best_idx]
                best_metrics["best_val_accuracy_epoch"] = best_idx + 1
            if train_losses:
                best_metrics["best_train_loss"] = min(train_losses)

        # Get final metrics
        final_metrics = {}
        if self.epoch_history:
            last = self.epoch_history[-1]
            final_metrics = {k: v for k, v in last.items() if v is not None and k != "epoch"}

        # Create report
        report = TrainingReport(
            model_info=model_info,
            training_config={
                "epochs": self.config.epochs,
                "batch_size": self.config.batch_size,
                "learning_rate": self.config.learning_rate,
                "weight_decay": self.config.weight_decay,
                "scheduler": self.config.scheduler,
                "accuracy_tolerance": self.config.accuracy_tolerance,
            },
            epoch_metrics=self.epoch_history,
            final_metrics=final_metrics,
            best_metrics=best_metrics,
            training_time_seconds=training_time,
        )

        # Save JSON report
        report.save(save_dir / "training_report.json")
        print(f"  Saved: {save_dir / 'training_report.json'}")

        # Generate plots
        if self.config.generate_plots:
            try:
                plots = generate_training_plots(self.epoch_history, save_dir)
                for p in plots:
                    print(f"  Saved: {p}")
            except Exception as e:
                print(f"  Warning: Could not generate plots: {e}")

        # Print summary
        report.print_summary()
