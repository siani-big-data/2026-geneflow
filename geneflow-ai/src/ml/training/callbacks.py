"""Training callbacks."""

from abc import ABC
from pathlib import Path
from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from .trainer import Trainer


class TrainingCallback(ABC):
    """Base class for training callbacks."""

    def on_train_start(self, trainer: "Trainer") -> None:
        """Called at the start of training."""
        pass

    def on_train_end(self, trainer: "Trainer") -> None:
        """Called at the end of training."""
        pass

    def on_epoch_start(self, trainer: "Trainer", epoch: int) -> None:
        """Called at the start of each epoch."""
        pass

    def on_epoch_end(self, trainer: "Trainer", epoch: int, metrics: dict) -> bool:
        """Called at the end of each epoch. Return True to stop training."""
        return False

    def on_batch_end(self, trainer: "Trainer", batch_idx: int, metrics: dict) -> None:
        """Called at the end of each batch."""
        pass


class EarlyStopping(TrainingCallback):
    """Stop training when validation loss stops improving."""

    def __init__(
        self,
        patience: int = 10,
        min_delta: float = 1e-4,
        metric: str = "loss",
        mode: str = "min",
    ):
        self.patience = patience
        self.min_delta = min_delta
        self.metric = metric
        self.mode = mode
        self.best_value = float("inf") if mode == "min" else float("-inf")
        self.counter = 0

    def on_epoch_end(self, trainer: "Trainer", epoch: int, metrics: dict) -> bool:
        current = metrics.get(self.metric)
        if current is None:
            return False

        if self.mode == "min":
            improved = current < (self.best_value - self.min_delta)
        else:
            improved = current > (self.best_value + self.min_delta)

        if improved:
            self.best_value = current
            self.counter = 0
        else:
            self.counter += 1
            if self.counter >= self.patience:
                print(f"EarlyStopping: No improvement for {self.patience} epochs")
                return True

        return False


class ModelCheckpoint(TrainingCallback):
    """Save model checkpoints."""

    def __init__(
        self,
        save_dir: str | Path,
        save_best: bool = True,
        save_last: bool = True,
        metric: str = "loss",
        mode: str = "min",
    ):
        self.save_dir = Path(save_dir)
        self.save_dir.mkdir(parents=True, exist_ok=True)
        self.save_best = save_best
        self.save_last = save_last
        self.metric = metric
        self.mode = mode
        self.best_value = float("inf") if mode == "min" else float("-inf")

    def on_epoch_end(self, trainer: "Trainer", epoch: int, metrics: dict) -> bool:
        # Save last
        if self.save_last:
            trainer.save_checkpoint(self.save_dir / "last.pt")

        # Save best
        if self.save_best:
            current = metrics.get(self.metric)
            if current is not None:
                if self.mode == "min":
                    improved = current < self.best_value
                else:
                    improved = current > self.best_value

                if improved:
                    self.best_value = current
                    trainer.save_checkpoint(self.save_dir / "best.pt")
                    print(f"  Saved best model: {self.metric}={current:.4f}")

        return False


class ProgressLogger(TrainingCallback):
    """Log training progress with detailed metrics."""

    def on_epoch_start(self, trainer: "Trainer", epoch: int) -> None:
        total_epochs = trainer.config.epochs
        print(f"\nEpoch {epoch + 1}/{total_epochs}")
        print("-" * 60)

    def on_epoch_end(self, trainer: "Trainer", epoch: int, metrics: dict) -> bool:
        # Format metrics nicely
        loss = metrics.get("loss", 0)
        accuracy = metrics.get("accuracy", 0)
        mae = metrics.get("mae", 0)
        lr = metrics.get("lr", 0)

        print(f"  Loss: {loss:.4f} | Accuracy: {accuracy*100:.1f}% | MAE: {mae:.2f} | LR: {lr:.2e}")

        return False


class DetailedProgressLogger(TrainingCallback):
    """Log detailed training progress with train/val separation."""

    def __init__(self):
        self.train_metrics = {}

    def on_epoch_start(self, trainer: "Trainer", epoch: int) -> None:
        total_epochs = trainer.config.epochs
        print(f"\n{'='*60}")
        print(f"Epoch {epoch + 1}/{total_epochs}")
        print("=" * 60)

    def on_epoch_end(self, trainer: "Trainer", epoch: int, metrics: dict) -> bool:
        # Separate train and val metrics
        train_loss = metrics.get("loss", 0)
        train_acc = metrics.get("accuracy", 0)
        train_mae = metrics.get("mae")

        # Build train line
        train_line = f"  [TRAIN] Loss: {train_loss:.4f} | Accuracy: {train_acc*100:.1f}%"
        if train_mae is not None:
            train_line += f" | MAE: {train_mae:.2f}"
        print(train_line)

        # Check if we have val metrics
        if hasattr(trainer, 'epoch_history') and trainer.epoch_history:
            last = trainer.epoch_history[-1] if trainer.epoch_history else {}
            val_loss = last.get("val_loss")
            val_acc = last.get("val_accuracy")
            val_mae = last.get("val_mae")

            if val_loss is not None:
                val_line = f"  [VAL]   Loss: {val_loss:.4f} | Accuracy: {val_acc*100:.1f}%"
                if val_mae is not None:
                    val_line += f" | MAE: {val_mae:.2f}"
                print(val_line)

        lr = metrics.get("lr", 0)
        print(f"  [LR]    {lr:.2e}")

        return False
