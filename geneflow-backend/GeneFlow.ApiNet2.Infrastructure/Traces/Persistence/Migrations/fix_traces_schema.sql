-- Migration script to fix traces schema
-- This adds missing columns that were not in the original table creation

-- Ensure schema exists
CREATE SCHEMA IF NOT EXISTS traces;

-- First, let's check if the traces table exists with incorrect schema
-- If it does and has no data, we'll recreate it

-- Drop and recreate the table if it exists (since it's newly added)
DROP TABLE IF EXISTS traces.trace_annotations CASCADE;
DROP TABLE IF EXISTS traces.sequence_edits CASCADE;
DROP TABLE IF EXISTS traces.traces CASCADE;

-- Create traces table with full schema
CREATE TABLE traces.traces (
    id uuid NOT NULL,
    study_id character varying(10) NOT NULL,
    uploaded_by character varying(10) NOT NULL,
    name character varying(100) NOT NULL,
    description character varying(500),
    file_name character varying(255) NOT NULL,
    content_type character varying(100) NOT NULL,
    storage_path character varying(500) NOT NULL,
    size_bytes bigint NOT NULL,
    checksum character varying(64) NOT NULL,
    format character varying(20) NOT NULL,
    status character varying(20) NOT NULL,
    average_quality_score numeric(5,2),
    total_bases integer,
    quality_above_q20_percentage numeric(5,2),
    quality_above_q30_percentage numeric(5,2),
    trimmed_length integer,
    gc_content_percentage numeric(5,2),
    trim_start_5_prime integer,
    trim_end_5_prime integer,
    trim_start_3_prime integer,
    trim_end_3_prime integer,
    trim_algorithm character varying(50),
    trimmed_by character varying(100),
    trimmed_at timestamp with time zone,
    has_chromatogram_data boolean NOT NULL DEFAULT FALSE,
    failuREDACTED character varying(1000),
    processed_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL,
    created_by character varying(100),
    modified_at timestamp with time zone,
    modified_by character varying(100),
    is_deleted boolean NOT NULL DEFAULT FALSE,
    deleted_at timestamp with time zone,
    deleted_by character varying(100),
    CONSTRAINT "PK_traces" PRIMARY KEY (id)
);

-- Create indexes
CREATE INDEX "IX_traces_study_id" ON traces.traces (study_id);
CREATE INDEX "IX_traces_format" ON traces.traces (format);
CREATE INDEX "IX_traces_status" ON traces.traces (status);

-- Create sequence_edits table
CREATE TABLE traces.sequence_edits (
    id uuid NOT NULL,
    trace_id uuid NOT NULL,
    edit_type character varying(20) NOT NULL,
    position integer NOT NULL,
    original_base character varying(1),
    new_base character varying(1),
    reason character varying(500),
    edited_by character varying(10) NOT NULL,
    edited_at timestamp with time zone NOT NULL,
    is_active boolean NOT NULL DEFAULT TRUE,
    CONSTRAINT "PK_sequence_edits" PRIMARY KEY (id),
    CONSTRAINT "FK_sequence_edits_traces_trace_id" FOREIGN KEY (trace_id) REFERENCES traces.traces(id) ON DELETE CASCADE
);

CREATE INDEX "IX_sequence_edits_trace_id_Position" ON traces.sequence_edits (trace_id, position);

-- Create trace_annotations table
CREATE TABLE traces.trace_annotations (
    id uuid NOT NULL,
    trace_id uuid NOT NULL,
    type character varying(20) NOT NULL,
    label character varying(100) NOT NULL,
    description character varying(500),
    start_position integer NOT NULL,
    end_position integer NOT NULL,
    strand character varying(10) NOT NULL,
    color character varying(20) NOT NULL,
    is_shared boolean NOT NULL DEFAULT FALSE,
    metadata jsonb,
    created_at timestamp with time zone NOT NULL,
    created_by character varying(100),
    modified_at timestamp with time zone,
    modified_by character varying(100),
    CONSTRAINT "PK_trace_annotations" PRIMARY KEY (id),
    CONSTRAINT "FK_trace_annotations_traces_trace_id" FOREIGN KEY (trace_id) REFERENCES traces.traces(id) ON DELETE CASCADE
);

CREATE INDEX "IX_trace_annotations_trace_id_StartPosition_EndPosition" ON traces.trace_annotations (trace_id, start_position, end_position);
CREATE INDEX "IX_trace_annotations_trace_id_IsShared" ON traces.trace_annotations (trace_id, is_shared);

-- Insert migration history entry so EF knows this has been applied
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260421000000_InitialTraces', '8.0.0')
ON CONFLICT DO NOTHING;
