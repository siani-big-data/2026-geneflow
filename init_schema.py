"""Initialize PostgreSQL schemas for GeneFlow (matching EF Core)."""

import asyncio
import os

import asyncpg


async def init_schemas():
    """Initialize all database schemas."""
    dsn = f"postgresql://{os.getenv('POSTGRES_USER', 'geneflow')}:{os.getenv('POSTGRES_PASSWORD', 'geneflow')}@{os.getenv('POSTGRES_HOST', 'localhost')}:{os.getenv('POSTGRES_PORT', '5432')}/{os.getenv('POSTGRES_DB', 'geneflow')}"

    print(f"Connecting to: {dsn}")

    conn = await asyncpg.connect(dsn)

    # Drop and recreate for clean state
    print("Dropping existing schema...")
    await conn.execute("DROP SCHEMA IF EXISTS identity CASCADE")

    # Users/Identity schema (matching EF Core UserConfiguration)
    users_schema = """
    CREATE SCHEMA IF NOT EXISTS identity;

    CREATE TABLE identity.users (
        id VARCHAR(10) PRIMARY KEY,
        email VARCHAR(255) NOT NULL UNIQUE,
        username VARCHAR(100) NOT NULL UNIQUE,
        password_hash VARCHAR(256) NOT NULL,
        is_active BOOLEAN NOT NULL DEFAULT TRUE,

        -- Email Verification (owned type)
        email_verified BOOLEAN NOT NULL DEFAULT FALSE,
        email_verification_token VARCHAR(128),
        email_verification_token_expiry TIMESTAMP,

        -- Password Reset (owned type)
        password_reset_token VARCHAR(128),
        password_reset_token_expiry TIMESTAMP,

        -- Account Lockout (owned type)
        failed_login_attempts INTEGER NOT NULL DEFAULT 0,
        lockout_end TIMESTAMP,

        -- Two Factor Auth (owned type)
        two_factor_enabled BOOLEAN NOT NULL DEFAULT FALSE,

        -- Roles as JSONB
        roles JSONB NOT NULL DEFAULT '["User"]'::jsonb,

        -- Auditing fields
        created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
        created_by VARCHAR(100),
        modified_at TIMESTAMP,
        modified_by VARCHAR(100),
        is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
        deleted_at TIMESTAMP,
        deleted_by VARCHAR(100)
    );

    CREATE INDEX idx_users_email ON identity.users(email);
    CREATE INDEX idx_users_username ON identity.users(username);

    -- Two Factor Codes table
    CREATE TABLE identity.two_factor_codes (
        id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
        "UserId" VARCHAR(10) NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
        code VARCHAR(6) NOT NULL,
        created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
        expires_at TIMESTAMP NOT NULL,
        is_used BOOLEAN NOT NULL DEFAULT FALSE,
        used_at TIMESTAMP
    );

    -- Refresh Tokens table
    CREATE TABLE identity.refresh_tokens (
        token VARCHAR(256) PRIMARY KEY,
        "UserId" VARCHAR(10) NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
        expires_at TIMESTAMP NOT NULL,
        created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
        is_revoked BOOLEAN NOT NULL DEFAULT FALSE,
        revoked_at TIMESTAMP,
        replaced_by_token VARCHAR(256)
    );

    CREATE INDEX idx_refresh_tokens_token ON identity.refresh_tokens(token);

    -- External Logins table
    CREATE TABLE identity.external_logins (
        id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
        "UserId" VARCHAR(10) NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
        provider VARCHAR(50) NOT NULL,
        provider_key VARCHAR(256) NOT NULL,
        provider_display_name VARCHAR(256),
        linked_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
        UNIQUE(provider, provider_key)
    );
    """

    print("Creating identity schema...")
    await conn.execute(users_schema)

    print("Schema initialized successfully!")
    await conn.close()


if __name__ == "__main__":
    asyncio.run(init_schemas())
