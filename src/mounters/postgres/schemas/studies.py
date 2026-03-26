"""Studies schema definition."""

STUDIES_SCHEMA = """
CREATE SCHEMA IF NOT EXISTS studies;

CREATE TABLE IF NOT EXISTS studies.studies (
    id UUID PRIMARY KEY,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    owner_id UUID NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP
);

CREATE TABLE IF NOT EXISTS studies.members (
    study_id UUID NOT NULL,
    user_id UUID NOT NULL,
    role_id INTEGER NOT NULL,
    invited_by UUID,
    PRIMARY KEY (study_id, user_id)
);

CREATE TABLE IF NOT EXISTS studies.invitations (
    id UUID PRIMARY KEY,
    study_id UUID NOT NULL,
    email VARCHAR(255) NOT NULL,
    token VARCHAR(255) NOT NULL,
    invited_by UUID NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    accepted_at TIMESTAMP
);
"""
