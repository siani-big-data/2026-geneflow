"""DEPRECATED: Use src.api.responses instead.

This module exists for backwards compatibility only.
All imports are re-exported from the correctly spelled 'responses' module.
"""

import warnings

warnings.warn(
    "src.api.reponses is deprecated, use src.api.responses instead",
    DeprecationWarning,
    stacklevel=2,
)

from src.api.responses import (
    AvailableCategoriesResponse,
    CategoriesResponse,
    CategoryDatesResponse,
    CategoryStatsResponse,
    DLQResponse,
    DLQRetryAllResponse,
    DLQRetryResponse,
    ErrorResponse,
    EventsResponse,
    HealthResponse,
    ReplayResponse,
)

__all__ = [
    "AvailableCategoriesResponse",
    "CategoriesResponse",
    "CategoryDatesResponse",
    "CategoryStatsResponse",
    "DLQResponse",
    "DLQRetryAllResponse",
    "DLQRetryResponse",
    "ErrorResponse",
    "EventsResponse",
    "HealthResponse",
    "ReplayResponse",
]
