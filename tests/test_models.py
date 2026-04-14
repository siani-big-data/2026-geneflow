"""Tests for model classes."""

import json
from datetime import datetime

from src.models import (
    DatalakeEvent,
    EventBusMessage,
    EventCategory,
    RetryableEvent,
)


class TestEventCategory:
    def test_all_categories_exist(self):
        expected = [
            "users",
            "studies",
            "traces",
            "alignments",
            "subscriptions",
            "plans",
            "ai",
            "blast",
            "system",
            "profiles",
            "billing",
            "payments",
        ]
        assert [c.value for c in EventCategory] == expected

    def test_category_is_string(self):
        assert EventCategory.USERS == "users"
        assert EventCategory.USERS.value == "users"

    def test_category_serializable(self):
        data = {"category": EventCategory.USERS}
        result = json.dumps(data)
        assert '"users"' in result

    def test_from_string(self):
        cat = EventCategory.from_string("users")
        assert cat == EventCategory.USERS

    def test_from_string_case_insensitive(self):
        cat = EventCategory.from_string("USERS")
        assert cat == EventCategory.USERS


class TestEventBusMessage:
    def test_from_redis(self):
        redis_data = {
            "eventId": "abc-123",
            "type": "UserRegistered",
            "timestamp": "1711357800000",
            "data": '{"userId": "user-1"}',
        }

        msg = EventBusMessage.from_redis(redis_data)

        assert msg.eventId == "abc-123"
        assert msg.type == "UserRegistered"
        assert msg.timestamp == 1711357800000
        assert msg.data == '{"userId": "user-1"}'

    def test_from_redis_defaults(self):
        redis_data = {}

        msg = EventBusMessage.from_redis(redis_data)

        assert msg.eventId == ""
        assert msg.type == ""
        assert msg.timestamp == 0
        assert msg.data == {}


class TestDatalakeEvent:
    def test_to_json_line(self):
        event = DatalakeEvent(
            eventId="abc-123",
            type="UserRegistered",
            category="users",
            timestamp=datetime(2026, 3, 25, 10, 0, 0),
            streamId="1234-0",
            data={"userId": "user-1"},
        )

        line = event.to_json_line()
        parsed = json.loads(line)

        assert parsed["eventId"] == "abc-123"
        assert parsed["type"] == "UserRegistered"
        assert parsed["category"] == "users"
        assert parsed["timestamp"] == "2026-03-25T10:00:00"
        assert parsed["streamId"] == "1234-0"
        assert parsed["data"] == {"userId": "user-1"}

    def test_to_dict(self):
        event = DatalakeEvent(
            eventId="abc-123",
            type="UserRegistered",
            category="users",
            timestamp=datetime(2026, 3, 25, 10, 0, 0),
            streamId="1234-0",
            data={"userId": "user-1"},
        )

        result = event.to_dict()

        assert result["eventId"] == "abc-123"
        assert result["category"] == "users"

    def test_date_partition(self):
        event = DatalakeEvent(
            eventId="abc-123",
            type="UserRegistered",
            category="users",
            timestamp=datetime(2026, 3, 25, 10, 0, 0),
            streamId="1234-0",
            data={},
        )

        assert event.date_partition == "2026-03-25"


class TestRetryableEvent:
    def test_fields(self):
        event = RetryableEvent(
            id="abc-123",
            category="users",
            date=datetime(2026, 3, 25),
            eventLine='{"test": true}',
            retryCount=5,
            lastError="Storage timeout",
        )

        assert event.id == "abc-123"
        assert event.category == "users"
        assert event.retryCount == 5
        assert event.lastError == "Storage timeout"
        assert event.eventLine == '{"test": true}'

    def test_default_created_at(self):
        before = datetime.utcnow()
        event = RetryableEvent(
            id="abc-123",
            category="users",
            date=datetime(2026, 3, 25),
            eventLine='{}',
            retryCount=0,
            lastError="",
        )
        after = datetime.utcnow()

        assert before <= event.createdAt <= after

    def test_optional_next_retry_at(self):
        event = RetryableEvent(
            id="abc-123",
            category="users",
            date=datetime(2026, 3, 25),
            eventLine='{}',
            retryCount=0,
            lastError="",
            nextRetryAt=datetime(2026, 3, 25, 12, 0, 0),
        )

        assert event.nextRetryAt == datetime(2026, 3, 25, 12, 0, 0)
