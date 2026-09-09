"""Billing event handler for PostgreSQL."""

from src.mounters.postgres.handlers.base import BaseHandler


class BillingHandler(BaseHandler):
    """Handler for billing-related events."""

    def __init__(self, connection):
        super().__init__(connection)
        self._event_mappings = {
            "PlanCreated": "insert_plan",
            "SubscriptionCreated": "insert_subscription",
            "SubscriptionUpdated": "update_subscription",
            "SubscriptionCancelled": "update_subscription",
        }

    async def insert_plan(self, payload: dict) -> None:
        """Insert a billing plan."""
        await self._connection.execute(
            """INSERT INTO billing.plans (id, name, monthly_price, annual_price, max_studies)
            VALUES ($1, $2, $3, $4, $5)""",
            payload.get("id"),
            payload.get("name"),
            payload.get("monthly_price"),
            payload.get("annual_price"),
            payload.get("max_studies"),
        )

    async def insert_subscription(self, payload: dict) -> None:
        """Insert a subscription."""
        await self._connection.execute(
            """INSERT INTO billing.subscriptions (id, user_id, plan_id, plan_name, period_start, period_end)
            VALUES ($1, $2, $3, $4, $5, $6)""",
            payload.get("id"),
            payload.get("user_id"),
            payload.get("plan_id"),
            payload.get("plan_name"),
            payload.get("period_start"),
            payload.get("period_end"),
        )

    async def update_subscription(self, payload: dict) -> None:
        """Update a subscription."""
        updates = []
        values = []
        idx = 1

        for key in ["status", "cancellation_reason", "cancelled_at"]:
            if key in payload:
                updates.append(f"{key} = ${idx}")
                values.append(payload[key])
                idx += 1

        if updates:
            values.append(payload.get("id"))
            query = f"UPDATE billing.subscriptions SET {', '.join(updates)} WHERE id = ${idx}"
            await self._connection.execute(query, *values)
