"""Profiles schema definition for Profiles bounded context."""

PROFILES_SCHEMA = """
-- =============================================================================
-- PROFILES SCHEMA
-- =============================================================================
CREATE SCHEMA IF NOT EXISTS profiles;

-- =============================================================================
-- PROFILES TABLE
-- =============================================================================
CREATE TABLE IF NOT EXISTS profiles.profiles (
    -- Primary identification
    id VARCHAR(20) PRIMARY KEY,  -- Prefixed ID (e.g., P000000001)
    user_id VARCHAR(20) NOT NULL UNIQUE,  -- Reference to identity.users

    -- Personal information
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100),

    -- Professional information
    bio TEXT,
    location VARCHAR(200),
    professional_role VARCHAR(100),
    institution_name VARCHAR(200),
    institution_department VARCHAR(200),
    research_field VARCHAR(50),  -- Smart enum name (e.g., 'Genomics', 'Proteomics')

    -- Research identifiers
    orcid_id VARCHAR(19),  -- Format: 0000-0000-0000-0000
    website VARCHAR(500),

    -- Profile photo
    photo_url VARCHAR(1000),
    photo_thumbnail_url VARCHAR(1000),

    -- Status
    is_complete BOOLEAN DEFAULT FALSE,

    -- Soft delete
    is_deleted BOOLEAN DEFAULT FALSE,
    deleted_at TIMESTAMP,

    -- Audit
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

-- Indexes for common queries
CREATE INDEX IF NOT EXISTS idx_profiles_user_id ON profiles.profiles(user_id);
CREATE INDEX IF NOT EXISTS idx_profiles_research_field ON profiles.profiles(research_field) WHERE research_field IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_profiles_institution ON profiles.profiles(institution_name) WHERE institution_name IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_profiles_orcid ON profiles.profiles(orcid_id) WHERE orcid_id IS NOT NULL;
"""
