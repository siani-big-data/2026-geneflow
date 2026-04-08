"""Payments event handler for PostgreSQL."""

import json

from src.mounters.postgres.handlers.base import BaseHandler


class PaymentsHandler(BaseHandler):
    """Handler for payment-related events from PaymentMethods bounded context."""

    def __init__(self, connection):
        super().__init__(connection)
        self._event_mappings = {
            # Payment Method events
            "PaymentMethodAddedEvent": "insert_payment_method",
            "PaymentMethodRemovedEvent": "delete_payment_method",
            "PaymentMethodSetAsDefaultEvent": "set_default_payment_method",
            "PaymentMethodExpiredEvent": "update_payment_method_status",
            "PaymentMethodFailedEvent": "update_payment_method_status",
            # Stripe Customer events
            "StripeCustomerCreatedEvent": "insert_stripe_customer",
        }

    async def insert_payment_method(self, payload: dict) -> None:
        """Insert a new payment method."""
        await self._connection.execute(
            """INSERT INTO payments.payment_methods (
                id, user_id, stripe_customer_id, stripe_payment_method_id,
                card_last4, card_brand, card_exp_month, card_exp_year,
                billing_country, billing_postal_code, status, is_default, created_at
            ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13)
            ON CONFLICT (id) DO NOTHING""",
            payload.get("payment_method_id") or payload.get("id"),
            payload.get("user_id"),
            payload.get("stripe_customer_id"),
            payload.get("stripe_payment_method_id"),
            payload.get("card_last4"),
            payload.get("card_brand"),
            payload.get("card_exp_month"),
            payload.get("card_exp_year"),
            payload.get("billing_country"),
            payload.get("billing_postal_code"),
            payload.get("status", 1),  # 1 = Active
            payload.get("is_default", False),
            payload.get("occurred_at"),
        )

        # Log the event
        await self._log_event(
            payload.get("payment_method_id") or payload.get("id"),
            payload.get("user_id"),
            "PaymentMethodAdded",
            payload,
        )

    async def delete_payment_method(self, payload: dict) -> None:
        """Delete a payment method."""
        payment_method_id = payload.get("payment_method_id") or payload.get("id")

        await self._connection.execute(
            """DELETE FROM payments.payment_methods WHERE id = $1""",
            payment_method_id,
        )

        # Log the event
        await self._log_event(
            payment_method_id,
            payload.get("user_id"),
            "PaymentMethodRemoved",
            payload,
        )

    async def set_default_payment_method(self, payload: dict) -> None:
        """Set a payment method as default (and unset others)."""
        user_id = payload.get("user_id")
        payment_method_id = payload.get("payment_method_id") or payload.get("id")

        # Unset current default
        await self._connection.execute(
            """UPDATE payments.payment_methods
            SET is_default = FALSE, modified_at = CURRENT_TIMESTAMP
            WHERE user_id = $1 AND is_default = TRUE""",
            user_id,
        )

        # Set new default
        await self._connection.execute(
            """UPDATE payments.payment_methods
            SET is_default = TRUE, modified_at = CURRENT_TIMESTAMP
            WHERE id = $1""",
            payment_method_id,
        )

        # Log the event
        await self._log_event(
            payment_method_id,
            user_id,
            "PaymentMethodSetAsDefault",
            payload,
        )

    async def update_payment_method_status(self, payload: dict) -> None:
        """Update payment method status (expired, failed, etc.)."""
        payment_method_id = payload.get("payment_method_id") or payload.get("id")
        status = payload.get("status")

        await self._connection.execute(
            """UPDATE payments.payment_methods
            SET status = $1, modified_at = CURRENT_TIMESTAMP
            WHERE id = $2""",
            status,
            payment_method_id,
        )

        # Determine event type from status
        event_type = "PaymentMethodStatusUpdated"
        if status == 2:
            event_type = "PaymentMethodExpired"
        elif status == 3:
            event_type = "PaymentMethodFailed"

        # Log the event
        await self._log_event(
            payment_method_id,
            payload.get("user_id"),
            event_type,
            payload,
        )

    async def insert_stripe_customer(self, payload: dict) -> None:
        """Insert Stripe customer mapping."""
        await self._connection.execute(
            """INSERT INTO payments.stripe_customers (user_id, stripe_customer_id, created_at)
            VALUES ($1, $2, $3)
            ON CONFLICT (user_id) DO UPDATE SET stripe_customer_id = $2""",
            payload.get("user_id"),
            payload.get("stripe_customer_id"),
            payload.get("occurred_at"),
        )

    async def _log_event(
        self,
        payment_method_id: str | None,
        user_id: str,
        event_type: str,
        payload: dict,
    ) -> None:
        """Log a payment event for audit purposes."""
        # Remove sensitive data before logging
        safe_payload = {
            k: v
            for k, v in payload.items()
            if k not in ["stripe_customer_id", "stripe_payment_method_id"]
        }

        await self._connection.execute(
            """INSERT INTO payments.payment_events_log (payment_method_id, user_id, event_type, event_data)
            VALUES ($1, $2, $3, $4)""",
            payment_method_id,
            user_id,
            event_type,
            json.dumps(safe_payload),
        )

    async def truncate(self) -> None:
        """Truncate all payments tables for rebuild."""
        await self._connection.execute("TRUNCATE payments.payment_events_log CASCADE")
        await self._connection.execute("TRUNCATE payments.payment_methods CASCADE")
        await self._connection.execute("TRUNCATE payments.stripe_customers CASCADE")
