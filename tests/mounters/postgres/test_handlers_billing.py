"""Tests for BillingHandler."""

from unittest.mock import AsyncMock, MagicMock

import pytest

from src.mounters.postgres.handlers.billing import BillingHandler


@pytest.fixture
def conn():
    c = MagicMock()
    c.execute = AsyncMock()
    return c


@pytest.fixture
def handler(conn):
    return BillingHandler(conn)


class TestBillingHandler:
    def test_event_mappings(self, handler):
        assert handler._event_mappings["PlanCreated"] == "insert_plan"
        assert handler._event_mappings["SubscriptionCreated"] == "insert_subscription"
        assert handler._event_mappings["SubscriptionUpdated"] == "update_subscription"
        assert handler._event_mappings["SubscriptionCancelled"] == "update_subscription"

    async def test_insert_plan(self, handler, conn):
        await handler.insert_plan(
            {
                "id": "plan-1",
                "name": "Pro",
                "monthly_price": 9.99,
                "annual_price": 99.99,
                "max_studies": 10,
            }
        )
        conn.execute.assert_awaited_once()
        args = conn.execute.await_args.args
        assert "INSERT INTO billing.plans" in args[0]

    async def test_insert_subscription(self, handler, conn):
        await handler.insert_subscription(
            {
                "id": "sub-1",
                "user_id": "u-1",
                "plan_id": "p-1",
                "plan_name": "Pro",
                "period_start": "2024-01-01T00:00:00Z",
                "period_end": "2024-02-01T00:00:00Z",
            }
        )
        conn.execute.assert_awaited_once()
        args = conn.execute.await_args.args
        assert "INSERT INTO billing.subscriptions" in args[0]

    async def test_update_subscription_with_status_and_reason(self, handler, conn):
        await handler.update_subscription(
            {
                "id": "sub-1",
                "status": "Cancelled",
                "cancellation_reason": "user request",
                "cancelled_at": "2024-01-01T00:00:00Z",
            }
        )
        conn.execute.assert_awaited_once()
        args = conn.execute.await_args.args
        assert "UPDATE billing.subscriptions" in args[0]
        assert "status = $1" in args[0]

    async def test_update_subscription_only_status(self, handler, conn):
        await handler.update_subscription({"id": "sub-1", "status": "Active"})
        conn.execute.assert_awaited_once()

    async def test_update_subscription_no_fields_skipped(self, handler, conn):
        # Only id present, no actual updateable fields -> no execute called
        await handler.update_subscription({"id": "sub-1"})
        conn.execute.assert_not_called()

    async def test_update_subscription_empty_payload_skipped(self, handler, conn):
        await handler.update_subscription({})
        conn.execute.assert_not_called()
