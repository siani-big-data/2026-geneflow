"""Generic training loop for PyTorch models."""

import time
from dataclasses import dataclass
from pathlib import Path
from typing import Callable

import torch
import torch.nn as nn
from torch.optim import AdamW
from torch.optim.lr_scheduler import CosineAnnealingLR, ReduceLROnPlateau
from torch.utils.data import DataLoader

from ..models.base import BaseModel
from .callbacks import TrainingCallback
from .metrics import MetricsTracker


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
                    print(f"Early stopping at epoch {epoch}")
                    break

        finally:
            # Callbacks: on_train_end
            for cb in self.callbacks:
                cb.on_train_end(self)

        self.model._is_trained = True
        return self.metrics.get_summary()

    def _train_epoch(self, loader: DataLoader) -> dict:
        """Train for one epoch."""
        self.model.train()
        total_loss = 0.0
        num_batches = 0
        start_time = time.time()

        for batch_idx, batch in enumerate(loader):
            # Move to device
            batch = self._to_device(batch)

            # Forward pass
            self.optimizer.zero_grad()

            if self.scaler:
                with torch.amp.autocast(device_type="cuda"):
                    loss = self._compute_loss(batch)
                self.scaler.scale(loss).backward()
                self.scaler.unscale_(self.optimizer)
                torch.nn.utils.clip_grad_norm_(self.model.parameters(), self.config.gradient_clip)
                self.scaler.step(self.optimizer)
                self.scaler.update()
            else:
                loss = self._compute_loss(batch)
                loss.backward()
                torch.nn.utils.clip_grad_norm_(self.model.parameters(), self.config.gradient_clip)
                self.optimizer.step()

            total_loss += loss.item()
            num_batches += 1
            self.global_step += 1

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
        return {
            "loss": total_loss / num_batches,
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

        for batch in loader:
            batch = self._to_device(batch)
            loss = self._compute_loss(batch)
            total_loss += loss.item()
            num_batches += 1

            # Collect predictions for metrics
            with torch.amp.autocast(device_type="cuda", enabled=bool(self.scaler)):
                preds = self.model(batch["signals"])
            all_preds.append(preds.cpu())
            all_targets.append(batch["quality"].cpu())

        # Compute additional metrics
        preds = torch.cat(all_preds, dim=0)
        targets = torch.cat(all_targets, dim=0)

        metrics = {
            "loss": total_loss / num_batches,
        }

        # MAE for regression
        if preds.shape == targets.shape:
            mask = targets > 0
            if mask.any():
                mae = (preds[mask] - targets[mask]).abs().mean().item()
                metrics["mae"] = mae

        return metrics

    def _compute_loss(self, batch: dict) -> torch.Tensor:
        """Compute loss for a batch."""
        outputs = self.model(batch["signals"])
        targets = batch["quality"]
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
