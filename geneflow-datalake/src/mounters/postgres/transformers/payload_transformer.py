"""Transforms C# serialized payloads to Python format."""

from datetime import datetime
from typing import Any


class PayloadTransformer:
    """Transforms event payloads from C# serialization format to Python."""

    def get_field(self, payload: dict, *keys: str) -> Any:
        """Get a field from payload, trying multiple key variations (snake_case and PascalCase)."""
        for key in keys:
            if key in payload:
                return payload[key]
        return None

    def get_string(self, payload: dict, *keys: str) -> str | None:
        """Get a string field."""
        return self.get_field(payload, *keys)

    def get_int(self, payload: dict, *keys: str, default: int = 0) -> int:
        """Get an integer field with default."""
        value = self.get_field(payload, *keys)
        if value is None:
            return default
        return int(value)

    def get_datetime(self, payload: dict, *keys: str) -> datetime | None:
        """Get a datetime field, parsing ISO format with Z suffix."""
        value = self.get_field(payload, *keys)
        if value is None:
            return None
        if isinstance(value, datetime):
            return value
        return datetime.fromisoformat(value.replace("Z", "+00:00"))

    def get_value_object(self, payload: dict, *keys: str) -> Any:
        """Get a value from a C# value object (e.g., {"value": 123} or {"Value": 123})."""
        raw = self.get_field(payload, *keys)
        if isinstance(raw, dict):
            return raw.get("value") or raw.get("Value")
        return raw

    def get_bool(self, payload: dict, *keys: str, default: bool = False) -> bool:
        """Get a boolean field with default."""
        value = self.get_field(payload, *keys)
        if value is None:
            return default
        return bool(value)
