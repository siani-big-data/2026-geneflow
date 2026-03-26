"""Billing schema definition."""

BILLING_SCHEMA = """
CREATE SCHEMA IF NOT EXISTS billing;

CREATE TABLE IF NOT EXISTS billing.plans (
    id UUID PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    monthly_price DECIMAL(10,2),
    annual_price DECIMAL(10,2),
    max_studies INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS billing.subscriptions (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    plan_id UUID NOT NULL,
    plan_name VARCHAR(100),
    status INTEGER DEFAULT 1,
    period_start TIMESTAMP NOT NULL,
    period_end TIMESTAMP NOT NULL,
    cancellation_reason TEXT,
    cancelled_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
"""
