"""Base model class for all custom ML models."""

from abc import ABC, abstractmethod
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

import torch
import torch.nn as nn


@dataclass
class ModelConfig:
    """Base configuration for models."""

    name: str
    version: str = "1.0.0"
    input_size: int = 0
    output_size: int = 0
    hidden_size: int = 256
    num_layers: int = 4
    dropout: float = 0.1
    learning_rate: float = 1e-4
    batch_size: int = 32
    max_epochs: int = 100
    early_stopping_patience: int = 10
    device: str = "cuda" if torch.cuda.is_available() else "cpu"
    extra: dict = field(default_factory=dict)

    def to_dict(self) -> dict:
        return {
            "name": self.name,
            "version": self.version,
            "input_size": self.input_size,
            "output_size": self.output_size,
            "hidden_size": self.hidden_size,
            "num_layers": self.num_layers,
            "dropout": self.dropout,
            "learning_rate": self.learning_rate,
            "batch_size": self.batch_size,
            "max_epochs": self.max_epochs,
            "device": self.device,
            **self.extra,
        }


class BaseModel(nn.Module, ABC):
    """Abstract base class for all GeneFlow ML models."""

    def __init__(self, config: ModelConfig):
        super().__init__()
        self.config = config
        self._is_trained = False

    @property
    def name(self) -> str:
        return self.config.name

    @property
    def version(self) -> str:
        return self.config.version

    @property
    def is_trained(self) -> bool:
        return self._is_trained

    @property
    def device(self) -> torch.device:
        return torch.device(self.config.device)

    @abstractmethod
    def forward(self, x: torch.Tensor) -> torch.Tensor:
        """Forward pass."""
        pass

    @abstractmethod
    def predict(self, x: Any) -> Any:
        """Inference method - takes raw input, returns processed output."""
        pass

    def save(self, path: str | Path) -> None:
        """Save model checkpoint."""
        path = Path(path)
        path.parent.mkdir(parents=True, exist_ok=True)

        checkpoint = {
            "config": self.config.to_dict(),
            "state_dict": self.state_dict(),
            "is_trained": self._is_trained,
        }
        torch.save(checkpoint, path)

    def load(self, path: str | Path) -> None:
        """Load model checkpoint."""
        checkpoint = torch.load(path, map_location=self.device, weights_only=False)
        self.load_state_dict(checkpoint["state_dict"])
        self._is_trained = checkpoint.get("is_trained", True)

    def count_parameters(self) -> int:
        """Count trainable parameters."""
        return sum(p.numel() for p in self.parameters() if p.requires_grad)

    def to_device(self) -> "BaseModel":
        """Move model to configured device."""
        return self.to(self.device)
