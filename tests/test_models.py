import json
from datetime import datetime

from src.models import (
    DatalakeEvent,
    DLQEvent,
    EventBusMessage,
    EventCategory,
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
        ]
        assert [c.value for c in EventCategory] == expected

    def test_category_is_string(self):
        assert EventCategory.USERS == "users"
        assert EventCategory.USERS.value == "users"

    def test_category_serializable(self):
        data = {"category": EventCategory.USERS}
        result = json.dumps(data)
        assert '"users"' in result


class TestEventBusMessage:
    def test_from_redis(self):
        redis_data = {
            "eventId": "abc-123",
            "type": "UserRegistered",
            "category": "users",
            "timestamp": "1711357800000",
            "data": '{"userId": "user-1"}',
            "source": "api",
            "version": "1.0",
            "correlationId": "corr-456",
        }

        msg = EventBusMessage.from_redis(redis_data)

        assert msg.eventId == "abc-123"
        assert msg.type == "UserRegistered"
        assert msg.category == "users"
        assert msg.timestamp == 1711357800000
        assert msg.data == '{"userId": "user-1"}'
        assert msg.source == "api"
        assert msg.correlationId == "corr-456"

    def test_from_redis_defaults(self):
        redis_data = {}

        msg = EventBusMessage.from_redis(redis_data)

        assert msg.eventId == ""
        assert msg.type == "Unknown"
        assert msg.category == "unknown"
        assert msg.source == "unknown"
        assert msg.version == "1.0"
        assert msg.correlationId is None


class TestDatalakeEvent:
    def test_to_json_line(self):
        event = DatalakeEvent(
            eventId="abc-123",
            type="UserRegistered",
            category="users",
            timestamp=datetime(2026, 3, 25, 10, 0, 0),
            streamId="1234-0",
            data={"userId": "user-1"},
            receivedAt=datetime(2026, 3, 25, 10, 0, 1),
        )

        line = event.to_json_line()
        parsed = json.loads(line)

        assert parsed["eventId"] == "abc-123"
        assert parsed["type"] == "UserRegistered"
        assert parsed["category"] == "users"
        assert parsed["timestamp"] == "2026-03-25T10:00:00"
        assert parsed["streamId"] == "1234-0"
        assert parsed["data"] == {"userId": "user-1"}
        assert parsed["receivedAt"] == "2026-03-25T10:00:01"


class TestDLQEvent:
    def test_to_dict(self):
        event = DLQEvent(
            eventId="abc-123",
            category="users",
            date="2026-03-25",
            eventLine='{"test": true}',
            retryCount=5,
            lastError="Storage timeout",
            createdAt="2026-03-25T10:00:00",
            movedToDlqAt="2026-03-25T10:05:00",
        )

        result = event.to_dict()

        assert result["eventId"] == "abc-123"
        assert result["retryCount"] == 5
        assert result["lastError"] == "Storage timeout"
