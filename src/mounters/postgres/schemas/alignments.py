"""Alignments schema definition."""

ALIGNMENTS_SCHEMA = """
CREATE SCHEMA IF NOT EXISTS alignments;

CREATE TABLE IF NOT EXISTS alignments.alignments (
    id UUID PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    study_id UUID NOT NULL,
    created_by UUID NOT NULL,
    type_id INTEGER NOT NULL,
    status_id INTEGER DEFAULT 1,
    alignment_length INTEGER,
    identity_percentage DECIMAL(5,2),
    consensus_sequence TEXT,
    completed_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS alignments.alignment_traces (
    alignment_id UUID NOT NULL,
    trace_id UUID NOT NULL,
    sequence_order INTEGER NOT NULL,
    PRIMARY KEY (alignment_id, trace_id)
);
"""
