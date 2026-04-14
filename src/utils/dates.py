"""Centralized date parsing and formatting utilities.

Handles the various date formats encountered from C# backend serialization
and provides consistent parsing/formatting across the application.
"""

from datetime import datetime
from typing import Union

from src.constants import DATE_FORMAT

ISO_FORMAT_WITH_TIMEZONE = "%Y-%m-%dT%H:%M:%S%z"
ISO_FORMAT_WITH_Z = "%Y-%m-%dT%H:%M:%SZ"
ISO_FORMAT_WITH_MS_Z = "%Y-%m-%dT%H:%M:%S.%fZ"


def parse_date(value: Union[str, datetime, None]) -> datetime | None:
    """Parse a date string in YYYY-MM-DD format.

    Args:
        value: Date string, datetime, or None

    Returns:
        datetime object or None if parsing fails
    """
    if value is None:
        return None
    if isinstance(value, datetime):
        return value
    try:
        return datetime.strptime(value, DATE_FORMAT)
    except (ValueError, TypeError):
        return None


def parse_datetime(value: Union[str, datetime, None]) -> datetime | None:
    """Parse an ISO datetime string, handling various formats from C# serialization.

    Handles:
    - 2024-01-01T00:00:00Z
    - 2024-01-01T00:00:00.000Z
    - 2024-01-01T00:00:00+00:00

    Args:
        value: ISO datetime string, datetime, or None

    Returns:
        datetime object or None if parsing fails
    """
    if value is None:
        return None
    if isinstance(value, datetime):
        return value

    normalized = value.replace("Z", "+00:00")

    try:
        return datetime.fromisoformat(normalized)
    except (ValueError, TypeError):
        return None


def format_date(value: datetime | None) -> str | None:
    """Format a datetime to YYYY-MM-DD string.

    Args:
        value: datetime object or None

    Returns:
        Formatted date string or None
    """
    if value is None:
        return None
    return value.strftime(DATE_FORMAT)


def format_datetime(value: datetime | None) -> str | None:
    """Format a datetime to ISO format with Z suffix.

    Args:
        value: datetime object or None

    Returns:
        ISO formatted string with Z suffix or None
    """
    if value is None:
        return None
    return value.strftime(ISO_FORMAT_WITH_MS_Z)
