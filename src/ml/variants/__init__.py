"""Variant detection models."""

from .heterozygote_detector import HeterozygoteDetector
from .snp_caller import SNPCaller

__all__ = ["SNPCaller", "HeterozygoteDetector"]
