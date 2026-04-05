"""Users schema definition for Identity bounded context."""

USERS_SCHEMA = """
-- =============================================================================
-- IDENTITY SCHEMA
-- =============================================================================
CREATE SCHEMA IF NOT EXISTS identity;

-- =============================================================================
-- USERS TABLE
-- =============================================================================
CREATE TABLE IF NOT EXISTS identity.users (
    -- Primary identification
    id VARCHAR(20) PRIMARY KEY,  -- Prefixed ID (e.g., U000000001)
    email VARCHAR(255) NOT NULL UNIQUE,
    username VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255),  -- NULL for OAuth-only users

    -- Account status
    is_active BOOLEAN DEFAULT TRUE,
    email_verified BOOLEAN DEFAULT FALSE,

    -- Two-factor authentication
    two_factor_enabled BOOLEAN DEFAULT FALSE,

    -- Account lockout
    lockout_end TIMESTAMP,
    failed_login_attempts INTEGER DEFAULT 0,

    -- Soft delete
    is_deleted BOOLEAN DEFAULT FALSE,
    deleted_at TIMESTAMP,

    -- Audit
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

-- Indexes for common queries
CREATE INDEX IF NOT EXISTS idx_users_email ON identity.users(email);
CREATE INDEX IF NOT EXISTS idx_users_username ON identity.users(username);
CREATE INDEX IF NOT EXISTS idx_users_is_active ON identity.users(is_active) WHERE is_active = TRUE;

-- =============================================================================
-- USER ROLES TABLE (Many-to-Many)
-- =============================================================================
CREATE TABLE IF NOT EXISTS identity.user_roles (
    user_id VARCHAR(20) NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    role_id INTEGER NOT NULL,  -- 1=User, 2=Admin, 3=SuperAdmin
    assigned_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (user_id, role_id)
);

CREATE INDEX IF NOT EXISTS idx_user_roles_user ON identity.user_roles(user_id);

-- =============================================================================
-- EXTERNAL LOGINS TABLE (OAuth providers)
-- =============================================================================
CREATE TABLE IF NOT EXISTS identity.external_logins (
    id SERIAL PRIMARY KEY,
    user_id VARCHAR(20) NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    provider VARCHAR(50) NOT NULL,  -- 'Google', 'GitHub', etc.
    provider_key VARCHAR(255) NOT NULL,  -- External user ID
    linked_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(provider, provider_key)
);

CREATE INDEX IF NOT EXISTS idx_external_logins_user ON identity.external_logins(user_id);
CREATE INDEX IF NOT EXISTS idx_external_logins_provider ON identity.external_logins(provider, provider_key);

-- =============================================================================
-- REFRESH TOKENS TABLE
-- =============================================================================
CREATE TABLE IF NOT EXISTS identity.refresh_tokens (
    id SERIAL PRIMARY KEY,
    user_id VARCHAR(20) NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    token VARCHAR(255) NOT NULL UNIQUE,
    expires_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    revoked_at TIMESTAMP,
    replaced_by_token VARCHAR(255),
    is_revoked BOOLEAN DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user ON identity.refresh_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_token ON identity.refresh_tokens(token);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_active ON identity.refresh_tokens(user_id, is_revoked)
    WHERE is_revoked = FALSE;
"""
