"""Tests for PaymentsHandler."""

import json
from unittest.mock import AsyncMock, MagicMock

import pytest

from src.mounters.postgres.handlers.payments import PaymentsHandler


@pytest.fixture
def conn():
    c = MagicMock()
    c.execute = AsyncMock()
    return c


@pytest.fixture
def handler(conn):
    return PaymentsHandler(conn)


class TestPaymentsHandler:
    def test_event_mappings(self, handler):
        assert handler._event_mappings["PaymentMethodAddedEvent"] == "insert_payment_method"
        assert handler._event_mappings["PaymentMethodRemovedEvent"] == "delete_payment_method"
        assert (
            handler._event_mappings["PaymentMethodSetAsDefaultEvent"]
            == "set_default_payment_method"
        )
        assert (
            handler._event_mappings["PaymentMethodExpiredEvent"] == "update_payment_method_status"
        )
        assert handler._event_mappings["PaymentMethodFailedEvent"] == "update_payment_method_status"
        assert handler._event_mappings["StripeCustomerCreatedEvent"] == "insert_stripe_customer"

    async def test_insert_payment_method_uses_payment_method_id(self, handler, conn):
        await handler.insert_payment_method(
            {
                "payment_method_id": "pm-1",
                "user_id": "u-1",
                "stripe_customer_id": "cus-1",
                "stripe_payment_method_id": "spm-1",
                "card_last4": "4242",
                "card_brand": "visa",
                "card_exp_month": 12,
                "card_exp_year": 2030,
                "billing_country": "US",
                "billing_postal_code": "10001",
                "status": 1,
                "is_default": True,
                "occurred_at": "2024-01-01T00:00:00Z",
            }
        )
        # insert + log_event = 2 calls
        assert conn.execute.await_count == 2
        first = conn.execute.await_args_list[0]
        assert "INSERT INTO payments.payment_methods" in first.args[0]
        assert first.args[1] == "pm-1"

    async def test_insert_payment_method_falls_back_to_id_field(self, handler, conn):
        await handler.insert_payment_method({"id": "pm-2", "user_id": "u"})
        first = conn.execute.await_args_list[0]
        assert first.args[1] == "pm-2"
        # status defaults to 1
        assert first.args[11] == 1
        # is_default defaults to False
        assert first.args[12] is False

    async def test_delete_payment_method(self, handler, conn):
        await handler.delete_payment_method({"payment_method_id": "pm-1", "user_id": "u-1"})
        assert conn.execute.await_count == 2
        first = conn.execute.await_args_list[0]
        assert "DELETE FROM payments.payment_methods" in first.args[0]

    async def test_delete_payment_method_id_fallback(self, handler, conn):
        await handler.delete_payment_method({"id": "pm-z", "user_id": "u-1"})
        first = conn.execute.await_args_list[0]
        assert first.args[1] == "pm-z"

    async def test_set_default_payment_method(self, handler, conn):
        await handler.set_default_payment_method({"payment_method_id": "pm-1", "user_id": "u-1"})
        # 3 calls: unset others, set this, log
        assert conn.execute.await_count == 3
        assert "is_default = FALSE" in conn.execute.await_args_list[0].args[0]
        assert "is_default = TRUE" in conn.execute.await_args_list[1].args[0]

    async def test_set_default_payment_method_id_fallback(self, handler, conn):
        await handler.set_default_payment_method({"id": "pm-2", "user_id": "u-1"})
        assert conn.execute.await_args_list[1].args[1] == "pm-2"

    async def test_update_payment_method_status_expired(self, handler, conn):
        await handler.update_payment_method_status(
            {"payment_method_id": "pm-1", "user_id": "u-1", "status": 2}
        )
        # update + log
        assert conn.execute.await_count == 2
        log_call = conn.execute.await_args_list[1]
        assert log_call.args[3] == "PaymentMethodExpired"

    async def test_update_payment_method_status_failed(self, handler, conn):
        await handler.update_payment_method_status(
            {"payment_method_id": "pm-1", "user_id": "u-1", "status": 3}
        )
        log_call = conn.execute.await_args_list[1]
        assert log_call.args[3] == "PaymentMethodFailed"

    async def test_update_payment_method_status_other(self, handler, conn):
        await handler.update_payment_method_status(
            {"payment_method_id": "pm-1", "user_id": "u-1", "status": 99}
        )
        log_call = conn.execute.await_args_list[1]
        assert log_call.args[3] == "PaymentMethodStatusUpdated"

    async def test_insert_stripe_customer(self, handler, conn):
        await handler.insert_stripe_customer(
            {
                "user_id": "u-1",
                "stripe_customer_id": "cus-1",
                "occurred_at": "2024-01-01T00:00:00Z",
            }
        )
        conn.execute.assert_awaited_once()
        args = conn.execute.await_args.args
        assert "INSERT INTO payments.stripe_customers" in args[0]

    async def test_log_event_sanitizes_sensitive(self, handler, conn):
        await handler._log_event(
            "pm-1",
            "u-1",
            "Type",
            {"stripe_customer_id": "secret", "card_last4": "4242"},
        )
        conn.execute.assert_awaited_once()
        # Sensitive removed in event_data JSON
        json_data = json.loads(conn.execute.await_args.args[4])
        assert "stripe_customer_id" not in json_data
        assert "card_last4" in json_data

    async def test_truncate(self, handler, conn):
        await handler.truncate()
        assert conn.execute.await_count == 3
