"""Settings configuration for GeneFlow Analysis Worker.

DEPRECATED: This module is deprecated. Use `src.config` instead.
This file is kept for backwards compatibility.
"""

import warnings

from src.config import Settings, settings

warnings.warn(
    "Importing from 'src.config' directly is deprecated. "
    "Use 'from src.config import Settings, settings' instead.",
    DeprecationWarning,
    stacklevel=2,
)

__all__ = ["Settings", "settings"]
