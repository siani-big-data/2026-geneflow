"""Tests for message parser module."""

import json
from datetime import datetime

import pytest

from src.consumer.message_parser import MessageParser
from src.models import EventBusMessage


class TestMessageParser:
    """Tests for MessageParser class."""

    @pytest.fixture
    def parser(self):
        return MessageParser()

    def test_parse_redis_message(self, parser):
        redis_data = {
            "eventId": "abc-123",
            "type": "UserRegistered",
            "timestamp": "1711357800000",
            "data": '{"userId": "user-1"}',
        }

        result = parser.parse_redis_message(redis_data)

        assert isinstance(result, EventBusMessage)
        assert result.eventId == "abc-123"
        assert result.type == "UserRegistered"
        assert result.timestamp == 1711357800000

    def test_parse_timestamp_valid(self, parser):
        timestamp_ms = 1711357800000

        result = parser.parse_timestamp(timestamp_ms)

        assert isinstance(result, datetime)
        assert result.year == 2024

    def test_parse_timestamp_invalid_returns_now(self, parser):
        before = datetime.utcnow()

        result = parser.parse_timestamp(None)

        after = datetime.utcnow()
        assert before <= result <= after

    def test_parse_timestamp_invalid_type(self, parser):
        before = datetime.utcnow()

        result = parser.parse_timestamp("not a number")

        after = datetime.utcnow()
        assert before <= result <= after

    def test_parse_event_data_json_string(self, parser):
        data = '{"userId": "user-1", "email": "test@example.com"}'

        result = parser.parse_event_data(data)

        assert result == {"userId": "user-1", "email": "test@example.com"}

    def test_parse_event_data_dict_passthrough(self, parser):
        data = {"userId": "user-1", "email": "test@example.com"}

        result = parser.parse_event_data(data)

        assert result == {"userId": "user-1", "email": "test@example.com"}

    def test_parse_event_data_invalid_json(self, parser):
        data = "not valid json"

        result = parser.parse_event_data(data)

        assert result == {"raw": "not valid json"}

    def test_parse_event_data_other_type(self, parser):
        data = 12345

        result = parser.parse_event_data(data)

        assert result == {"raw": "12345"}

    def test_parse_event_data_list(self, parser):
        data = [1, 2, 3]

        result = parser.parse_event_data(data)

        assert result == {"raw": "[1, 2, 3]"}

    def test_create_datalake_event(self, parser):
        message = EventBusMessage(
            eventId="abc-123",
            type="UserRegistered",
            timestamp=1711357800000,
            data='{"userId": "user-1"}',
        )

        result = parser.create_datalake_event(message, "users", "stream:users-1-0")

        assert result.eventId == "abc-123"
        assert result.type == "UserRegistered"
        assert result.category == "users"
        assert result.streamId == "stream:users-1-0"
        assert result.data == {"userId": "user-1"}
        assert isinstance(result.timestamp, datetime)

    def test_create_datalake_event_with_dict_data(self, parser):
        message = EventBusMessage(
            eventId="abc-123",
            type="UserRegistered",
            timestamp=1711357800000,
            data={"already": "dict"},
        )

        result = parser.create_datalake_event(message, "users", "stream:users-1-0")

        assert result.data == {"already": "dict"}

    def test_create_datalake_event_normalizes_timestamp(self, parser):
        message = EventBusMessage(
            eventId="abc-123",
            type="UserRegistered",
            timestamp=1711357800000,
            data="{}",
        )

        result = parser.create_datalake_event(message, "users", "1-0")

        assert result.timestamp.year == 2024
        assert result.timestamp.month == 3
        assert result.timestamp.day == 25

    def test_to_json_line_produces_valid_json(self, parser):
        message = EventBusMessage(
            eventId="abc-123",
            type="UserRegistered",
            timestamp=1711357800000,
            data='{"userId": "user-1"}',
        )

        event = parser.create_datalake_event(message, "users", "1-0")
        json_line = event.to_json_line()

        parsed = json.loads(json_line)
        assert parsed["eventId"] == "abc-123"
        assert parsed["type"] == "UserRegistered"
        assert parsed["category"] == "users"
        assert parsed["data"] == {"userId": "user-1"}


class TestMessageParserEdgeCases:
    """Edge case tests for MessageParser."""

    @pytest.fixture
    def parser(self):
        return MessageParser()

    def test_parse_empty_redis_message(self, parser):
        redis_data = {}

        result = parser.parse_redis_message(redis_data)

        assert result.eventId == ""
        assert result.type == ""
        assert result.timestamp == 0

    def test_parse_nested_json_data(self, parser):
        data = '{"user": {"profile": {"name": "Test"}, "settings": {"theme": "dark"}}}'

        result = parser.parse_event_data(data)

        assert result["user"]["profile"]["name"] == "Test"
        assert result["user"]["settings"]["theme"] == "dark"

    def test_parse_data_with_unicode(self, parser):
        data = '{"name": "Tést Üser", "emoji": "🚀"}'

        result = parser.parse_event_data(data)

        assert result["name"] == "Tést Üser"
        assert result["emoji"] == "🚀"

    def test_parse_data_with_special_characters(self, parser):
        data = '{"path": "C:\\\\Users\\\\test", "quote": "He said \\"hello\\""}'

        result = parser.parse_event_data(data)

        assert result["path"] == "C:\\Users\\test"
        assert result["quote"] == 'He said "hello"'

    def test_parse_data_with_null_values(self, parser):
        data = '{"value": null, "list": [null, 1, null]}'

        result = parser.parse_event_data(data)

        assert result["value"] is None
        assert result["list"] == [None, 1, None]

    def test_parse_timestamp_zero(self, parser):
        result = parser.parse_timestamp(0)

        assert isinstance(result, datetime)
        assert result.year == 1970
