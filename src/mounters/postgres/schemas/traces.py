"""Traces schema definition."""

TRACES_SCHEMA = """
CREATE SCHEMA IF NOT EXISTS traces;

CREATE TABLE IF NOT EXISTS traces.traces (
    id UUID PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    study_id UUID NOT NULL,
    uploaded_by UUID NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    format_id INTEGER NOT NULL,
    size_bytes BIGINT NOT NULL,
    status_id INTEGER DEFAULT 1,
    total_bases INTEGER,
    average_quality_score DECIMAL(5,2),
    processed_at TIMESTAMP,
    is_deleted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS traces.annotations (
    id UUID PRIMARY KEY,
    trace_id UUID NOT NULL,
    type_id INTEGER NOT NULL,
    label VARCHAR(255),
    start_position INTEGER NOT NULL,
    end_position INTEGER NOT NULL,
    created_by UUID NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
"""
