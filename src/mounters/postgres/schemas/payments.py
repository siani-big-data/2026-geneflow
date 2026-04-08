"""Payments schema definition for PaymentMethods bounded context."""

PAYMENTS_SCHEMA = """
-- =============================================================================
-- PAYMENTS SCHEMA
-- =============================================================================
CREATE SCHEMA IF NOT EXISTS payments;

-- =============================================================================
-- PAYMENT METHODS TABLE
-- =============================================================================
CREATE TABLE IF NOT EXISTS payments.payment_methods (
    -- Primary identification
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,

    -- Stripe IDs (never store actual card numbers)
    stripe_customer_id VARCHAR(255) NOT NULL,
    stripe_payment_method_id VARCHAR(255) NOT NULL,

    -- Card details (masked/safe info only)
    card_last4 CHAR(4) NOT NULL,
    card_brand INTEGER NOT NULL,  -- 1=Visa, 2=Mastercard, 3=Amex, etc.
    card_exp_month INTEGER NOT NULL,
    card_exp_year INTEGER NOT NULL,

    -- Billing address (optional)
    billing_country CHAR(2),
    billing_postal_code VARCHAR(20),

    -- Status and flags
    status INTEGER NOT NULL DEFAULT 1,  -- 1=Active, 2=Expired, 3=Failed
    is_default BOOLEAN NOT NULL DEFAULT FALSE,

    -- Audit
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_at TIMESTAMP
);

-- Indexes for common queries
CREATE INDEX IF NOT EXISTS idx_payment_methods_user_id
    ON payments.payment_methods(user_id);

CREATE INDEX IF NOT EXISTS idx_payment_methods_user_default
    ON payments.payment_methods(user_id, is_default)
    WHERE is_default = TRUE;

CREATE UNIQUE INDEX IF NOT EXISTS idx_payment_methods_stripe_pm_id
    ON payments.payment_methods(stripe_payment_method_id);

CREATE INDEX IF NOT EXISTS idx_payment_methods_status
    ON payments.payment_methods(status);

-- =============================================================================
-- STRIPE CUSTOMERS TABLE (maps users to Stripe customers)
-- =============================================================================
CREATE TABLE IF NOT EXISTS payments.stripe_customers (
    user_id UUID PRIMARY KEY,
    stripe_customer_id VARCHAR(255) NOT NULL UNIQUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- =============================================================================
-- PAYMENT EVENTS LOG (for audit and debugging)
-- =============================================================================
CREATE TABLE IF NOT EXISTS payments.payment_events_log (
    id SERIAL PRIMARY KEY,
    payment_method_id UUID,
    user_id UUID NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    event_data JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_payment_events_user
    ON payments.payment_events_log(user_id);

CREATE INDEX IF NOT EXISTS idx_payment_events_pm
    ON payments.payment_events_log(payment_method_id)
    WHERE payment_method_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_payment_events_type
    ON payments.payment_events_log(event_type);
"""
