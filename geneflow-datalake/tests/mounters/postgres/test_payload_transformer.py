"""Tests for PayloadTransformer."""

from datetime import datetime

import pytest

from src.mounters.postgres.transformers.payload_transformer import PayloadTransformer


@pytest.fixture
def transformer():
    return PayloadTransformer()


class TestPayloadTransformer:
    def test_get_field_first_key_hit(self, transformer):
        assert transformer.get_field({"a": 1, "b": 2}, "a", "b") == 1

    def test_get_field_second_key_hit(self, transformer):
        assert transformer.get_field({"b": 2}, "a", "b") == 2

    def test_get_field_no_match(self, transformer):
        assert transformer.get_field({"x": 0}, "a", "b") is None

    def test_get_string(self, transformer):
        assert transformer.get_string({"name": "alice"}, "name") == "alice"

    def test_get_string_missing(self, transformer):
        assert transformer.get_string({}, "name") is None

    def test_get_int_int_value(self, transformer):
        assert transformer.get_int({"n": 5}, "n") == 5

    def test_get_int_string_value(self, transformer):
        assert transformer.get_int({"n": "10"}, "n") == 10

    def test_get_int_missing_default(self, transformer):
        assert transformer.get_int({}, "n") == 0

    def test_get_int_missing_custom_default(self, transformer):
        assert transformer.get_int({}, "n", default=42) == 42

    def test_get_datetime_from_iso_string(self, transformer):
        dt = transformer.get_datetime({"t": "2024-01-01T00:00:00Z"}, "t")
        assert isinstance(dt, datetime)
        assert dt.year == 2024

    def test_get_datetime_from_iso_string_no_z(self, transformer):
        dt = transformer.get_datetime({"t": "2024-01-01T00:00:00+00:00"}, "t")
        assert isinstance(dt, datetime)

    def test_get_datetime_passthrough(self, transformer):
        existing = datetime(2024, 1, 1)
        assert transformer.get_datetime({"t": existing}, "t") is existing

    def test_get_datetime_missing(self, transformer):
        assert transformer.get_datetime({}, "t") is None

    def test_get_value_object_lowercase_value(self, transformer):
        assert transformer.get_value_object({"id": {"value": "abc"}}, "id") == "abc"

    def test_get_value_object_pascal_value(self, transformer):
        assert transformer.get_value_object({"id": {"Value": "xyz"}}, "id") == "xyz"

    def test_get_value_object_raw_scalar(self, transformer):
        assert transformer.get_value_object({"id": "plain"}, "id") == "plain"

    def test_get_value_object_missing(self, transformer):
        assert transformer.get_value_object({}, "id") is None

    def test_get_bool_true(self, transformer):
        assert transformer.get_bool({"f": True}, "f") is True

    def test_get_bool_false(self, transformer):
        assert transformer.get_bool({"f": False}, "f") is False

    def test_get_bool_truthy_value(self, transformer):
        assert transformer.get_bool({"f": 1}, "f") is True

    def test_get_bool_missing_default(self, transformer):
        assert transformer.get_bool({}, "f") is False

    def test_get_bool_missing_custom_default(self, transformer):
        assert transformer.get_bool({}, "f", default=True) is True
