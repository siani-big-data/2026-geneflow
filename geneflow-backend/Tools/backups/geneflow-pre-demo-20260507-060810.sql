--
-- PostgreSQL database dump
--

\restrict oUJzyqhmB8QHp17rvbnpV6nFp3zDoNgHAbbEVHtf9kjbTRqhcmZ6O8jfzSmzq9s

-- Dumped from database version 16.13
-- Dumped by pg_dump version 16.13

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: alignments; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA alignments;


--
-- Name: billing; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA billing;


--
-- Name: identity; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA identity;


--
-- Name: payments; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA payments;


--
-- Name: pipelines; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA pipelines;


--
-- Name: plans; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA plans;


--
-- Name: profiles; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA profiles;


--
-- Name: studies; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA studies;


--
-- Name: subscriptions; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA subscriptions;


--
-- Name: traces; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA traces;


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: alignment_traces; Type: TABLE; Schema: alignments; Owner: -
--

CREATE TABLE alignments.alignment_traces (
    "Id" integer NOT NULL,
    alignment_id uuid NOT NULL,
    trace_id uuid NOT NULL
);


--
-- Name: alignment_traces_Id_seq; Type: SEQUENCE; Schema: alignments; Owner: -
--

CREATE SEQUENCE alignments."alignment_traces_Id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: alignment_traces_Id_seq; Type: SEQUENCE OWNED BY; Schema: alignments; Owner: -
--

ALTER SEQUENCE alignments."alignment_traces_Id_seq" OWNED BY alignments.alignment_traces."Id";


--
-- Name: alignments; Type: TABLE; Schema: alignments; Owner: -
--

CREATE TABLE alignments.alignments (
    id uuid NOT NULL,
    study_id character varying(10) NOT NULL,
    name character varying(200) NOT NULL,
    type_id integer NOT NULL,
    status_id integer DEFAULT 1 NOT NULL,
    gap_open_penalty integer DEFAULT 10 NOT NULL,
    gap_extend_penalty integer DEFAULT 1 NOT NULL,
    generate_consensus boolean DEFAULT true NOT NULL,
    detect_variants boolean DEFAULT true NOT NULL,
    min_quality_for_consensus integer DEFAULT 20 NOT NULL,
    min_variant_frequency numeric(5,4) DEFAULT 0.2 NOT NULL,
    alignment_length integer,
    identity_percentage numeric(5,2),
    gaps_count integer,
    mismatch_count integer,
    match_count integer,
    average_consensus_quality numeric(5,2),
    result_path character varying(500),
    consensus_sequence text,
    variant_count integer,
    failuREDACTED character varying(1000),
    completed_at timestamp without time zone,
    created_at timestamp without time zone NOT NULL,
    created_by character varying(256),
    modified_at timestamp without time zone,
    modified_by character varying(256)
);


--
-- Name: payment_methods; Type: TABLE; Schema: billing; Owner: -
--

CREATE TABLE billing.payment_methods (
    id character varying(9) NOT NULL,
    user_id character varying(9) NOT NULL,
    stripe_payment_method_id character varying(255) NOT NULL,
    brand character varying(50) NOT NULL,
    last4 character varying(4) NOT NULL,
    expiry_month integer NOT NULL,
    expiry_year integer NOT NULL,
    is_default boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone NOT NULL,
    modified_at timestamp with time zone
);


--
-- Name: plan_features; Type: TABLE; Schema: billing; Owner: -
--

CREATE TABLE billing.plan_features (
    id integer NOT NULL,
    plan_id character varying(9) NOT NULL,
    featuREDACTED integer NOT NULL,
    featuREDACTED character varying(50)
);


--
-- Name: plan_features_id_seq; Type: SEQUENCE; Schema: billing; Owner: -
--

CREATE SEQUENCE billing.plan_features_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: plan_features_id_seq; Type: SEQUENCE OWNED BY; Schema: billing; Owner: -
--

ALTER SEQUENCE billing.plan_features_id_seq OWNED BY billing.plan_features.id;


--
-- Name: plans; Type: TABLE; Schema: billing; Owner: -
--

CREATE TABLE billing.plans (
    id character varying(9) NOT NULL,
    name character varying(100) NOT NULL,
    description character varying(500),
    monthly_price numeric(10,2) NOT NULL,
    annual_price numeric(10,2) NOT NULL,
    currency character varying(3) DEFAULT 'USD'::character varying NOT NULL,
    max_studies integer NOT NULL,
    max_traces_per_month integer NOT NULL,
    max_members_per_study integer NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    is_default boolean DEFAULT false NOT NULL,
    display_order integer DEFAULT 0 NOT NULL,
    created_at timestamp without time zone NOT NULL,
    created_by character varying(100),
    modified_at timestamp without time zone,
    modified_by character varying(100)
);


--
-- Name: subscriptions; Type: TABLE; Schema: billing; Owner: -
--

CREATE TABLE billing.subscriptions (
    id character varying(9) NOT NULL,
    user_id character varying(9) NOT NULL,
    plan_id character varying(9) NOT NULL,
    plan_name character varying(50) NOT NULL,
    status integer NOT NULL,
    billing_cycle integer NOT NULL,
    period_start timestamp without time zone NOT NULL,
    period_end timestamp without time zone NOT NULL,
    auto_renew boolean DEFAULT true NOT NULL,
    trial_end_date timestamp without time zone,
    cancelled_at timestamp without time zone,
    cancellation_reason character varying(500),
    created_at timestamp without time zone NOT NULL,
    modified_at timestamp without time zone
);


--
-- Name: external_logins; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.external_logins (
    id uuid NOT NULL,
    provider character varying(50) NOT NULL,
    provider_key character varying(256) NOT NULL,
    provider_display_name character varying(256),
    linked_at timestamp with time zone NOT NULL,
    "UserId" character varying(10) NOT NULL
);


--
-- Name: pipeline_executions; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.pipeline_executions (
    id character varying(10) NOT NULL,
    pipeline_id character varying(10) NOT NULL,
    trace_id character varying(10) NOT NULL,
    started_by character varying(10) NOT NULL,
    status character varying(20) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    started_at timestamp with time zone,
    completed_at timestamp with time zone,
    error_message character varying(2000),
    total_steps integer NOT NULL,
    completed_steps integer DEFAULT 0 NOT NULL
);


--
-- Name: pipeline_step_executions; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.pipeline_step_executions (
    id uuid NOT NULL,
    pipeline_step_id uuid NOT NULL,
    "order" integer NOT NULL,
    step_type character varying(30) NOT NULL,
    status character varying(20) NOT NULL,
    started_at timestamp with time zone,
    completed_at timestamp with time zone,
    error_message character varying(2000),
    result_summary character varying(4000),
    result_data jsonb,
    execution_id character varying(10)
);


--
-- Name: pipeline_steps; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.pipeline_steps (
    id uuid NOT NULL,
    step_type character varying(30) NOT NULL,
    "order" integer NOT NULL,
    label character varying(100),
    configuration character varying(4000) DEFAULT '{}'::character varying NOT NULL,
    is_enabled boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone NOT NULL,
    pipeline_id character varying(10)
);


--
-- Name: pipelines; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.pipelines (
    id character varying(10) NOT NULL,
    study_id character varying(10) NOT NULL,
    owner_id character varying(10) NOT NULL,
    name character varying(100) NOT NULL,
    description character varying(1000),
    status character varying(20) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by character varying(50),
    modified_at timestamp with time zone,
    modified_by character varying(50),
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text
);


--
-- Name: plan_features; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.plan_features (
    id integer NOT NULL,
    plan_id character varying(9) NOT NULL,
    featuREDACTED integer NOT NULL,
    featuREDACTED character varying(50) NOT NULL
);


--
-- Name: plan_features_id_seq; Type: SEQUENCE; Schema: identity; Owner: -
--

ALTER TABLE identity.plan_features ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME identity.plan_features_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: plans; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.plans (
    id character varying(9) NOT NULL,
    name character varying(50) NOT NULL,
    description character varying(500),
    monthly_price numeric(10,2) NOT NULL,
    annual_price numeric(10,2) NOT NULL,
    currency character varying(3) NOT NULL,
    max_studies integer NOT NULL,
    max_traces_per_month integer NOT NULL,
    max_members_per_study integer NOT NULL,
    is_active boolean NOT NULL,
    is_default boolean NOT NULL,
    display_order integer NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by character varying(100),
    modified_at timestamp with time zone,
    modified_by character varying(100)
);


--
-- Name: profiles; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.profiles (
    id character varying(10) NOT NULL,
    user_id character varying(10) NOT NULL,
    first_name character varying(100) NOT NULL,
    last_name character varying(100),
    bio character varying(500),
    location character varying(200),
    professional_role character varying(100),
    institution_name character varying(200),
    institution_department character varying(200),
    research_field character varying(50),
    orcid_id character varying(19),
    website character varying(500),
    photo_url character varying(1000),
    photo_thumbnail_url character varying(1000),
    photo_size_bytes bigint,
    created_at timestamp with time zone NOT NULL,
    created_by character varying(100),
    modified_at timestamp with time zone,
    modified_by character varying(100),
    is_deleted boolean NOT NULL,
    deleted_at timestamp with time zone,
    deleted_by character varying(100)
);


--
-- Name: refresh_tokens; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.refresh_tokens (
    token character varying(256) NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    created_at timestamp with time zone NOT NULL,
    is_revoked boolean NOT NULL,
    revoked_at timestamp with time zone,
    replaced_by_token character varying(256),
    "UserId" character varying(10) NOT NULL
);


--
-- Name: sequence_edits; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.sequence_edits (
    id uuid NOT NULL,
    edit_type character varying(20) NOT NULL,
    "position" integer NOT NULL,
    original_base character(1),
    new_base character(1),
    reason character varying(500),
    edited_by character varying(10) NOT NULL,
    edited_at timestamp with time zone NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    trace_id uuid
);


--
-- Name: studies; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.studies (
    id character varying(10) NOT NULL,
    owner_id character varying(10) NOT NULL,
    title character varying(200) NOT NULL,
    description character varying(5000),
    research_field character varying(50) NOT NULL,
    status character varying(20) NOT NULL,
    allow_public_comments boolean DEFAULT true NOT NULL,
    allow_data_download boolean DEFAULT false NOT NULL,
    requiREDACTED boolean DEFAULT true NOT NULL,
    views_count integer DEFAULT 0 NOT NULL,
    stars_count integer DEFAULT 0 NOT NULL,
    institution character varying(200),
    principal_investigator character varying(200),
    is_featured boolean DEFAULT false NOT NULL,
    tags text[] NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by character varying(100),
    modified_at timestamp with time zone,
    modified_by character varying(100),
    is_deleted boolean NOT NULL,
    deleted_at timestamp with time zone,
    deleted_by character varying(100)
);


--
-- Name: study_invitations; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.study_invitations (
    id character varying(10) NOT NULL,
    study_id character varying(10) NOT NULL,
    email character varying(320) NOT NULL,
    role character varying(20) NOT NULL,
    status character varying(20) NOT NULL,
    token character varying(64) NOT NULL,
    invited_by character varying(10) NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    responded_at timestamp with time zone,
    message character varying(500),
    created_at timestamp with time zone NOT NULL,
    created_by character varying(100),
    modified_at timestamp with time zone,
    modified_by character varying(100)
);


--
-- Name: study_members; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.study_members (
    id uuid NOT NULL,
    user_id character varying(10) NOT NULL,
    role character varying(20) NOT NULL,
    joined_at timestamp with time zone NOT NULL,
    invited_by character varying(10),
    study_id character varying(10)
);


--
-- Name: study_papers; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.study_papers (
    id character varying(10) NOT NULL,
    title character varying(500) NOT NULL,
    authors character varying(1000),
    doi character varying(100),
    abstract character varying(5000),
    journal character varying(200),
    publication_year integer,
    file_id character varying(100),
    file_name character varying(255),
    file_size_bytes bigint,
    study_id character varying(10),
    created_at timestamp with time zone NOT NULL,
    created_by character varying(100),
    modified_at timestamp with time zone,
    modified_by character varying(100),
    is_deleted boolean DEFAULT false NOT NULL,
    deleted_at timestamp with time zone,
    deleted_by character varying(100)
);


--
-- Name: study_stars; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.study_stars (
    id uuid NOT NULL,
    study_id character varying(10) NOT NULL,
    user_id character varying(10) NOT NULL,
    starred_at timestamp with time zone NOT NULL
);


--
-- Name: study_views; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.study_views (
    id bigint NOT NULL,
    study_id character varying(10) NOT NULL,
    user_id character varying(10),
    ip_hash character varying(64),
    user_agent character varying(500),
    viewed_at timestamp with time zone NOT NULL
);


--
-- Name: study_views_id_seq; Type: SEQUENCE; Schema: identity; Owner: -
--

ALTER TABLE identity.study_views ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME identity.study_views_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: subscriptions; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.subscriptions (
    id character varying(9) NOT NULL,
    user_id character varying(9) NOT NULL,
    plan_id character varying(9) NOT NULL,
    plan_name character varying(50) NOT NULL,
    status integer NOT NULL,
    billing_cycle integer NOT NULL,
    period_start timestamp with time zone NOT NULL,
    period_end timestamp with time zone NOT NULL,
    auto_renew boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    modified_at timestamp with time zone,
    cancelled_at timestamp with time zone,
    cancellation_reason character varying(500),
    trial_end_date timestamp with time zone
);


--
-- Name: trace_annotations; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.trace_annotations (
    id uuid NOT NULL,
    type character varying(20) NOT NULL,
    label character varying(100) NOT NULL,
    description character varying(500),
    start_position integer NOT NULL,
    end_position integer NOT NULL,
    strand character varying(10) NOT NULL,
    color character varying(20) NOT NULL,
    is_shared boolean DEFAULT false NOT NULL,
    metadata jsonb,
    trace_id uuid,
    created_at timestamp with time zone NOT NULL,
    created_by character varying(100),
    modified_at timestamp with time zone,
    modified_by character varying(100)
);


--
-- Name: traces; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.traces (
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
    has_chromatogram_data boolean DEFAULT false NOT NULL,
    failuREDACTED character varying(1000),
    processed_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL,
    created_by character varying(100),
    modified_at timestamp with time zone,
    modified_by character varying(100),
    is_deleted boolean DEFAULT false NOT NULL,
    deleted_at timestamp with time zone,
    deleted_by character varying(100)
);


--
-- Name: two_factor_codes; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.two_factor_codes (
    id uuid NOT NULL,
    code character varying(6) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    is_used boolean NOT NULL,
    used_at timestamp with time zone,
    "UserId" character varying(10)
);


--
-- Name: users; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.users (
    id character varying(10) NOT NULL,
    email character varying(256) NOT NULL,
    username character varying(50) NOT NULL,
    password_hash character varying(256) NOT NULL,
    is_active boolean NOT NULL,
    email_verified boolean NOT NULL,
    email_verification_token character varying(128),
    email_verification_token_expiry timestamp with time zone,
    password_reset_token character varying(128),
    password_reset_token_expiry timestamp with time zone,
    failed_login_attempts integer NOT NULL,
    lockout_end timestamp with time zone,
    two_factor_enabled boolean NOT NULL,
    totp_secret character varying(512),
    totp_secret_created_at timestamp with time zone,
    roles jsonb NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by character varying(100),
    modified_at timestamp with time zone,
    modified_by character varying(100),
    is_deleted boolean NOT NULL,
    deleted_at timestamp with time zone,
    deleted_by character varying(100)
);


--
-- Name: payment_events_log; Type: TABLE; Schema: payments; Owner: -
--

CREATE TABLE payments.payment_events_log (
    id integer NOT NULL,
    payment_method_id uuid,
    user_id uuid NOT NULL,
    event_type character varying(100) NOT NULL,
    event_data jsonb,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: payment_events_log_id_seq; Type: SEQUENCE; Schema: payments; Owner: -
--

CREATE SEQUENCE payments.payment_events_log_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: payment_events_log_id_seq; Type: SEQUENCE OWNED BY; Schema: payments; Owner: -
--

ALTER SEQUENCE payments.payment_events_log_id_seq OWNED BY payments.payment_events_log.id;


--
-- Name: payment_methods; Type: TABLE; Schema: payments; Owner: -
--

CREATE TABLE payments.payment_methods (
    id uuid NOT NULL,
    user_id uuid NOT NULL,
    stripe_customer_id character varying(255) NOT NULL,
    stripe_payment_method_id character varying(255) NOT NULL,
    card_last4 character(4) NOT NULL,
    card_brand integer NOT NULL,
    card_exp_month integer NOT NULL,
    card_exp_year integer NOT NULL,
    billing_country character(2),
    billing_postal_code character varying(20),
    status integer DEFAULT 1 NOT NULL,
    is_default boolean DEFAULT false NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    modified_at timestamp without time zone
);


--
-- Name: stripe_customers; Type: TABLE; Schema: payments; Owner: -
--

CREATE TABLE payments.stripe_customers (
    user_id uuid NOT NULL,
    stripe_customer_id character varying(255) NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: pipeline_executions; Type: TABLE; Schema: pipelines; Owner: -
--

CREATE TABLE pipelines.pipeline_executions (
    id character varying(10) NOT NULL,
    pipeline_id character varying(10) NOT NULL,
    trace_id uuid NOT NULL,
    started_by character varying(10) NOT NULL,
    status character varying(20) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    started_at timestamp with time zone,
    completed_at timestamp with time zone,
    error_message character varying(2000),
    total_steps integer NOT NULL,
    completed_steps integer DEFAULT 0 NOT NULL
);


--
-- Name: pipeline_step_executions; Type: TABLE; Schema: pipelines; Owner: -
--

CREATE TABLE pipelines.pipeline_step_executions (
    id uuid NOT NULL,
    pipeline_step_id uuid NOT NULL,
    "order" integer NOT NULL,
    step_type character varying(30) NOT NULL,
    status character varying(20) NOT NULL,
    started_at timestamp with time zone,
    completed_at timestamp with time zone,
    error_message character varying(2000),
    result_summary character varying(4000),
    result_data jsonb,
    execution_id character varying(10)
);


--
-- Name: pipeline_steps; Type: TABLE; Schema: pipelines; Owner: -
--

CREATE TABLE pipelines.pipeline_steps (
    id uuid NOT NULL,
    step_type character varying(30) NOT NULL,
    "order" integer NOT NULL,
    label character varying(100),
    configuration character varying(4000) DEFAULT '{}'::character varying NOT NULL,
    is_enabled boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone NOT NULL,
    pipeline_id character varying(10)
);


--
-- Name: pipelines; Type: TABLE; Schema: pipelines; Owner: -
--

CREATE TABLE pipelines.pipelines (
    id character varying(10) NOT NULL,
    study_id character varying(10) NOT NULL,
    owner_id character varying(10) NOT NULL,
    name character varying(100) NOT NULL,
    description character varying(1000),
    status character varying(20) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by character varying(50),
    modified_at timestamp with time zone,
    modified_by character varying(50),
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text
);


--
-- Name: plan_features; Type: TABLE; Schema: plans; Owner: -
--

CREATE TABLE plans.plan_features (
    id integer NOT NULL,
    plan_id character varying(9) NOT NULL,
    featuREDACTED integer NOT NULL,
    featuREDACTED character varying(50) NOT NULL
);


--
-- Name: plan_features_id_seq; Type: SEQUENCE; Schema: plans; Owner: -
--

ALTER TABLE plans.plan_features ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME plans.plan_features_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: plans; Type: TABLE; Schema: plans; Owner: -
--

CREATE TABLE plans.plans (
    id character varying(9) NOT NULL,
    name character varying(50) NOT NULL,
    description character varying(500),
    monthly_price numeric(10,2) NOT NULL,
    annual_price numeric(10,2) NOT NULL,
    currency character varying(3) NOT NULL,
    max_studies integer NOT NULL,
    max_traces_per_month integer NOT NULL,
    max_members_per_study integer NOT NULL,
    is_active boolean NOT NULL,
    is_default boolean NOT NULL,
    display_order integer NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by character varying(100),
    modified_at timestamp with time zone,
    modified_by character varying(100)
);


--
-- Name: profiles; Type: TABLE; Schema: profiles; Owner: -
--

CREATE TABLE profiles.profiles (
    id character varying(10) NOT NULL,
    user_id character varying(10) NOT NULL,
    first_name character varying(100) NOT NULL,
    last_name character varying(100),
    bio character varying(500),
    location character varying(200),
    professional_role character varying(100),
    institution_name character varying(200),
    institution_department character varying(200),
    research_field character varying(50),
    orcid_id character varying(19),
    website character varying(500),
    photo_url character varying(1000),
    photo_thumbnail_url character varying(1000),
    photo_size_bytes bigint,
    created_at timestamp with time zone NOT NULL,
    created_by character varying(100),
    modified_at timestamp with time zone,
    modified_by character varying(100),
    is_deleted boolean NOT NULL,
    deleted_at timestamp with time zone,
    deleted_by character varying(100)
);


--
-- Name: __EFMigrationsHistory; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


--
-- Name: invitations; Type: TABLE; Schema: studies; Owner: -
--

CREATE TABLE studies.invitations (
    id uuid NOT NULL,
    study_id uuid NOT NULL,
    email character varying(255) NOT NULL,
    token character varying(255) NOT NULL,
    invited_by uuid NOT NULL,
    expires_at timestamp without time zone NOT NULL,
    accepted_at timestamp without time zone
);


--
-- Name: members; Type: TABLE; Schema: studies; Owner: -
--

CREATE TABLE studies.members (
    study_id uuid NOT NULL,
    user_id uuid NOT NULL,
    role_id integer NOT NULL,
    invited_by uuid
);


--
-- Name: studies; Type: TABLE; Schema: studies; Owner: -
--

CREATE TABLE studies.studies (
    id character varying(10) NOT NULL,
    owner_id character varying(10) NOT NULL,
    title character varying(200) NOT NULL,
    description character varying(5000),
    research_field character varying(50) NOT NULL,
    status character varying(20) DEFAULT 'Draft'::character varying NOT NULL,
    allow_public_comments boolean DEFAULT true,
    allow_data_download boolean DEFAULT false,
    requiREDACTED boolean DEFAULT true,
    views_count integer DEFAULT 0,
    stars_count integer DEFAULT 0,
    institution character varying(200),
    principal_investigator character varying(200),
    is_featured boolean DEFAULT false,
    tags text[],
    created_at timestamp without time zone NOT NULL,
    created_by character varying(100),
    modified_at timestamp without time zone,
    modified_by character varying(100),
    is_deleted boolean DEFAULT false NOT NULL,
    deleted_at timestamp without time zone,
    deleted_by character varying(100)
);


--
-- Name: study_invitations; Type: TABLE; Schema: studies; Owner: -
--

CREATE TABLE studies.study_invitations (
    id character varying(10) NOT NULL,
    study_id character varying(10) NOT NULL,
    email character varying(320) NOT NULL,
    role character varying(20) NOT NULL,
    status character varying(20) DEFAULT 'Pending'::character varying NOT NULL,
    token character varying(64) NOT NULL,
    invited_by character varying(10) NOT NULL,
    expires_at timestamp without time zone NOT NULL,
    responded_at timestamp without time zone,
    message character varying(500),
    created_at timestamp without time zone NOT NULL,
    created_by character varying(100),
    modified_at timestamp without time zone,
    modified_by character varying(100)
);


--
-- Name: study_members; Type: TABLE; Schema: studies; Owner: -
--

CREATE TABLE studies.study_members (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    study_id character varying(10) NOT NULL,
    user_id character varying(10) NOT NULL,
    role character varying(20) NOT NULL,
    joined_at timestamp without time zone NOT NULL,
    invited_by character varying(10)
);


--
-- Name: study_papers; Type: TABLE; Schema: studies; Owner: -
--

CREATE TABLE studies.study_papers (
    id character varying(10) NOT NULL,
    study_id character varying(10) NOT NULL,
    title character varying(500) NOT NULL,
    authors character varying(1000),
    doi character varying(100),
    abstract character varying(5000),
    journal character varying(200),
    publication_year integer,
    file_id character varying(100),
    file_name character varying(255),
    file_size_bytes bigint,
    created_at timestamp without time zone NOT NULL,
    created_by character varying(100),
    modified_at timestamp without time zone,
    modified_by character varying(100),
    is_deleted boolean DEFAULT false,
    deleted_at timestamp without time zone,
    deleted_by character varying(100)
);


--
-- Name: study_stars; Type: TABLE; Schema: studies; Owner: -
--

CREATE TABLE studies.study_stars (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    study_id character varying(10) NOT NULL,
    user_id character varying(10) NOT NULL,
    starred_at timestamp without time zone NOT NULL
);


--
-- Name: study_views; Type: TABLE; Schema: studies; Owner: -
--

CREATE TABLE studies.study_views (
    id bigint NOT NULL,
    study_id character varying(10) NOT NULL,
    user_id character varying(10),
    ip_hash character varying(64),
    user_agent character varying(500),
    viewed_at timestamp without time zone NOT NULL
);


--
-- Name: study_views_id_seq; Type: SEQUENCE; Schema: studies; Owner: -
--

CREATE SEQUENCE studies.study_views_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: study_views_id_seq; Type: SEQUENCE OWNED BY; Schema: studies; Owner: -
--

ALTER SEQUENCE studies.study_views_id_seq OWNED BY studies.study_views.id;


--
-- Name: subscriptions; Type: TABLE; Schema: subscriptions; Owner: -
--

CREATE TABLE subscriptions.subscriptions (
    id character varying(9) NOT NULL,
    user_id character varying(9) NOT NULL,
    plan_id character varying(9) NOT NULL,
    plan_name character varying(50) NOT NULL,
    status integer NOT NULL,
    billing_cycle integer NOT NULL,
    period_start timestamp with time zone NOT NULL,
    period_end timestamp with time zone NOT NULL,
    auto_renew boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    modified_at timestamp with time zone,
    cancelled_at timestamp with time zone,
    cancellation_reason character varying(500),
    trial_end_date timestamp with time zone
);


--
-- Name: annotations; Type: TABLE; Schema: traces; Owner: -
--

CREATE TABLE traces.annotations (
    id uuid NOT NULL,
    trace_id uuid NOT NULL,
    type_id integer NOT NULL,
    label character varying(255),
    start_position integer NOT NULL,
    end_position integer NOT NULL,
    created_by uuid NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


--
-- Name: sequence_edits; Type: TABLE; Schema: traces; Owner: -
--

CREATE TABLE traces.sequence_edits (
    id uuid NOT NULL,
    trace_id uuid NOT NULL,
    edit_type character varying(20) NOT NULL,
    "position" integer NOT NULL,
    original_base character varying(1),
    new_base character varying(1),
    reason character varying(500),
    edited_by character varying(10) NOT NULL,
    edited_at timestamp with time zone NOT NULL,
    is_active boolean DEFAULT true NOT NULL
);


--
-- Name: trace_annotations; Type: TABLE; Schema: traces; Owner: -
--

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
    is_shared boolean DEFAULT false NOT NULL,
    metadata jsonb,
    created_at timestamp with time zone NOT NULL,
    created_by character varying(100),
    modified_at timestamp with time zone,
    modified_by character varying(100)
);


--
-- Name: trace_trims; Type: TABLE; Schema: traces; Owner: -
--

CREATE TABLE traces.trace_trims (
    id uuid NOT NULL,
    trim_type character varying(20) NOT NULL,
    trim_end character varying(20) NOT NULL,
    start_position integer NOT NULL,
    end_position integer NOT NULL,
    algorithm character varying(50) NOT NULL,
    reason character varying(500),
    applied_by character varying(10) NOT NULL,
    applied_at timestamp with time zone NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    trace_id uuid NOT NULL
);


--
-- Name: traces; Type: TABLE; Schema: traces; Owner: -
--

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
    has_chromatogram_data boolean DEFAULT false NOT NULL,
    failuREDACTED character varying(1000),
    processed_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL,
    created_by character varying(100),
    modified_at timestamp with time zone,
    modified_by character varying(100),
    is_deleted boolean DEFAULT false NOT NULL,
    deleted_at timestamp with time zone,
    deleted_by character varying(100)
);


--
-- Name: alignment_traces Id; Type: DEFAULT; Schema: alignments; Owner: -
--

ALTER TABLE ONLY alignments.alignment_traces ALTER COLUMN "Id" SET DEFAULT nextval('alignments."alignment_traces_Id_seq"'::regclass);


--
-- Name: plan_features id; Type: DEFAULT; Schema: billing; Owner: -
--

ALTER TABLE ONLY billing.plan_features ALTER COLUMN id SET DEFAULT nextval('billing.plan_features_id_seq'::regclass);


--
-- Name: payment_events_log id; Type: DEFAULT; Schema: payments; Owner: -
--

ALTER TABLE ONLY payments.payment_events_log ALTER COLUMN id SET DEFAULT nextval('payments.payment_events_log_id_seq'::regclass);


--
-- Name: study_views id; Type: DEFAULT; Schema: studies; Owner: -
--

ALTER TABLE ONLY studies.study_views ALTER COLUMN id SET DEFAULT nextval('studies.study_views_id_seq'::regclass);


--
-- Data for Name: alignment_traces; Type: TABLE DATA; Schema: alignments; Owner: -
--

COPY alignments.alignment_traces ("Id", alignment_id, trace_id) FROM stdin;
\.


--
-- Data for Name: alignments; Type: TABLE DATA; Schema: alignments; Owner: -
--

COPY alignments.alignments (id, study_id, name, type_id, status_id, gap_open_penalty, gap_extend_penalty, generate_consensus, detect_variants, min_quality_for_consensus, min_variant_frequency, alignment_length, identity_percentage, gaps_count, mismatch_count, match_count, average_consensus_quality, result_path, consensus_sequence, variant_count, failuREDACTED, completed_at, created_at, created_by, modified_at, modified_by) FROM stdin;
\.


--
-- Data for Name: payment_methods; Type: TABLE DATA; Schema: billing; Owner: -
--

COPY billing.payment_methods (id, user_id, stripe_payment_method_id, brand, last4, expiry_month, expiry_year, is_default, created_at, modified_at) FROM stdin;
M00000002	U00000004	pm_1TOPdZDy1tq9x6ghEautNFsq	visa	4242	8	2029	t	2026-04-20 21:45:51.928605+00	\N
\.


--
-- Data for Name: plan_features; Type: TABLE DATA; Schema: billing; Owner: -
--

COPY billing.plan_features (id, plan_id, featuREDACTED, featuREDACTED) FROM stdin;
\.


--
-- Data for Name: plans; Type: TABLE DATA; Schema: billing; Owner: -
--

COPY billing.plans (id, name, description, monthly_price, annual_price, currency, max_studies, max_traces_per_month, max_members_per_study, is_active, is_default, display_order, created_at, created_by, modified_at, modified_by) FROM stdin;
\.


--
-- Data for Name: subscriptions; Type: TABLE DATA; Schema: billing; Owner: -
--

COPY billing.subscriptions (id, user_id, plan_id, plan_name, status, billing_cycle, period_start, period_end, auto_renew, trial_end_date, cancelled_at, cancellation_reason, created_at, modified_at) FROM stdin;
\.


--
-- Data for Name: external_logins; Type: TABLE DATA; Schema: identity; Owner: -
--

COPY identity.external_logins (id, provider, provider_key, provider_display_name, linked_at, "UserId") FROM stdin;
cf9b3d4b-4e87-4dfe-9d28-6d647917c962	Google	114513798410671624437	Eduardo Marrero Gonz├ílez	2026-04-19 19:05:08.70334+00	U00000004
b10b4dba-4109-4a76-b293-599a38065833	GitHub	86695680	Eduardo Marrero Gonz├ílez	2026-04-29 19:34:22.953838+00	U00000005
\.


--
-- Data for Name: pipeline_executions; Type: TABLE DATA; Schema: identity; Owner: -
--

COPY identity.pipeline_executions (id, pipeline_id, trace_id, started_by, status, created_at, started_at, completed_at, error_message, total_steps, completed_steps) FROM stdin;
\.


--
-- Data for Name: pipeline_step_executions; Type: TABLE DATA; Schema: identity; Owner: -
--

COPY identity.pipeline_step_executions (id, pipeline_step_id, "order", step_type, status, started_at, completed_at, error_message, result_summary, result_data, execution_id) FROM stdin;
\.


--
-- Data for Name: pipeline_steps; Type: TABLE DATA; Schema: identity; Owner: -
--

COPY identity.pipeline_steps (id, step_type, "order", label, configuration, is_enabled, created_at, pipeline_id) FROM stdin;
\.


--
-- Data for Name: pipelines; Type: TABLE DATA; Schema: identity; Owner: -
--

COPY identity.pipelines (id, study_id, owner_id, name, description, status, created_at, created_by, modified_at, modified_by, "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: plan_features; Type: TABLE DATA; Schema: identity; Owner: -
--

COPY identity.plan_features (id, plan_id, featuREDACTED, featuREDACTED) FROM stdin;
\.


--
-- Data for Name: plans; Type: TABLE DATA; Schema: identity; Owner: -
--

COPY identity.plans (id, name, description, monthly_price, annual_price, currency, max_studies, max_traces_per_month, max_members_per_study, is_active, is_default, display_order, created_at, created_by, modified_at, modified_by) FROM stdin;
\.


--
-- Data for Name: profiles; Type: TABLE DATA; Schema: identity; Owner: -
--

COPY identity.profiles (id, user_id, first_name, last_name, bio, location, professional_role, institution_name, institution_department, research_field, orcid_id, website, photo_url, photo_thumbnail_url, photo_size_bytes, created_at, created_by, modified_at, modified_by, is_deleted, deleted_at, deleted_by) FROM stdin;
\.


--
-- Data for Name: refresh_tokens; Type: TABLE DATA; Schema: identity; Owner: -
--

COPY identity.refresh_tokens (token, expires_at, created_at, is_revoked, revoked_at, replaced_by_token, "UserId") FROM stdin;
jZerssIDSEScb7QwloPULrtg0wx1ETDKEHKZJJzS8qgUufOEy9LTLoSIO_RaxTTWa0kcduRbS5H3ABrkuBDKqg	2026-05-13 21:16:47.087435+00	2026-05-06 21:16:47.088309+00	f	\N	\N	U00000004
sj3dmNOA0F58Yw0q1Qv1ufGyRPdpEkRt9llA3dJhm5BSpH1NxonCHECs9BXInDli7g3pMjViGdlZ_YOgc6Po3A	2026-05-13 22:52:28.016361+00	2026-05-06 22:52:28.016363+00	t	2026-05-06 22:53:13.666173+00	\N	U00000005
XYF2SCYIkBWzqFezFWtv8k1XYVYmdJNtZhC0eRsFU7DFMT_Isy_8I0WfPLJJSc7ZXOy-aM-8xi8f-WHRUEUBUw	2026-05-14 01:22:21.30429+00	2026-05-07 01:22:21.304735+00	f	\N	\N	U00000004
ru2iEOdKwpqSrhCyXY8Dfl7GwWLFu5N3PhzL12_OzyWVWRQ_KC8yepQNVQuZQuTZjge7WUvmOTkKV8PqHgsF5A	2026-05-14 02:18:56.063476+00	2026-05-07 02:18:56.063477+00	f	\N	\N	U00000004
3m8-jH7NGJk5lT2UgLa3nPT7msYDUD9sKFhXaVzyqYdzM3XsDwBqsXW7u1HdM1xsbJa_3hb3VqGyt1iKewI05g	2026-05-14 03:48:40.724763+00	2026-05-07 03:48:40.725324+00	f	\N	\N	U00000004
bttHD1We9xGYF87kAewxQok_uq4WdGO9FdqqcxZkZApSdFs1KPXnk43buUm0jROlA-94Hdu-tIlt8NT40jn8aA	2026-05-11 01:13:01.879321+00	2026-05-04 01:13:01.879739+00	f	\N	\N	U00000030
XA7givXiG-YbkiYwELK49n5P_0K9qNzKkA8l3spl7n_5wTirRvc6L5QiORUlUVxwsSgf_9wQHT6gEEemf-Uoyg	2026-05-11 01:13:17.140314+00	2026-05-04 01:13:17.140317+00	f	\N	\N	U00000031
jgc6MYbu1NG0AbUGPAoPwioLwEmturJ6lBWS8KYyiqbajTSdLiwBQ73edn5nD21u7J61ajw8uZ8gdXdZNWB8LQ	2026-05-11 01:20:17.331785+00	2026-05-04 01:20:17.332222+00	f	\N	\N	U00000032
nwKADZypIaJNKEV762uxrgJuz2WOxZ9Q_SgDRrI0dDhVI2RrMXaV_NcfhxYdsUBLhY1h3pwVTZO6SzAP1457HQ	2026-05-11 01:28:28.487769+00	2026-05-04 01:28:28.488229+00	f	\N	\N	U00000033
pmS__bxGi2GuOjGxzgkjItaSYVxFLIu_w5gafkYJ5wEL9wp9M4wx_7oTo9lWXSJDNPppZV1v1-2mD4_PNIIeBw	2026-05-11 01:34:14.374392+00	2026-05-04 01:34:14.374395+00	f	\N	\N	U00000034
maEM98kxUon9TZwgEcNQ7zkZ1edxXof7o4S46IpnD8mt25T3znv3GdOkMJOXeXyh-nvN6aBIsYr1WSX208trlg	2026-05-11 01:37:59.916739+00	2026-05-04 01:37:59.916741+00	f	\N	\N	U00000035
FjHHz5tro5Edof7FgpH5WhXblb9peKCapM698wrPEvAZ1VztaDf3bULh_b96fs7Mojwb2UjHjMGW5KHM2nZlkg	2026-05-11 04:32:38.506182+00	2026-05-04 04:32:38.506184+00	f	\N	\N	U00000036
qX8a34iM2cFslaLMWZ-q9h-RBWObxJBqQ-vEp597BX_fdi61haNWQ84eu17kxmERdtbMcp99nPxHrqnnINViVg	2026-05-11 04:39:03.854806+00	2026-05-04 04:39:03.854808+00	f	\N	\N	U00000037
sKaH4g-n37B9HZmYvEP2Sc4RdTC1f8wWM6mUsxeAeQiuxKV1DFBYFymRBpuq2B-c_eXT19LWAGAtGyhqxynS7A	2026-05-12 21:21:04.440171+00	2026-05-05 21:21:04.440755+00	f	\N	\N	U00000004
ggLBs2SMv98RMONRLrG9KdyWxDMrLpvIer9WW0n5evi4ugB6xFgNPvLHnLeBEp0I2LkWNnEq2CGvk47GtCAvog	2026-05-12 21:21:47.605646+00	2026-05-05 21:21:47.605649+00	f	\N	\N	U00000004
S3oK4zrjsdfH3K13huL_eOrioY2cdRZ64YrNW7d9vnYReacfEpuhfJPLW3Jij4jhmgk_JankxZf7roAebqBJug	2026-05-13 00:42:47.941206+00	2026-05-06 00:42:47.94169+00	f	\N	\N	U00000004
d_27ehYNmJraeeNwhUk1POAvJpYSCxXWjZjL987uhsM92lQULS-h6FNUru4p5-Lq9J14v0Pnz4Gw4pDQ0pXiyQ	2026-05-13 01:14:41.376888+00	2026-05-06 01:14:41.376893+00	f	\N	\N	U00000004
rszFBAOMQjiPYqj2ffSDkrgZ2V_jxhqpoLiYQKPl657QimLReWSACxQQ8KL40l9io3BpmjbSucJZymQ7HTnspg	2026-05-13 21:34:49.021075+00	2026-05-06 21:34:49.021078+00	f	\N	\N	U00000005
4f26_iPBvkzbEucRurSAOHDo68Z8CGLohjiinZ0tYpeGOeDBLsFL6QfWZfQWYIB3irMg1zkOliDRATcGbzwetg	2026-05-13 20:11:56.385133+00	2026-05-06 20:11:56.385607+00	f	\N	\N	U00000004
hA3jtFA1ZNPsIkYDeGBSHk_Bw4lpW2Qm8ajkqYPAyK6rFzCafX-TaowrhT6d4PJN38A934GChsGSbhu2aQAJQw	2026-05-13 22:53:20.039846+00	2026-05-06 22:53:20.039848+00	f	\N	\N	U00000004
9Dd8iMxf3SX4lI7DhZ65v-42P5C4IXyIkIw9qsQ0aLCD9s0-jRFf-MqwJJRw9oo1gIf_Izv4qhYg0c3DRA9SBw	2026-05-14 00:24:03.253434+00	2026-05-07 00:24:03.253436+00	f	\N	\N	U00000004
vpLJ3MvgjHviuEEgG-90Rlc_lfpkryDSmvyT4_p0qCeOa0gKdH1a1v3cKh47Q7lvTPRJaMj6fumfLktP6lVGOQ	2026-05-14 00:28:03.762959+00	2026-05-07 00:28:03.763416+00	f	\N	\N	U00000004
45BVOma7Nr5omSqp386p-m0bL-EELOFt2CjaV7L3HGEvhbodNxgAA5CCu-4Qn0SgalBO47s738q_JLAXMQswxg	2026-05-14 01:08:04.447772+00	2026-05-07 01:08:04.448478+00	f	\N	\N	U00000004
hGCcktdM4uE9xVT-YGKob_LzKi7znIMp3LdXu0_Jg5Gl6a4XfWMggbl73bb6LHl9-SOUxuwN1R49js1muQXmXQ	2026-05-14 03:01:16.127334+00	2026-05-07 03:01:16.127337+00	f	\N	\N	U00000004
pBU2mHByFXnvGacCok-qOKvPHkVb01oEcp0k4aqqhwMFX2yChc7iqMcp4vQvO5R-5fpM3LyhRpyzGTtnDgmAfw	2026-05-14 03:31:47.201746+00	2026-05-07 03:31:47.202455+00	f	\N	\N	U00000004
766N7lHMh5XBsAEyfEVQohOoEigabxupAGTscwMpXztG24krd-ggNfL0Kr8VlZiqg0Nd0XBv5GCortA-pZeZKA	2026-05-14 03:51:57.507789+00	2026-05-07 03:51:57.508464+00	f	\N	\N	U00000004
\.


--
-- Data for Name: sequence_edits; Type: TABLE DATA; Schema: identity; Owner: -
--

COPY identity.sequence_edits (id, edit_type, "position", original_base, new_base, reason, edited_by, edited_at, is_active, trace_id) FROM stdin;
\.


--
-- Data for Name: studies; Type: TABLE DATA; Schema: identity; Owner: -
--

COPY identity.studies (id, owner_id, title, description, research_field, status, allow_public_comments, allow_data_download, requiREDACTED, views_count, stars_count, institution, principal_investigator, is_featured, tags, created_at, created_by, modified_at, modified_by, is_deleted, deleted_at, deleted_by) FROM stdin;
\.


--
-- Data for Name: study_invitations; Type: TABLE DATA; Schema: identity; Owner: -
--

COPY identity.study_invitations (id, study_id, email, role, status, token, invited_by, expires_at, responded_at, message, created_at, created_by, modified_at, modified_by) FROM stdin;
\.


--
-- Data for Name: study_members; Type: TABLE DATA; Schema: identity; Owner: -
--

COPY identity.study_members (id, user_id, role, joined_at, invited_by, study_id) FROM stdin;
\.


--
-- Data for Name: study_papers; Type: TABLE DATA; Schema: identity; Owner: -
--

COPY identity.study_papers (id, title, authors, doi, abstract, journal, publication_year, file_id, file_name, file_size_bytes, study_id, created_at, created_by, modified_at, modified_by, is_deleted, deleted_at, deleted_by) FROM stdin;
\.


--
-- Data for Name: study_stars; Type: TABLE DATA; Schema: identity; Owner: -
--

COPY identity.study_stars (id, study_id, user_id, starred_at) FROM stdin;
\.


--
-- Data for Name: study_views; Type: TABLE DATA; Schema: identity; Owner: -
--

COPY identity.study_views (id, study_id, user_id, ip_hash, user_agent, viewed_at) FROM stdin;
\.


--
-- Data for Name: subscriptions; Type: TABLE DATA; Schema: identity; Owner: -
--

COPY identity.subscriptions (id, user_id, plan_id, plan_name, status, billing_cycle, period_start, period_end, auto_renew, created_at, modified_at, cancelled_at, cancellation_reason, trial_end_date) FROM stdin;
\.


--
-- Data for Name: trace_annotations; Type: TABLE DATA; Schema: identity; Owner: -
--

COPY identity.trace_annotations (id, type, label, description, start_position, end_position, strand, color, is_shared, metadata, trace_id, created_at, created_by, modified_at, modified_by) FROM stdin;
\.


--
-- Data for Name: traces; Type: TABLE DATA; Schema: identity; Owner: -
--

COPY identity.traces (id, study_id, uploaded_by, name, description, file_name, content_type, storage_path, size_bytes, checksum, format, status, average_quality_score, total_bases, quality_above_q20_percentage, quality_above_q30_percentage, trimmed_length, gc_content_percentage, trim_start_5_prime, trim_end_5_prime, trim_start_3_prime, trim_end_3_prime, trim_algorithm, trimmed_by, trimmed_at, has_chromatogram_data, failuREDACTED, processed_at, created_at, created_by, modified_at, modified_by, is_deleted, deleted_at, deleted_by) FROM stdin;
\.


--
-- Data for Name: two_factor_codes; Type: TABLE DATA; Schema: identity; Owner: -
--

COPY identity.two_factor_codes (id, code, created_at, expires_at, is_used, used_at, "UserId") FROM stdin;
\.


--
-- Data for Name: users; Type: TABLE DATA; Schema: identity; Owner: -
--

COPY identity.users (id, email, username, password_hash, is_active, email_verified, email_verification_token, email_verification_token_expiry, password_reset_token, password_reset_token_expiry, failed_login_attempts, lockout_end, two_factor_enabled, totp_secret, totp_secret_created_at, roles, created_at, created_by, modified_at, modified_by, is_deleted, deleted_at, deleted_by) FROM stdin;
U00000035	e2e_b89e90b5@example.com	e2eb89e90b5	$2a$12$NA5NOkhIpgv4YyCB9XudHOOJCOWGVvSj76K95gHv9EXSfXvArUqXO	t	t	IVXgZLs6B0ypOi2gHrQMXA	2026-05-05 01:37:57.549147+00	\N	\N	0	\N	f	\N	\N	["User"]	2026-05-04 01:37:57.54915+00	\N	2026-05-04 01:37:59.916743+00	\N	f	\N	\N
U00000032	e2e_e34b0835@example.com	e2ee34b0835	$2a$12$XO3lrQQKoVxFRE5fjw4PJO3eUquD7eaQaLz6GgdC4i0CMYRWgDtZu	t	t	24SFi8yYgk6LOfRYthzeGQ	2026-05-05 01:20:14.456574+00	\N	\N	0	\N	f	\N	\N	["User"]	2026-05-04 01:20:14.456995+00	\N	2026-05-04 01:20:17.332393+00	\N	f	\N	\N
U00000033	e2e_93cfeb8e@example.com	e2e93cfeb8e	$2a$12$3DbifJJNJMyvjyUQN56ijORRC/J0yGyMkgPdOiytFsquJhoUI6iKC	t	t	nEAP_m1oLUmFNIEcYEKUiA	2026-05-05 01:28:25.63268+00	\N	\N	0	\N	f	\N	\N	["User"]	2026-05-04 01:28:25.633129+00	\N	2026-05-04 01:28:28.488419+00	\N	f	\N	\N
U00000004	edumarreroglezz@gmail.com	eduardomarrerogonzle	$2a$12$evTAI54TrjuoJuyC4wECEeOEl/O3iI7o5S6tYDh7/WlbeNi18F4ti	t	t	\N	\N	\N	\N	0	\N	f	\N	\N	["User"]	2026-04-19 19:05:08.703339+00	\N	2026-05-07 03:51:57.5088+00	\N	f	\N	\N
U00000029	e2e_2b65cda0@example.com	e2e2b65cda0	$2a$12$Tcwy47GnLn8v/y/OgOvNs.Nm0ghuT0kahg2hfam0i./AwA.N2SjJW	t	f	GLefO6dqi067GhvPjqt_1w	2026-05-05 01:11:37.019817+00	\N	\N	0	\N	f	\N	\N	["User"]	2026-05-04 01:11:37.020267+00	\N	\N	\N	f	\N	\N
U00000009	sarah.chen@marine-lab.edu	sarahchen	$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy	t	t	\N	\N	\N	\N	0	\N	f	\N	\N	["User"]	2026-04-21 22:24:46.855676+00	\N	\N	\N	f	\N	\N
U00000010	michael.green@plantsciences.org	mgreen	$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy	t	t	\N	\N	\N	\N	0	\N	f	\N	\N	["User"]	2026-04-21 22:24:46.855676+00	\N	\N	\N	f	\N	\N
U00000011	emma.wilson@genetics-center.edu	ewilson	$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy	t	t	\N	\N	\N	\N	0	\N	f	\N	\N	["User"]	2026-04-21 22:24:46.855676+00	\N	\N	\N	f	\N	\N
U00000012	james.liu@sequencing.edu	jliu	$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy	t	t	\N	\N	\N	\N	0	\N	f	\N	\N	["User"]	2026-04-21 22:24:46.855676+00	\N	\N	\N	f	\N	\N
U00000013	ana.martinez@cancer-institute.org	amartinez	$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy	t	t	\N	\N	\N	\N	0	\N	f	\N	\N	["User"]	2026-04-21 22:24:46.855676+00	\N	\N	\N	f	\N	\N
U00000014	robert.brown@enviro-lab.edu	rbrown	$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy	t	t	\N	\N	\N	\N	0	\N	f	\N	\N	["User"]	2026-04-21 22:24:46.855676+00	\N	\N	\N	f	\N	\N
U00000008	test-1776646385@example.com	testuser1776646385	$2a$12$tXBosWlRsXE5qk1eUaJGN.c78hGaWnmP37O5tfy3RL77TjOfOc0bO	t	f	UyxtG66_IkShhjkJqoz_nA	2026-04-21 00:53:05.657978+00	\N	\N	0	\N	f	\N	\N	["User"]	2026-04-20 00:53:05.658719+00	\N	\N	\N	f	\N	\N
U00000015	lisa.wang@oceanography.edu	lwang	$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy	t	t	\N	\N	\N	\N	0	\N	f	\N	\N	["User"]	2026-04-21 22:24:46.855676+00	\N	\N	\N	f	\N	\N
U00000016	david.kim@molbio.edu	dkim	$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy	t	t	\N	\N	\N	\N	0	\N	f	\N	\N	["User"]	2026-04-21 22:24:46.855676+00	\N	\N	\N	f	\N	\N
U00000017	jennifer.lee@synbio.org	jlee	$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy	t	t	\N	\N	\N	\N	0	\N	f	\N	\N	["User"]	2026-04-21 22:24:46.855676+00	\N	\N	\N	f	\N	\N
U00000018	thomas.anderson@agri-research.edu	tanderson	$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy	t	t	\N	\N	\N	\N	0	\N	f	\N	\N	["User"]	2026-04-21 22:24:46.855676+00	\N	\N	\N	f	\N	\N
U00000019	maria.garcia@medical-center.org	mgarcia	$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy	t	t	\N	\N	\N	\N	0	\N	f	\N	\N	["User"]	2026-04-21 22:24:46.855676+00	\N	\N	\N	f	\N	\N
U00000020	peter.zhang@botanical.edu	pzhang	$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy	t	t	\N	\N	\N	\N	0	\N	f	\N	\N	["User"]	2026-04-21 22:24:46.855676+00	\N	\N	\N	f	\N	\N
U00000021	susan.park@virology.edu	spark	$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy	t	t	\N	\N	\N	\N	0	\N	f	\N	\N	["User"]	2026-04-21 22:24:46.855676+00	\N	\N	\N	f	\N	\N
U00000022	john.smith@diagnostics.org	jsmith	$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy	t	t	\N	\N	\N	\N	0	\N	f	\N	\N	["User"]	2026-04-21 22:24:46.855676+00	\N	\N	\N	f	\N	\N
U00000023	karen.white@infectious.edu	kwhite	$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy	t	t	\N	\N	\N	\N	0	\N	f	\N	\N	["User"]	2026-04-21 22:24:46.855676+00	\N	\N	\N	f	\N	\N
U00000024	daniel.moore@conservation.org	dmoore	$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy	t	t	\N	\N	\N	\N	0	\N	f	\N	\N	["User"]	2026-04-21 22:24:46.855676+00	\N	\N	\N	f	\N	\N
U00000025	rachel.taylor@enveng.edu	rtaylor	$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy	t	t	\N	\N	\N	\N	0	\N	f	\N	\N	["User"]	2026-04-21 22:24:46.855676+00	\N	\N	\N	f	\N	\N
U00000026	chris.johnson@epigenetics.org	cjohnson	$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy	t	t	\N	\N	\N	\N	0	\N	f	\N	\N	["User"]	2026-04-21 22:24:46.855676+00	\N	\N	\N	f	\N	\N
U00000027	laura.davis@biotech.edu	ldavis	$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy	t	t	\N	\N	\N	\N	0	\N	f	\N	\N	["User"]	2026-04-21 22:24:46.855676+00	\N	\N	\N	f	\N	\N
U00000028	mark.wilson@bioinformatics.edu	mwilson	$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy	t	t	\N	\N	\N	\N	0	\N	f	\N	\N	["User"]	2026-04-21 22:24:46.855676+00	\N	\N	\N	f	\N	\N
U00000030	e2e_f5902469@example.com	e2ef5902469	$2a$12$Yx4bM4cg5LkRDbj/nht8YOXT/7IpKpgwssgGijIeogUnaCri5x34q	t	t	39uJkAt9UkeCy1vuvmc2og	2026-05-05 01:12:59.516083+00	\N	\N	0	\N	f	\N	\N	["User"]	2026-05-04 01:12:59.516086+00	\N	2026-05-04 01:13:01.879921+00	\N	f	\N	\N
U00000005	eduardo.marrero106@alu.ulpgc.es	eduardomarrerogonzle1	OAUTH_NO_PASSWORD	t	t	\N	\N	\N	\N	0	\N	f	\N	\N	["User"]	2026-04-29 19:34:22.953772+00	\N	2026-05-06 22:53:13.666174+00	\N	f	\N	\N
U00000031	e2e_166b7f9b@example.com	e2e166b7f9b	$2a$12$OLqwtBtyLI8rAjqon4rXdOmKfRiqAj5CJBY7SJC9D4GrDUMULKQQ6	t	t	fPMWUCAOjEaNiPqzpZV5-A	2026-05-05 01:13:14.889788+00	\N	\N	0	\N	f	\N	\N	["User"]	2026-05-04 01:13:14.889791+00	\N	2026-05-04 01:13:17.140319+00	\N	f	\N	\N
U00000034	e2e_4f0d90a5@example.com	e2e4f0d90a5	$2a$12$wfCamLEXh0kMcjrX.G2Na.iWfZFaOTHR7P0SWAgG7Cm5N4E.0maPW	t	t	FpPreYHaNEOGBl22FeiRVg	2026-05-05 01:34:12.084803+00	\N	\N	0	\N	f	\N	\N	["User"]	2026-05-04 01:34:12.084807+00	\N	2026-05-04 01:34:14.374398+00	\N	f	\N	\N
U00000036	e2e_cf68ca20@example.com	e2ecf68ca20	$2a$12$KN6adWuoz0YruKju5QxoaOKdZE3lNK.pnrs2701p259K3pRvy7ThW	t	t	D-229X-vnk62PCG0IeKCiw	2026-05-05 04:32:36.143177+00	\N	\N	0	\N	f	\N	\N	["User"]	2026-05-04 04:32:36.14318+00	\N	2026-05-04 04:32:38.506186+00	\N	f	\N	\N
U00000037	e2e_8b6a8eea@example.com	e2e8b6a8eea	$2a$12$MLNL8JPI9MLfJqrHMH5gOO.tBKwsy8ud21s.kBhdHdk5ODGbBw.iq	t	t	GnPNH-WupUCGKX7vBTrK-Q	2026-05-05 04:39:01.412417+00	\N	\N	0	\N	f	\N	\N	["User"]	2026-05-04 04:39:01.41242+00	\N	2026-05-04 04:39:03.85481+00	\N	f	\N	\N
\.


--
-- Data for Name: payment_events_log; Type: TABLE DATA; Schema: payments; Owner: -
--

COPY payments.payment_events_log (id, payment_method_id, user_id, event_type, event_data, created_at) FROM stdin;
\.


--
-- Data for Name: payment_methods; Type: TABLE DATA; Schema: payments; Owner: -
--

COPY payments.payment_methods (id, user_id, stripe_customer_id, stripe_payment_method_id, card_last4, card_brand, card_exp_month, card_exp_year, billing_country, billing_postal_code, status, is_default, created_at, modified_at) FROM stdin;
\.


--
-- Data for Name: stripe_customers; Type: TABLE DATA; Schema: payments; Owner: -
--

COPY payments.stripe_customers (user_id, stripe_customer_id, created_at) FROM stdin;
\.


--
-- Data for Name: pipeline_executions; Type: TABLE DATA; Schema: pipelines; Owner: -
--

COPY pipelines.pipeline_executions (id, pipeline_id, trace_id, started_by, status, created_at, started_at, completed_at, error_message, total_steps, completed_steps) FROM stdin;
X00000005	P00000004	e4ba8e19-3122-4635-969e-d2461562d506	U00000004	Running	2026-05-07 00:40:12.073709+00	2026-05-07 01:10:48.294805+00	\N	\N	3	2
X00000006	P00000005	e1f9c3f0-679e-40ce-afe7-c284b827ba77	U00000004	Running	2026-05-07 03:08:51.683625+00	2026-05-07 03:08:52.580851+00	\N	\N	4	1
X00000007	P00000006	656cb79a-c7c2-4ef9-af43-aa90507919ad	U00000004	Failed	2026-05-07 03:35:10.885365+00	2026-05-07 03:35:10.986653+00	2026-05-07 03:35:12.003634+00	Step 'heterozygote' requires Phred quality scores, but trace 656cb79a-c7c2-4ef9-af43-aa90507919ad has none in its parsed data.	3	1
\.


--
-- Data for Name: pipeline_step_executions; Type: TABLE DATA; Schema: pipelines; Owner: -
--

COPY pipelines.pipeline_step_executions (id, pipeline_step_id, "order", step_type, status, started_at, completed_at, error_message, result_summary, result_data, execution_id) FROM stdin;
5c46bdad-7256-4433-8f76-fcf7e806e747	e1ce23ad-79e4-4a19-9cea-d234cc8c9997	1	Quality	Completed	2026-05-07 01:10:48.295498+00	2026-05-07 01:10:48.370322+00	\N	\N	\N	X00000005
9c0e4194-dfbc-47ec-8e22-e6075c4d9c18	1e49177a-d0b4-43ab-8601-f355744b1be1	2	Motif	Completed	2026-05-07 01:10:48.378983+00	2026-05-07 01:10:49.07494+00	\N	\N	\N	X00000005
07328e81-dc67-4776-9e8f-3e491318f942	b07d07ef-06d2-42d9-853b-982cdc50519a	3	Trimming	Running	2026-05-07 01:10:49.08224+00	\N	\N	\N	\N	X00000005
2f36d109-7632-436d-b6e2-6fe9b16ae64d	5ac016bd-0e0d-4cd8-b05d-a2e9657fa4b9	4	Motif	Pending	\N	\N	\N	\N	\N	X00000006
443c6b10-3507-4875-8797-db147f931151	e6a983cc-acda-45ca-94fd-2fbb63b926b3	3	Heterozygote	Pending	\N	\N	\N	\N	\N	X00000006
34162c35-8090-4472-b54a-2f1c12cfd7a0	7bfc93c1-1d6c-4ad0-a3fa-23e3d8ff6299	1	Quality	Completed	2026-05-07 03:08:52.581316+00	2026-05-07 03:08:52.588334+00	\N	\N	\N	X00000006
00756c2e-a38a-4fa4-b09c-3daa16644ff0	e151df2b-2e5a-4f7b-a52a-d83b50e57a5a	2	Trimming	Running	2026-05-07 03:08:52.595941+00	\N	\N	\N	\N	X00000006
826342da-4b25-49e9-b53d-be1dcfeb5a76	0daacfcd-907d-420e-baec-2bb5f31dbce8	3	ORF	Pending	\N	\N	\N	\N	\N	X00000007
213a239f-fb96-496f-b938-253e50c530e6	1c550922-c78e-4b84-986c-c062b7ba34d2	1	Motif	Completed	2026-05-07 03:35:10.987133+00	2026-05-07 03:35:11.988148+00	\N	\N	\N	X00000007
ed346afa-ca58-4ba5-a9b6-2fcf1246b434	5d1da197-2a86-440c-a433-a16f14f2ef6d	2	Heterozygote	Failed	2026-05-07 03:35:11.995744+00	2026-05-07 03:35:12.003437+00	Step 'heterozygote' requires Phred quality scores, but trace 656cb79a-c7c2-4ef9-af43-aa90507919ad has none in its parsed data.	\N	\N	X00000007
\.


--
-- Data for Name: pipeline_steps; Type: TABLE DATA; Schema: pipelines; Owner: -
--

COPY pipelines.pipeline_steps (id, step_type, "order", label, configuration, is_enabled, created_at, pipeline_id) FROM stdin;
cac9a657-20f4-4343-83e2-2b964270bebb	Quality	1	\N	{}	t	2026-04-27 21:43:18.826386+00	\N
6a1d5ef0-c454-4a35-b021-40fc2ce83812	Quality	1	\N	{}	t	2026-05-06 23:40:47.435817+00	P00000003
580ea7de-4e56-4657-959d-465438332438	Translation	2	\N	{"frame":1}	t	2026-05-06 23:40:56.304678+00	P00000003
1e49177a-d0b4-43ab-8601-f355744b1be1	Motif	2	\N	{"pattern":"GATT","search_complement":false}	t	2026-05-07 00:28:30.069288+00	\N
b07d07ef-06d2-42d9-853b-982cdc50519a	Trimming	3	\N	{"algorithm":"lucy","cutoff":0.1}	t	2026-05-07 00:28:45.451279+00	\N
e1ce23ad-79e4-4a19-9cea-d234cc8c9997	Quality	1	\N	{}	t	2026-05-07 00:28:22.98247+00	\N
5ac016bd-0e0d-4cd8-b05d-a2e9657fa4b9	Motif	4	\N	{"pattern":"AAA","search_complement":false}	t	2026-05-07 03:08:26.890037+00	\N
7bfc93c1-1d6c-4ad0-a3fa-23e3d8ff6299	Quality	1	\N	{}	t	2026-05-07 03:08:12.82791+00	\N
e151df2b-2e5a-4f7b-a52a-d83b50e57a5a	Trimming	2	\N	{"algorithm":"mott","cutoff":0.05}	t	2026-05-07 03:08:15.231613+00	\N
e6a983cc-acda-45ca-94fd-2fbb63b926b3	Heterozygote	3	\N	{"min_ratio":0.3,"max_ratio":0.7}	t	2026-05-07 03:08:17.775094+00	\N
0daacfcd-907d-420e-baec-2bb5f31dbce8	ORF	3	\N	{"min_length":100}	t	2026-05-07 03:15:55.85653+00	\N
1c550922-c78e-4b84-986c-c062b7ba34d2	Motif	1	\N	{"pattern":"ATT","search_complement":false}	t	2026-05-07 03:15:49.660756+00	\N
5d1da197-2a86-440c-a433-a16f14f2ef6d	Heterozygote	2	\N	{"min_ratio":0.3,"max_ratio":0.7}	t	2026-05-07 03:15:53.328712+00	\N
\.


--
-- Data for Name: pipelines; Type: TABLE DATA; Schema: pipelines; Owner: -
--

COPY pipelines.pipelines (id, study_id, owner_id, name, description, status, created_at, created_by, modified_at, modified_by, "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
P00000002	S00000006	U00000004	Prueba	Prueba	Draft	2026-04-27 20:56:36.521444+00	4	2026-04-27 22:43:47.09158+00	4	f	\N	\N
P00000003	S00000007	U00000004	├ºPrueba	aaa	Draft	2026-05-06 23:40:43.814724+00	4	2026-05-06 23:40:56.30468+00	4	f	\N	\N
\.


--
-- Data for Name: plan_features; Type: TABLE DATA; Schema: plans; Owner: -
--

COPY plans.plan_features (id, plan_id, featuREDACTED, featuREDACTED) FROM stdin;
1	L00000003	1	CopilotAccess
2	L00000003	2	PriorityProcessing
3	L00000003	5	ExportFeatures
4	L00000003	3	AdvancedAnalytics
5	L00000003	4	ApiAccess
6	L00000003	6	TeamCollaboration
7	L00000003	7	CustomWorkflows
8	L00000003	8	SsoIntegration
9	L00000003	9	DedicatedSupport
\.


--
-- Data for Name: plans; Type: TABLE DATA; Schema: plans; Owner: -
--

COPY plans.plans (id, name, description, monthly_price, annual_price, currency, max_studies, max_traces_per_month, max_members_per_study, is_active, is_default, display_order, created_at, created_by, modified_at, modified_by) FROM stdin;
L00000001	Free	Plan gratuito para empezar. Ideal para probar la plataforma.	0.00	0.00	EUR	2	50	3	t	t	0	-infinity	\N	\N	\N
L00000002	Pro	Para investigadores individuales y equipos pequenos.	29.00	290.00	EUR	10	500	10	t	f	1	-infinity	\N	\N	\N
L00000003	Enterprise	Para equipos grandes y organizaciones. Sin limites.	99.00	990.00	EUR	-1	-1	-1	t	f	2	-infinity	\N	\N	\N
\.


--
-- Data for Name: profiles; Type: TABLE DATA; Schema: profiles; Owner: -
--

COPY profiles.profiles (id, user_id, first_name, last_name, bio, location, professional_role, institution_name, institution_department, research_field, orcid_id, website, photo_url, photo_thumbnail_url, photo_size_bytes, created_at, created_by, modified_at, modified_by, is_deleted, deleted_at, deleted_by) FROM stdin;
P00000006	U00000004	Eduardo	Marrero Gonz├ílez	Biolog├¡a molecular	Las Palmas de Gran Canaria	Estudiante de Doctorado	Universidad Las Palmas de Gran Canaria. ULPGC	Departamento de Biolog├¡a	MolecularBiology	0000-0002-1825-0097	https://martinez-lab.stanford.ed	/storage/profiles/6/photo.jpg	/storage/profiles/6/thumbnail.jpg	143236	2026-04-19 19:37:21.756035+00	\N	2026-04-21 21:17:06.229405+00	\N	f	\N	\N
P00000007	U00000009	Sarah	Chen	Marine biologist specializing in developmental biology and CRISPR applications.	Woods Hole, MA	Senior Researcher	Marine Biological Laboratory	Developmental Biology	Genomics	\N	\N	\N	\N	\N	2026-04-21 22:24:46.861807+00	\N	\N	\N	f	\N	\N
P00000008	U00000010	Michael	Green	Plant geneticist focused on drought resistance mechanisms.	Davis, CA	Associate Professor	Plant Sciences Institute	Plant Genetics	Genomics	\N	\N	\N	\N	\N	2026-04-21 22:24:46.861807+00	\N	\N	\N	f	\N	\N
P00000009	U00000011	Emma	Wilson	Population geneticist studying human ancestry and migration patterns.	Cambridge, UK	Research Fellow	Genetics Research Center	Population Genetics	Genetics	\N	\N	\N	\N	\N	2026-04-21 22:24:46.861807+00	\N	\N	\N	f	\N	\N
P00000010	U00000012	James	Liu	Bioinformatician developing sequencing analysis pipelines.	San Francisco, CA	Core Facility Director	Sequencing Core Facility	Bioinformatics	Bioinformatics	\N	\N	\N	\N	\N	2026-04-21 22:24:46.861807+00	\N	\N	\N	f	\N	\N
P00000011	U00000013	Ana	Martinez	Cancer researcher studying protein expression in hypoxic conditions.	Houston, TX	Principal Investigator	Cancer Research Institute	Proteomics	Proteomics	\N	\N	\N	\N	\N	2026-04-21 22:24:46.861807+00	\N	\N	\N	f	\N	\N
P00000012	U00000014	Robert	Brown	Environmental microbiologist studying soil ecosystems.	Madison, WI	Professor	Environmental Sciences Lab	Microbiology	Metagenomics	\N	\N	\N	\N	\N	2026-04-21 22:24:46.861807+00	\N	\N	\N	f	\N	\N
P00000013	U00000015	Lisa	Wang	Marine microbiologist studying deep-sea bacterial evolution.	San Diego, CA	Assistant Professor	Oceanography Institute	Marine Biology	Phylogenetics	\N	\N	\N	\N	\N	2026-04-21 22:24:46.861807+00	\N	\N	\N	f	\N	\N
P00000014	U00000016	David	Kim	Molecular biologist studying stress responses in yeast.	Boston, MA	Postdoctoral Fellow	Molecular Biology Lab	Molecular Biology	Transcriptomics	\N	\N	\N	\N	\N	2026-04-21 22:24:46.861807+00	\N	\N	\N	f	\N	\N
P00000015	U00000017	Jennifer	Lee	Synthetic biologist developing CRISPR tools.	Berkeley, CA	Research Scientist	Synthetic Biology Center	Synthetic Biology	MolecularBiology	\N	\N	\N	\N	\N	2026-04-21 22:24:46.861807+00	\N	\N	\N	f	\N	\N
P00000016	U00000018	Thomas	Anderson	Agricultural geneticist working on crop improvement.	Ames, IA	Research Director	Agricultural Research Station	Crop Genetics	Genetics	\N	\N	\N	\N	\N	2026-04-21 22:24:46.861807+00	\N	\N	\N	f	\N	\N
P00000017	U00000019	Maria	Garcia	Physician-scientist studying gut microbiome and IBD.	Baltimore, MD	Clinical Researcher	Medical Research Center	Gastroenterology	Metagenomics	\N	\N	\N	\N	\N	2026-04-21 22:24:46.861807+00	\N	\N	\N	f	\N	\N
P00000018	U00000020	Peter	Zhang	Botanist specializing in chloroplast genomics.	St. Louis, MO	Curator	Botanical Gardens Research	Plant Sciences	Genomics	\N	\N	\N	\N	\N	2026-04-21 22:24:46.861807+00	\N	\N	\N	f	\N	\N
P00000019	U00000021	Susan	Park	Virologist characterizing novel environmental viruses.	Atlanta, GA	Senior Scientist	Virology Institute	Virology	Genomics	\N	\N	\N	\N	\N	2026-04-21 22:24:46.861807+00	\N	\N	\N	f	\N	\N
P00000020	U00000022	John	Smith	Clinical scientist developing diagnostic PCR assays.	Rochester, MN	Lab Director	Diagnostic Lab	Clinical Diagnostics	MolecularBiology	\N	\N	\N	\N	\N	2026-04-21 22:24:46.861807+00	\N	\N	\N	f	\N	\N
P00000021	U00000023	Karen	White	Infectious disease researcher studying antimicrobial resistance.	Seattle, WA	Associate Professor	Infectious Disease Center	Microbiology	Transcriptomics	\N	\N	\N	\N	\N	2026-04-21 22:24:46.861807+00	\N	\N	\N	f	\N	\N
P00000022	U00000024	Daniel	Moore	Conservation biologist studying island endemic species.	Honolulu, HI	Research Fellow	Conservation Biology Lab	Conservation Genetics	Genetics	\N	\N	\N	\N	\N	2026-04-21 22:24:46.861807+00	\N	\N	\N	f	\N	\N
P00000023	U00000025	Rachel	Taylor	Environmental engineer studying wastewater microbiomes.	Ann Arbor, MI	Assistant Professor	Environmental Engineering Dept	Environmental Microbiology	Metagenomics	\N	\N	\N	\N	\N	2026-04-21 22:24:46.861807+00	\N	\N	\N	f	\N	\N
P00000024	U00000026	Chris	Johnson	Epigeneticist mapping cancer methylation patterns.	Philadelphia, PA	Principal Investigator	Epigenetics Research Unit	Epigenetics	Genomics	\N	\N	\N	\N	\N	2026-04-21 22:24:46.861807+00	\N	\N	\N	f	\N	\N
P00000025	U00000027	Laura	Davis	Biotechnologist optimizing industrial yeast strains.	San Jose, CA	Senior Scientist	Biotechnology Institute	Industrial Biotechnology	Bioinformatics	\N	\N	\N	\N	\N	2026-04-21 22:24:46.861807+00	\N	\N	\N	f	\N	\N
P00000026	U00000028	Mark	Wilson	Bioinformatician developing NGS quality control tools.	Durham, NC	Core Director	Bioinformatics Core	Computational Biology	Bioinformatics	\N	\N	\N	\N	\N	2026-04-21 22:24:46.861807+00	\N	\N	\N	f	\N	\N
P00000027	U00000005	Eduardo	Marrero Gonz├ílez	Pruebaaaaaa	Las Palmas de Gran Canaria	Pruebaaaaa	Pruebaaaa	Pruebaaa	CellBiology	0000-0002-1825-0097	https://martinez-lab.stanford.ed	\N	\N	\N	2026-04-29 19:34:52.68892+00	\N	2026-04-29 19:34:52.691458+00	\N	f	\N	\N
\.


--
-- Data for Name: __EFMigrationsHistory; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") FROM stdin;
20260406014506_InitialUserContext	8.0.11
20260407201811_InitialProfiles	8.0.11
20260408184107_InitialPlans	8.0.11
20260411171003_ConvertPlanIdToPrefixedString	8.0.11
20260408184129_InitialSubscriptions	8.0.11
20260411171025_ConvertSubscriptionIdToPrefixedString	8.0.11
20260418000000_InitialPaymentMethods	8.0.11
20260421000950_InitialTraces	8.0.0
20260427191401_AddPipelines	8.0.11
20260427201642_AddPipelines	8.0.11
20260427214947_FixStepExecutionIdMapping	8.0.11
20260429000001_AddTraceTrims	8.0.11
20260507120000_FixPipelineExecutionTraceIdColumnType	8.0.11
\.


--
-- Data for Name: invitations; Type: TABLE DATA; Schema: studies; Owner: -
--

COPY studies.invitations (id, study_id, email, token, invited_by, expires_at, accepted_at) FROM stdin;
\.


--
-- Data for Name: members; Type: TABLE DATA; Schema: studies; Owner: -
--

COPY studies.members (study_id, user_id, role_id, invited_by) FROM stdin;
\.


--
-- Data for Name: studies; Type: TABLE DATA; Schema: studies; Owner: -
--

COPY studies.studies (id, owner_id, title, description, research_field, status, allow_public_comments, allow_data_download, requiREDACTED, views_count, stars_count, institution, principal_investigator, is_featured, tags, created_at, created_by, modified_at, modified_by, is_deleted, deleted_at, deleted_by) FROM stdin;
S00000002	U00000004	Prueba	a	Genomics	Draft	t	f	t	0	0	Universidad de Las Palmas de Gran Canaria	Prueba	f	{a}	2026-04-20 19:55:13.651292	4	2026-04-20 19:55:13.652651	\N	t	2026-04-20 19:57:42.300789	U00000004
S00000003	U00000004	Prueb	a	Genomics	Draft	t	f	t	1	1	Universidad de Las Palmas de Gran Canaria	Prueba	f	{a}	2026-04-20 21:53:12.006863	4	2026-04-20 21:53:12.00776	\N	t	2026-04-20 22:59:44.670885	U00000004
S00000004	U00000004	Pruebas	Prueba	Genomics	Draft	t	f	t	1	0	\N	\N	f	{}	2026-04-20 23:05:37.026557	4	2026-04-21 00:13:17.445049	\N	t	2026-04-21 00:13:19.755205	U00000004
S00000008	U00000004	Mitochondrial DNA Analysis	Population-level study of mitochondrial DNA variations in European populations for ancestry research.	Genetics	Published	t	f	t	4	0	Genetics Research Center	Dr. Emma Wilson	f	{research,genetics,mtdna}	2026-04-21 22:21:12.364152	U00000004	2026-04-22 00:26:13.081709	\N	f	\N	\N
S00000009	U00000004	Sanger Sequencing Validation	Validation pipeline for Sanger sequencing results comparing manual and automated analysis methods.	Bioinformatics	Draft	t	f	t	4	1	Sequencing Core Facility	Dr. James Liu	f	{research,bioinformatics}	2026-04-21 22:21:12.364152	U00000004	\N	\N	f	\N	\N
S00000006	U00000004	CRISPR Gene Editing in Zebrafish	Analyzing CRISPR-Cas9 gene editing efficiency in zebrafish embryos for developmental biology research.	Genomics	Active	t	f	t	6	2	Marine Biological Laboratory	Dr. Sarah Chen	f	{research,genomics,crispr}	2026-04-21 22:21:12.364152	U00000004	\N	\N	t	2026-04-29 21:03:54.703006	U00000004
S00000007	U00000004	Whole Genome Sequencing of Arabidopsis	Complete genome sequencing of Arabidopsis thaliana variants to identify SNPs associated with drought resistance.	Genomics	Published	t	f	t	6	0	Plant Sciences Institute	Dr. Michael Green	t	{research,genomics,plants}	2026-04-21 22:21:12.364152	U00000004	2026-04-22 00:26:24.088457	\N	f	\N	\N
S00000005	U00000004	Pruebaa	Prueba	Genomics	Draft	t	f	t	1	2	\N	\N	f	{}	2026-04-21 00:13:31.655662	4	2026-04-21 21:22:35.771132	\N	f	\N	\N
S00000020	U00000004	Drug Resistance Transcriptomics	Transcriptomic profiling of drug resistance mechanisms in bacterial pathogens.	Transcriptomics	Active	t	f	t	0	0	Infectious Disease Center	Dr. Karen White	f	{research,transcriptomics,amr}	2026-04-21 22:21:12.364152	U00000004	\N	\N	f	\N	\N
S00000024	U00000010	Root Microbiome Interactions	Study of plant-microbiome interactions in the rhizosphere under stress conditions.	Metagenomics	Draft	t	f	t	0	0	Plant Sciences Institute	Dr. Michael Green	f	{microbiome,roots,stress}	2026-04-16 22:28:11.935385	U00000010	\N	\N	f	\N	\N
S00000025	U00000011	European Population Structure Analysis	Fine-scale population structure analysis using ancient and modern DNA.	Genetics	Completed	t	f	t	0	0	Genetics Research Center	Dr. Emma Wilson	f	{population,ancient-dna,europe}	2026-01-21 22:28:11.935385	U00000011	\N	\N	f	\N	\N
S00000026	U00000011	Neanderthal Introgression Mapping	Mapping Neanderthal genetic contributions to modern human genomes.	Genetics	Active	t	f	t	0	0	Genetics Research Center	Dr. Emma Wilson	f	{neanderthal,introgression,evolution}	2026-04-01 22:28:11.935385	U00000011	\N	\N	f	\N	\N
S00000027	U00000012	Long-Read Sequencing Pipeline	Development of analysis pipeline for PacBio and Oxford Nanopore data.	Bioinformatics	Active	t	f	t	0	0	Sequencing Core Facility	Dr. James Liu	f	{long-read,pacbio,nanopore}	2026-02-20 22:28:11.935385	U00000012	\N	\N	f	\N	\N
S00000028	U00000012	Variant Calling Benchmarking	Comprehensive benchmarking of variant calling algorithms on diverse datasets.	Bioinformatics	Active	t	f	t	0	0	Sequencing Core Facility	Dr. James Liu	f	{variant-calling,benchmarking,ngs}	2026-04-11 22:28:11.935385	U00000012	\N	\N	f	\N	\N
S00000029	U00000013	Hypoxia Proteome Profiling	Mass spectrometry-based proteome profiling of cancer cells under hypoxia.	Proteomics	Active	t	f	t	0	0	Cancer Research Institute	Dr. Ana Martinez	f	{hypoxia,mass-spec,cancer}	2026-03-27 22:28:11.935385	U00000013	\N	\N	f	\N	\N
S00000030	U00000013	Tumor Microenvironment Secretome	Analysis of secreted proteins in the tumor microenvironment.	Proteomics	Draft	t	f	t	0	0	Cancer Research Institute	Dr. Ana Martinez	f	{secretome,tumor,microenvironment}	2026-04-18 22:28:11.935385	U00000013	\N	\N	f	\N	\N
S00000031	U00000014	Agricultural Soil Health Indicators	Metagenomic indicators of soil health in sustainable farming practices.	Metagenomics	Active	t	f	t	0	0	Environmental Sciences Lab	Dr. Robert Brown	f	{soil,agriculture,sustainability}	2026-03-12 22:28:11.935385	U00000014	\N	\N	f	\N	\N
S00000032	U00000014	Forest Floor Carbon Cycling	Microbial communities involved in forest floor carbon cycling.	Metagenomics	Active	t	f	t	0	0	Environmental Sciences Lab	Dr. Robert Brown	f	{forest,carbon,cycling}	2026-02-25 22:28:11.935385	U00000014	\N	\N	f	\N	\N
S00000033	U00000015	Hydrothermal Vent Ecosystem Evolution	Phylogenetic analysis of chemosynthetic bacteria from hydrothermal vents.	Phylogenetics	Active	t	f	t	0	0	Oceanography Institute	Dr. Lisa Wang	f	{hydrothermal,chemosynthesis,deep-sea}	2026-02-10 22:28:11.935385	U00000015	\N	\N	f	\N	\N
S00000014	U00000004	Plasmid Verification Project	Construction and verification of CRISPR plasmids for gene editing applications.	MolecularBiology	Draft	t	f	t	1	0	Synthetic Biology Center	Dr. Jennifer Lee	f	{research,molecular-biology,plasmids}	2026-04-21 22:21:12.364152	U00000004	\N	\N	f	\N	\N
S00000016	U00000004	Gut Microbiome Survey	Comprehensive metagenomic survey of human gut microbiome in healthy vs. IBD patients.	Metagenomics	Active	t	f	t	1	0	Medical Research Center	Dr. Maria Garcia	f	{research,metagenomics,health}	2026-04-21 22:21:12.364152	U00000004	\N	\N	f	\N	\N
S00000019	U00000004	PCR Primer Optimization	Systematic optimization of PCR primers for diagnostic applications in clinical settings.	MolecularBiology	Active	t	f	t	1	0	Diagnostic Lab	Dr. John Smith	f	{research,molecular-biology,pcr}	2026-04-21 22:21:12.364152	U00000004	\N	\N	f	\N	\N
S00000010	U00000004	Novel Protein Expression Study	Investigating novel protein expression patterns in cancer cell lines under hypoxic conditions.	Proteomics	Active	t	f	t	1	0	Cancer Research Institute	Dr. Ana Martinez	f	{research,proteomics,cancer}	2026-04-21 22:21:12.364152	U00000004	\N	\N	f	\N	\N
S00000017	U00000004	Chloroplast Assembly	De novo assembly of chloroplast genomes from various plant species for comparative analysis.	Genomics	Completed	t	f	t	1	0	Botanical Gardens Research	Dr. Peter Zhang	f	{research,genomics,chloroplast}	2026-04-21 22:21:12.364152	U00000004	\N	\N	f	\N	\N
S00000034	U00000015	Coral Symbiont Diversity	Evolutionary relationships among coral-associated microorganisms.	Phylogenetics	Active	t	f	t	0	0	Oceanography Institute	Dr. Lisa Wang	f	{coral,symbiosis,diversity}	2026-03-17 22:28:11.935385	U00000015	\N	\N	f	\N	\N
S00000035	U00000016	Yeast Heat Shock Response	Single-cell RNA-seq analysis of yeast heat shock response dynamics.	Transcriptomics	Completed	t	f	t	0	0	Molecular Biology Lab	Dr. David Kim	f	{yeast,heat-shock,single-cell}	2026-01-31 22:28:11.935385	U00000016	\N	\N	f	\N	\N
S00000036	U00000016	Oxidative Stress Transcriptome	Time-course transcriptomic analysis of oxidative stress in S. cerevisiae.	Transcriptomics	Active	t	f	t	0	0	Molecular Biology Lab	Dr. David Kim	f	{oxidative-stress,time-course,yeast}	2026-04-09 22:28:11.935385	U00000016	\N	\N	f	\N	\N
S00000037	U00000017	Base Editor Optimization	Optimization of cytosine and adenine base editors for therapeutic applications.	MolecularBiology	Active	t	f	t	0	0	Synthetic Biology Center	Dr. Jennifer Lee	f	{base-editing,crispr,therapeutics}	2026-03-24 22:28:11.935385	U00000017	\N	\N	f	\N	\N
S00000038	U00000017	Prime Editing Delivery Systems	Development of novel delivery systems for prime editing in vivo.	MolecularBiology	Draft	t	f	t	0	0	Synthetic Biology Center	Dr. Jennifer Lee	f	{prime-editing,delivery,gene-therapy}	2026-04-14 22:28:11.935385	U00000017	\N	\N	f	\N	\N
S00000039	U00000018	Wheat Rust Resistance Loci	Mapping of stem rust resistance loci in diverse wheat germplasm.	Genetics	Active	t	f	t	0	0	Agricultural Research Station	Dr. Thomas Anderson	f	{wheat,rust,resistance}	2026-03-02 22:28:11.935385	U00000018	\N	\N	f	\N	\N
S00000040	U00000018	Rice Yield QTL Analysis	Quantitative trait loci analysis for rice yield under drought stress.	Genetics	Active	t	f	t	0	0	Agricultural Research Station	Dr. Thomas Anderson	f	{rice,qtl,yield}	2026-03-30 22:28:11.935385	U00000018	\N	\N	f	\N	\N
S00000041	U00000019	IBD Microbiome Signatures	Identification of microbial signatures distinguishing Crohn's disease subtypes.	Metagenomics	Active	t	f	t	0	0	Medical Research Center	Dr. Maria Garcia	f	{ibd,crohns,microbiome}	2026-02-15 22:28:11.935385	U00000019	\N	\N	f	\N	\N
S00000042	U00000019	Probiotic Intervention Study	Metagenomics analysis of probiotic intervention in ulcerative colitis patients.	Metagenomics	Active	t	f	t	0	0	Medical Research Center	Dr. Maria Garcia	f	{probiotics,uc,clinical}	2026-04-03 22:28:11.935385	U00000019	\N	\N	f	\N	\N
S00000043	U00000021	Environmental Virome Discovery	Discovery of novel viruses from wastewater surveillance samples.	Genomics	Active	t	f	t	0	0	Virology Institute	Dr. Susan Park	f	{virome,wastewater,surveillance}	2026-03-14 22:28:11.935385	U00000021	\N	\N	f	\N	\N
S00000044	U00000021	Phage-Bacteria Dynamics	Longitudinal study of bacteriophage-bacteria dynamics in environmental samples.	Genomics	Draft	t	f	t	0	0	Virology Institute	Dr. Susan Park	f	{phage,bacteria,ecology}	2026-04-13 22:28:11.935385	U00000021	\N	\N	f	\N	\N
S00000045	U00000023	Carbapenem Resistance Mechanisms	Transcriptomic analysis of carbapenem resistance in Enterobacteriaceae.	Transcriptomics	Active	t	f	t	0	0	Infectious Disease Center	Dr. Karen White	f	{amr,carbapenem,enterobacteriaceae}	2026-03-10 22:28:11.935385	U00000023	\N	\N	f	\N	\N
S00000046	U00000023	Biofilm Gene Expression	Gene expression profiling of biofilm formation in multidrug-resistant pathogens.	Transcriptomics	Active	t	f	t	0	0	Infectious Disease Center	Dr. Karen White	f	{biofilm,mdr,expression}	2026-04-07 22:28:11.935385	U00000023	\N	\N	f	\N	\N
S00000047	U00000026	Pancreatic Cancer Methylome	Comprehensive DNA methylation profiling of pancreatic cancer progression.	Genomics	Active	t	f	t	0	0	Epigenetics Research Unit	Dr. Chris Johnson	f	{methylation,pancreatic,progression}	2026-02-05 22:28:11.935385	U00000026	\N	\N	f	\N	\N
S00000048	U00000026	Histone Modification Atlas	ChIP-seq atlas of histone modifications in normal vs tumor tissues.	Genomics	Active	t	f	t	0	0	Epigenetics Research Unit	Dr. Chris Johnson	f	{histone,chip-seq,epigenetics}	2026-03-19 22:28:11.935385	U00000026	\N	\N	f	\N	\N
S00000049	U00000028	Machine Learning Variant Classifier	Deep learning model for pathogenic variant classification.	Bioinformatics	Active	t	f	t	0	0	Bioinformatics Core	Dr. Mark Wilson	f	{machine-learning,variant,classification}	2026-03-04 22:28:11.935385	U00000028	\N	\N	f	\N	\N
S00000050	U00000028	Single-Cell Analysis Toolkit	Development of integrated toolkit for single-cell RNA-seq analysis.	Bioinformatics	Active	t	f	t	0	0	Bioinformatics Core	Dr. Mark Wilson	f	{single-cell,toolkit,rnaseq}	2026-04-05 22:28:11.935385	U00000028	\N	\N	f	\N	\N
S00000012	U00000004	Marine Bacteria Phylogeny	Evolutionary phylogenetic analysis of marine bacteria from deep sea hydrothermal vents.	Phylogenetics	Published	t	f	t	0	0	Oceanography Institute	Dr. Lisa Wang	f	{research,phylogenetics,marine}	2026-04-21 22:21:12.364152	U00000004	2026-04-22 00:26:13.081709	\N	f	\N	\N
S00000013	U00000004	Stress Response RNA-Seq	RNA-Seq analysis of stress response genes in yeast under various environmental conditions.	Transcriptomics	Published	t	f	t	0	0	Molecular Biology Lab	Dr. David Kim	f	{research,transcriptomics,yeast}	2026-04-21 22:21:12.364152	U00000004	2026-04-22 00:26:13.081709	\N	f	\N	\N
S00000015	U00000004	Agricultural SNP Detection	SNP detection in wheat and rice cultivars for marker-assisted breeding programs.	Genetics	Published	t	f	t	0	0	Agricultural Research Station	Dr. Thomas Anderson	f	{research,genetics,agriculture}	2026-04-21 22:21:12.364152	U00000004	2026-04-22 00:26:13.081709	\N	f	\N	\N
S00000018	U00000004	Viral Characterization	Genomic characterization of novel viral strains isolated from environmental samples.	Genomics	Published	t	f	t	0	0	Virology Institute	Dr. Susan Park	f	{research,genomics,virology}	2026-04-21 22:21:12.364152	U00000004	2026-04-22 00:26:13.081709	\N	f	\N	\N
S00000022	U00000009	CRISPR Knockout Library Screening	High-throughput CRISPR knockout screening in marine model organisms.	Genomics	Published	t	f	t	0	0	Marine Biological Laboratory	Dr. Sarah Chen	f	{crispr,screening,marine}	2026-04-06 22:28:11.935385	U00000009	2026-04-22 00:26:13.081709	\N	f	\N	\N
S00000021	U00000009	Zebrafish Embryo Development Atlas	Comprehensive imaging and sequencing atlas of zebrafish embryonic development stages.	Genomics	Published	t	f	t	0	0	Marine Biological Laboratory	Dr. Sarah Chen	t	{zebrafish,development,atlas}	2026-03-22 22:28:11.935385	U00000009	2026-04-22 00:26:24.088457	\N	f	\N	\N
S00000023	U00000010	Drought Tolerance Gene Discovery	Identification of novel drought tolerance genes in Arabidopsis using GWAS.	Genomics	Published	t	f	t	1	0	Plant Sciences Institute	Dr. Michael Green	f	{drought,gwas,arabidopsis}	2026-03-07 22:28:11.935385	U00000010	2026-04-22 00:26:13.081709	\N	f	\N	\N
S00000011	U00000004	Soil Microbiome Metagenomics	Metagenomic analysis of soil microbial communities in agricultural vs. forest environments.	Metagenomics	Published	t	f	t	1	0	Environmental Sciences Lab	Dr. Robert Brown	t	{research,metagenomics,soil}	2026-04-21 22:21:12.364152	U00000004	2026-04-22 00:26:24.088457	\N	f	\N	\N
S00000052	U00000031	E2E Analysis Study 166b7f9b	Automated E2E validation	Genomics	Draft	t	f	t	0	0	\N	\N	f	{}	2026-05-04 01:13:17.281578	31	\N	\N	f	\N	\N
S00000053	U00000032	E2E Analysis Study e34b0835	Automated E2E validation	Genomics	Draft	t	f	t	0	0	\N	\N	f	{}	2026-05-04 01:20:17.511226	32	\N	\N	f	\N	\N
S00000054	U00000033	E2E Analysis Study 93cfeb8e	Automated E2E validation	Genomics	Draft	t	f	t	0	0	\N	\N	f	{}	2026-05-04 01:28:28.681338	33	\N	\N	f	\N	\N
S00000055	U00000034	E2E Analysis Study 4f0d90a5	Automated E2E validation	Genomics	Draft	t	f	t	0	0	\N	\N	f	{}	2026-05-04 01:34:14.393166	34	\N	\N	f	\N	\N
S00000056	U00000035	E2E Analysis Study b89e90b5	Automated E2E validation	Genomics	Draft	t	f	t	0	0	\N	\N	f	{}	2026-05-04 01:37:59.951435	35	\N	\N	f	\N	\N
S00000057	U00000036	E2E Analysis Study cf68ca20	Automated E2E validation	Genomics	Draft	t	f	t	0	0	\N	\N	f	{}	2026-05-04 04:32:38.523674	36	\N	\N	f	\N	\N
S00000058	U00000037	E2E Analysis Study 8b6a8eea	Automated E2E validation	Genomics	Draft	t	f	t	0	0	\N	\N	f	{}	2026-05-04 04:39:03.874086	37	\N	\N	f	\N	\N
\.


--
-- Data for Name: study_invitations; Type: TABLE DATA; Schema: studies; Owner: -
--

COPY studies.study_invitations (id, study_id, email, role, status, token, invited_by, expires_at, responded_at, message, created_at, created_by, modified_at, modified_by) FROM stdin;
I00000001	S00000005	eduardo.marrero106@alu.ulpgc.es	Admin	Pending	2f463791d6fb2060dade9f3e8c24da442f2205a10e11bfda3b880a69cfd46dce	U00000004	2026-04-28 22:33:48.721021	\N		2026-04-21 22:33:48.721102	4	\N	\N
I00000003	S00000009	eduardo.marrero106@alu.ulpgc.es	Viewer	Declined	b13ded7a549af1082a37bbea203aa1d3e72ef1435f3d160b8990d24b33014c9a	U00000004	2026-05-13 21:50:15.176494	2026-05-06 22:04:14.046414		2026-05-06 21:50:15.176496	4	\N	\N
I00000002	S00000008	eduardo.marrero106@alu.ulpgc.es	Viewer	Declined	8815d570e6be63eb38fbba893556693e272cd8943c6e467d76628b46b17bb3df	U00000004	2026-05-13 21:27:26.383244	2026-05-06 22:04:14.967128		2026-05-06 21:27:26.383309	4	\N	\N
I00000004	S00000009	eduardo.marrero106@alu.ulpgc.es	Viewer	Declined	0517ef19cbcafbfe6a3e22672ee367c4512eca155a61ca417acce7d76cc4e093	U00000004	2026-05-13 22:18:36.58041	2026-05-06 22:23:32.432735		2026-05-06 22:18:36.580474	4	\N	\N
I00000005	S00000009	eduardo.marrero106@alu.ulpgc.es	Editor	Declined	daa43b12f7e709e8e9a3fae76e03ef933e15897860eb6a685ee1ed235ceaf896	U00000004	2026-05-13 22:33:42.653516	2026-05-06 22:46:06.217579		2026-05-06 22:33:42.653591	4	\N	\N
I00000006	S00000009	eduardo.marrero106@alu.ulpgc.es	Admin	Accepted	8d73807657fb2fb120ab7cc94b258daa89eff099669bbbd31dafdac4c719f855	U00000004	2026-05-13 22:52:20.852412	2026-05-06 22:52:38.208351		2026-05-06 22:52:20.852477	4	\N	\N
\.


--
-- Data for Name: study_members; Type: TABLE DATA; Schema: studies; Owner: -
--

COPY studies.study_members (id, study_id, user_id, role, joined_at, invited_by) FROM stdin;
276ae99b-e8ad-4bfd-83a5-9c6d6e75a9ce	S00000002	U00000004	Owner	2026-04-20 19:55:13.651229	\N
9b951d89-b316-47f2-bc9e-61d3403df0b6	S00000003	U00000004	Owner	2026-04-20 21:53:12.006797	\N
c2b90802-6576-4f2f-a1c6-47703f95b6d1	S00000004	U00000004	Owner	2026-04-20 23:05:37.02649	\N
60e9b7ed-ca2b-4101-b2b7-428fe25dc8a1	S00000005	U00000004	Owner	2026-04-21 00:13:31.655599	\N
cf1c85ab-131f-4c7b-a6b0-570c2d2192ce	S00000006	U00000004	Owner	2026-04-21 22:21:12.364152	\N
a1b186fc-793f-44ee-affa-29118a559a75	S00000007	U00000004	Owner	2026-04-21 22:21:12.364152	\N
f1f5519b-3a6b-4edd-8078-896afd6bf018	S00000008	U00000004	Owner	2026-04-21 22:21:12.364152	\N
5d4969b1-fba3-4bc7-b651-35ed116aa823	S00000009	U00000004	Owner	2026-04-21 22:21:12.364152	\N
07bfbbc6-d7b4-4a94-81cc-c905637e6d57	S00000010	U00000004	Owner	2026-04-21 22:21:12.364152	\N
62039539-f12c-46e4-a480-388feb0caa2d	S00000011	U00000004	Owner	2026-04-21 22:21:12.364152	\N
031d03a5-9e25-4cec-93fe-f031261ac666	S00000012	U00000004	Owner	2026-04-21 22:21:12.364152	\N
e65e8e1f-0d0e-4216-b151-ee213cb92e43	S00000013	U00000004	Owner	2026-04-21 22:21:12.364152	\N
4dddb2e1-6a74-4661-9573-a7061556a08b	S00000014	U00000004	Owner	2026-04-21 22:21:12.364152	\N
5f34c266-df0f-49b3-b924-99b66e710ab7	S00000015	U00000004	Owner	2026-04-21 22:21:12.364152	\N
30e8b755-d3c3-4bfb-af52-7cf6ac586263	S00000016	U00000004	Owner	2026-04-21 22:21:12.364152	\N
028ce44b-c1e1-4eff-9303-a5a426a02626	S00000017	U00000004	Owner	2026-04-21 22:21:12.364152	\N
849305d9-0364-4e7f-9774-651ad1a3a3b0	S00000018	U00000004	Owner	2026-04-21 22:21:12.364152	\N
28857454-dcf0-4dc4-87f7-509a39a4ff87	S00000019	U00000004	Owner	2026-04-21 22:21:12.364152	\N
e6a4c4cd-789d-4e9a-8a7c-dc1b51d784aa	S00000020	U00000004	Owner	2026-04-21 22:21:12.364152	\N
122c70f3-7283-41f6-9690-bb9b3ab03b46	S00000021	U00000009	Owner	2026-03-22 22:28:11.935385	\N
e1e82490-fef8-4b94-9ecb-bfa1ad3ce70d	S00000022	U00000009	Owner	2026-04-06 22:28:11.935385	\N
4b9749a3-1e3b-4376-ac35-f37c65faf860	S00000023	U00000010	Owner	2026-03-07 22:28:11.935385	\N
4421dc79-8eef-4bea-8c2a-702bd4a36eb4	S00000024	U00000010	Owner	2026-04-16 22:28:11.935385	\N
99b8a08a-ae52-43a7-9ef6-a8a9c233cd9b	S00000025	U00000011	Owner	2026-01-21 22:28:11.935385	\N
e1bda6ce-9368-4022-8d07-32c7001a385d	S00000026	U00000011	Owner	2026-04-01 22:28:11.935385	\N
6f64e6a8-2de1-48a3-a673-73c868b0c489	S00000027	U00000012	Owner	2026-02-20 22:28:11.935385	\N
2199c8a6-d136-4ce8-a4c1-14e65ab924e7	S00000028	U00000012	Owner	2026-04-11 22:28:11.935385	\N
ba1fa4a8-635c-40e3-8fa1-e26c25128620	S00000029	U00000013	Owner	2026-03-27 22:28:11.935385	\N
9936f305-de3b-448f-948e-9e6d9e4fbcb3	S00000030	U00000013	Owner	2026-04-18 22:28:11.935385	\N
2f1fdbaf-24fd-4b26-b508-8a4e6abecfe6	S00000031	U00000014	Owner	2026-03-12 22:28:11.935385	\N
2af07ee1-79ba-498d-a833-2ab0e8a9953c	S00000032	U00000014	Owner	2026-02-25 22:28:11.935385	\N
d2759bc6-cdbf-42f0-aaa6-2594db0eacf9	S00000033	U00000015	Owner	2026-02-10 22:28:11.935385	\N
1d5a3e6e-c127-451f-835a-2ff8cadfd152	S00000034	U00000015	Owner	2026-03-17 22:28:11.935385	\N
848d3582-ddab-420d-8128-a1e3dc237d45	S00000035	U00000016	Owner	2026-01-31 22:28:11.935385	\N
e3b41186-b99d-49a3-963b-561b1721ef55	S00000036	U00000016	Owner	2026-04-09 22:28:11.935385	\N
23f512ec-5113-4238-bd6b-b032eb335a34	S00000037	U00000017	Owner	2026-03-24 22:28:11.935385	\N
fe81e9b9-7fad-4a1b-bc2e-d7ab092511ab	S00000038	U00000017	Owner	2026-04-14 22:28:11.935385	\N
1fd3cd27-e47b-4deb-bd03-b0d14bb64888	S00000039	U00000018	Owner	2026-03-02 22:28:11.935385	\N
f1bca372-fe45-44be-95b0-5e677bed8c4d	S00000040	U00000018	Owner	2026-03-30 22:28:11.935385	\N
77038425-400a-4f6d-9fa4-da6d01ffe87a	S00000041	U00000019	Owner	2026-02-15 22:28:11.935385	\N
a47cb38e-7c0a-4f28-b506-2ee7a539e211	S00000042	U00000019	Owner	2026-04-03 22:28:11.935385	\N
73b948f8-ca69-4138-a4ea-1e4e053eaf0b	S00000043	U00000021	Owner	2026-03-14 22:28:11.935385	\N
cc72f811-b52c-43e9-9ba3-01a86d224898	S00000044	U00000021	Owner	2026-04-13 22:28:11.935385	\N
5f4ca303-815b-486a-a1da-c90a5eef9443	S00000045	U00000023	Owner	2026-03-10 22:28:11.935385	\N
40f7835f-8993-4648-ad02-30f7a3686a19	S00000046	U00000023	Owner	2026-04-07 22:28:11.935385	\N
813b3b77-599d-411b-a5d1-6040290bbbd7	S00000047	U00000026	Owner	2026-02-05 22:28:11.935385	\N
071bcb8a-c757-46b3-8ea3-863ff5866972	S00000048	U00000026	Owner	2026-03-19 22:28:11.935385	\N
bf750c24-4118-4430-ae4c-18c5ecd8ab86	S00000049	U00000028	Owner	2026-03-04 22:28:11.935385	\N
d10bd831-2084-46df-a470-c12b9f8d3fbd	S00000050	U00000028	Owner	2026-04-05 22:28:11.935385	\N
0057ede2-8dce-4f37-9d47-7515b763ea71	S00000021	U00000012	Collaborator	2026-03-27 22:28:11.951957	\N
e6582da0-9100-4cdb-b205-427a21ac466d	S00000025	U00000013	Collaborator	2026-01-26 22:28:11.951957	\N
d2598ac3-e1d0-4296-a3f8-ccd1e66e3637	S00000027	U00000028	Admin	2026-02-25 22:28:11.951957	\N
69cbbcad-a8ed-41f0-8906-84bffc57dec5	S00000037	U00000016	Collaborator	2026-04-01 22:28:11.951957	\N
539e0177-0206-4bcf-bb58-0d437569f76d	S00000041	U00000014	Collaborator	2026-02-20 22:28:11.951957	\N
f26ed6c2-81bf-417f-b22d-7c0a8f4c00c2	S00000043	U00000015	Collaborator	2026-03-22 22:28:11.951957	\N
531c2aae-e6a8-48f8-8145-39e3da050cf1	S00000052	U00000031	Owner	2026-05-04 01:13:17.281543	\N
17b050b3-1b4c-426e-ae3c-b4dfab9f8669	S00000053	U00000032	Owner	2026-05-04 01:20:17.511192	\N
dd5f4379-3278-48dc-b238-5629772a1a72	S00000054	U00000033	Owner	2026-05-04 01:28:28.681301	\N
ff92d03a-527d-4e74-8045-04003573e060	S00000055	U00000034	Owner	2026-05-04 01:34:14.393163	\N
2111f7d0-3742-400e-9488-a54c08beda65	S00000056	U00000035	Owner	2026-05-04 01:37:59.951432	\N
17e6bbdd-5a31-47e6-8b9f-159fdfd0c146	S00000057	U00000036	Owner	2026-05-04 04:32:38.523671	\N
2763a628-9fda-4756-b966-f094b6f778ef	S00000058	U00000037	Owner	2026-05-04 04:39:03.874083	\N
\.


--
-- Data for Name: study_papers; Type: TABLE DATA; Schema: studies; Owner: -
--

COPY studies.study_papers (id, study_id, title, authors, doi, abstract, journal, publication_year, file_id, file_name, file_size_bytes, created_at, created_by, modified_at, modified_by, is_deleted, deleted_at, deleted_by) FROM stdin;
R00000001	S00000006	Prueba	s	s	a	a	2026	\N	\N	\N	2026-04-27 22:43:19.575406	4	\N	\N	t	2026-04-28 22:17:10.966981	4
R00000002	S00000009	A	A	A	\N	A	\N	\N	\N	\N	2026-05-06 20:27:11.65344	4	\N	\N	t	2026-05-06 20:27:20.232489	4
\.


--
-- Data for Name: study_stars; Type: TABLE DATA; Schema: studies; Owner: -
--

COPY studies.study_stars (id, study_id, user_id, starred_at) FROM stdin;
3773fe97-2a19-4289-8870-d01ef5fa9084	S00000005	U00000004	2026-04-21 20:05:45.083205
e230ba6c-ceaf-4e90-9fa7-a3b7031d1308	S00000006	U00000004	2026-04-22 20:17:20.818007
\.


--
-- Data for Name: study_views; Type: TABLE DATA; Schema: studies; Owner: -
--

COPY studies.study_views (id, study_id, user_id, ip_hash, user_agent, viewed_at) FROM stdin;
3	S00000003	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-20 21:58:59.967565
4	S00000003	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-20 21:58:59.948782
5	S00000004	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-20 23:05:39.029437
6	S00000004	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-20 23:05:39.02157
7	S00000005	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-21 00:13:32.601513
8	S00000005	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-21 00:13:32.601519
9	S00000006	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-21 22:31:20.490509
10	S00000006	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-21 22:31:20.490115
11	S00000008	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-21 23:38:57.449707
12	S00000008	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-21 23:38:57.443146
13	S00000023	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-22 00:39:20.37747
14	S00000023	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-22 00:39:20.37718
15	S00000014	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-22 00:39:23.545164
16	S00000014	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-22 00:39:23.55732
17	S00000007	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-22 20:17:31.976768
18	S00000007	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-22 20:17:31.972918
19	S00000011	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-22 20:17:38.60097
20	S00000006	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-25 19:17:27.02316
21	S00000006	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-25 19:17:27.023788
22	S00000007	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-25 20:56:03.327895
23	S00000007	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-25 20:56:03.324454
24	S00000006	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-27 19:02:12.47572
25	S00000006	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-27 19:02:12.475717
26	S00000006	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-28 20:55:01.897002
27	S00000006	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-28 20:55:01.895767
28	S00000007	U00000005	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-29 19:45:47.487083
29	S00000007	U00000005	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-29 19:45:47.48988
30	S00000007	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-29 19:53:20.90193
31	S00000007	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-29 19:53:20.902947
32	S00000008	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-29 20:10:56.731547
33	S00000008	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-29 20:10:56.728769
34	S00000009	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-29 20:11:03.245694
35	S00000009	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-29 20:11:03.24577
36	S00000016	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-29 20:11:06.37079
37	S00000006	U00000005	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-29 20:11:18.203717
38	S00000006	U00000005	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-29 20:11:18.207102
39	S00000008	U00000005	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-29 20:11:30.101647
40	S00000008	U00000005	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-29 20:11:30.101558
41	S00000006	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-29 21:03:45.065027
42	S00000006	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-29 21:03:45.054221
43	S00000010	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-29 21:45:42.271996
44	S00000010	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-29 21:45:42.276422
45	S00000017	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-29 21:51:49.781474
46	S00000017	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-29 21:51:49.787333
47	S00000019	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-29 21:54:50.218191
48	S00000019	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-04-29 21:54:50.22372
49	S00000009	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-05-05 21:21:56.84018
50	S00000009	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-05-05 21:21:56.839507
51	S00000007	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-05-06 00:43:07.297496
52	S00000007	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-05-06 00:43:07.297492
53	S00000009	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-05-06 21:22:04.328452
54	S00000009	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-05-06 21:22:04.331681
55	S00000008	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-05-06 21:27:20.342543
56	S00000009	U00000005	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-05-06 22:52:33.980907
57	S00000009	U00000005	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-05-06 22:52:33.980965
58	S00000007	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-05-07 01:19:22.145692
59	S00000007	U00000004	\N	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36	2026-05-07 01:19:22.145701
\.


--
-- Data for Name: subscriptions; Type: TABLE DATA; Schema: subscriptions; Owner: -
--

COPY subscriptions.subscriptions (id, user_id, plan_id, plan_name, status, billing_cycle, period_start, period_end, auto_renew, created_at, modified_at, cancelled_at, cancellation_reason, trial_end_date) FROM stdin;
S00000003	U00000001	L00000001	Free	1	1	2026-04-18 00:23:29.204274+00	2126-04-18 00:23:29.204274+00	f	2026-04-18 00:23:29.204274+00	\N	\N	\N	\N
S00000004	U00000002	L00000001	Free	1	1	2026-04-18 12:57:34.439134+00	2126-04-18 12:57:34.439134+00	f	2026-04-18 12:57:34.439134+00	\N	\N	\N	\N
S00000005	U00000003	L00000001	Free	1	1	2026-04-18 13:19:51.510158+00	2126-04-18 13:19:51.510158+00	f	2026-04-18 13:19:51.510158+00	\N	\N	\N	\N
S00000007	U00000005	L00000001	Free	1	1	2026-04-18 14:01:49.768601+00	2126-04-18 14:01:49.768601+00	f	2026-04-18 14:01:49.768601+00	\N	\N	\N	\N
S00000008	U00000006	L00000001	Free	1	1	2026-04-18 14:03:38.188964+00	2126-04-18 14:03:38.188964+00	f	2026-04-18 14:03:38.188964+00	\N	\N	\N	\N
S00000009	U00000007	L00000001	Free	1	1	2026-04-18 14:51:13.097865+00	2126-04-18 14:51:13.097865+00	f	2026-04-18 14:51:13.097865+00	\N	\N	\N	\N
S00000010	U00000008	L00000001	Free	1	1	2026-04-20 00:53:05.990411+00	2126-04-20 00:53:05.990411+00	f	2026-04-20 00:53:05.990411+00	\N	\N	\N	\N
S00000006	U00000004	L00000002	Pro	4	1	2026-04-20 21:45:55.066156+00	2026-05-20 21:45:55.066156+00	f	2026-04-18 13:59:36.894469+00	2026-04-20 21:51:04.407024+00	2026-04-20 21:51:04.406983+00	\N	\N
S00000011	U00000029	L00000001	Free	1	1	2026-05-04 01:11:37.325018+00	2126-05-04 01:11:37.325018+00	f	2026-05-04 01:11:37.325018+00	\N	\N	\N	\N
S00000012	U00000030	L00000001	Free	1	1	2026-05-04 01:12:59.525602+00	2126-05-04 01:12:59.525602+00	f	2026-05-04 01:12:59.525602+00	\N	\N	\N	\N
S00000013	U00000031	L00000001	Free	1	1	2026-05-04 01:13:14.898575+00	2126-05-04 01:13:14.898575+00	f	2026-05-04 01:13:14.898575+00	\N	\N	\N	\N
S00000014	U00000032	L00000001	Free	1	1	2026-05-04 01:20:14.768158+00	2126-05-04 01:20:14.768158+00	f	2026-05-04 01:20:14.768158+00	\N	\N	\N	\N
S00000015	U00000033	L00000001	Free	1	1	2026-05-04 01:28:25.934104+00	2126-05-04 01:28:25.934104+00	f	2026-05-04 01:28:25.934104+00	\N	\N	\N	\N
S00000016	U00000034	L00000001	Free	1	1	2026-05-04 01:34:12.109967+00	2126-05-04 01:34:12.109967+00	f	2026-05-04 01:34:12.109967+00	\N	\N	\N	\N
S00000017	U00000035	L00000001	Free	1	1	2026-05-04 01:37:57.557206+00	2126-05-04 01:37:57.557206+00	f	2026-05-04 01:37:57.557206+00	\N	\N	\N	\N
S00000018	U00000036	L00000001	Free	1	1	2026-05-04 04:32:36.165143+00	2126-05-04 04:32:36.165143+00	f	2026-05-04 04:32:36.165143+00	\N	\N	\N	\N
S00000019	U00000037	L00000001	Free	1	1	2026-05-04 04:39:01.43207+00	2126-05-04 04:39:01.43207+00	f	2026-05-04 04:39:01.43207+00	\N	\N	\N	\N
\.


--
-- Data for Name: annotations; Type: TABLE DATA; Schema: traces; Owner: -
--

COPY traces.annotations (id, trace_id, type_id, label, start_position, end_position, created_by, created_at) FROM stdin;
\.


--
-- Data for Name: sequence_edits; Type: TABLE DATA; Schema: traces; Owner: -
--

COPY traces.sequence_edits (id, trace_id, edit_type, "position", original_base, new_base, reason, edited_by, edited_at, is_active) FROM stdin;
\.


--
-- Data for Name: trace_annotations; Type: TABLE DATA; Schema: traces; Owner: -
--

COPY traces.trace_annotations (id, trace_id, type, label, description, start_position, end_position, strand, color, is_shared, metadata, created_at, created_by, modified_at, modified_by) FROM stdin;
\.


--
-- Data for Name: trace_trims; Type: TABLE DATA; Schema: traces; Owner: -
--

COPY traces.trace_trims (id, trim_type, trim_end, start_position, end_position, algorithm, reason, applied_by, applied_at, is_active, trace_id) FROM stdin;
96d52e02-3e4b-44aa-8c17-8d6a38e9b59a	Manual	ThreePrime	745	795	Manual	Prueba	U00000004	2026-04-29 00:17:54.562786+00	f	2a15beae-28e7-4dfe-9d4e-8fc42a13bde2
03062850-240d-40fd-8d17-d8018d92ae72	Manual	FivePrime	735	795	Manual	\N	U00000004	2026-04-29 00:20:33.798041+00	f	2a15beae-28e7-4dfe-9d4e-8fc42a13bde2
684ec366-4520-42a2-92ad-1b0e354d8182	Manual	FivePrime	735	795	Manual	\N	U00000004	2026-04-29 21:55:07.207855+00	f	53d4b8f2-503a-4ba7-8296-ed9cd0e9d076
105de503-c1ef-4974-8083-96cd16c929e3	Manual	FivePrime	20	50	Manual	\N	U00000004	2026-05-07 01:21:27.263717+00	f	f06ea4ee-ae86-4c6b-9d8a-dc44912b9f68
e47f7cee-806e-44cc-9cec-62d63a059905	Manual	FivePrime	756	795	Manual	\N	U00000004	2026-05-07 02:12:08.072319+00	t	582760fc-168c-40cd-948c-f3d2b00ef96b
\.


--
-- Data for Name: traces; Type: TABLE DATA; Schema: traces; Owner: -
--

COPY traces.traces (id, study_id, uploaded_by, name, description, file_name, content_type, storage_path, size_bytes, checksum, format, status, average_quality_score, total_bases, quality_above_q20_percentage, quality_above_q30_percentage, trimmed_length, gc_content_percentage, has_chromatogram_data, failuREDACTED, processed_at, created_at, created_by, modified_at, modified_by, is_deleted, deleted_at, deleted_by) FROM stdin;
6f54f636-8ac0-41c0-982e-409fc677212c	S00000005	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	studies/S00000005/traces/34855d31-38c4-4b10-ba17-38bbadd66cd8.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Uploaded	\N	\N	\N	\N	\N	\N	t	\N	\N	2026-04-21 00:13:43.781733+00	4	\N	\N	f	\N	\N
4bbac531-e304-4922-8017-7a286b66df87	S00000011	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	studies/S00000011/traces/cbff9256-5b92-4b33-8b0f-548709b7d38d.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Uploaded	\N	\N	\N	\N	\N	\N	t	\N	\N	2026-04-22 20:21:38.517738+00	4	\N	\N	t	2026-04-22 20:59:20.764549+00	4
a30d4b3f-13e4-442b-a044-bebd5f90fb07	S00000011	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	studies/S00000011/traces/ebd8dc9a-9f9e-46aa-991e-28e302c0c52b.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Uploaded	\N	\N	\N	\N	\N	\N	t	\N	\N	2026-04-22 20:59:27.124582+00	4	\N	\N	f	\N	\N
1b1a9666-5c63-482c-a11f-274e7bb3897b	S00000006	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	studies/S00000006/traces/253a0ddd-2ccb-4821-9cb7-55b43f09f3f7.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Uploaded	\N	\N	\N	\N	\N	\N	t	\N	\N	2026-04-22 21:21:13.05951+00	4	\N	\N	t	2026-04-22 22:00:03.493987+00	4
8e01a428-c8ae-41d6-bd99-3946acaa82cf	S00000006	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	studies/S00000006/traces/0fe12bfa-9d0a-45e5-9530-18a16505ea89.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Uploaded	\N	\N	\N	\N	\N	\N	t	\N	\N	2026-04-22 22:00:10.308986+00	4	\N	\N	t	2026-04-22 22:07:47.369564+00	4
e43e7df2-c89a-483c-9f47-18d01c097f71	S00000006	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	studies/S00000006/traces/4e493663-280f-4d8c-a1a1-c68c4d5c9f6a.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Uploaded	\N	\N	\N	\N	\N	\N	t	\N	\N	2026-04-22 22:07:53.521629+00	4	\N	\N	t	2026-04-22 22:11:45.419671+00	4
7d65e611-0881-4c21-8fec-8a5b301b5af7	S00000006	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	studies/S00000006/traces/a45a9fac-d284-4851-9e69-351b974679d2.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Uploaded	\N	\N	\N	\N	\N	\N	t	\N	\N	2026-04-22 22:11:52.76073+00	4	\N	\N	t	2026-04-22 22:25:35.0223+00	4
fa8aa976-2781-47d8-a4d4-dcea87467b57	S00000006	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	studies/S00000006/traces/8382cfaa-739c-44cd-9998-559039d47a72.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Uploaded	\N	\N	\N	\N	\N	\N	t	\N	\N	2026-04-22 22:25:41.523621+00	4	\N	\N	t	2026-04-25 19:17:29.523944+00	4
4e2e9243-aa95-469b-8b04-ba56ed9f3e36	S00000006	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	studies/S00000006/traces/b70a657f-2fac-4132-8211-608c499ffdd0.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Uploaded	\N	\N	\N	\N	\N	\N	t	\N	\N	2026-04-25 19:17:36.070256+00	4	\N	\N	t	2026-04-25 19:30:20.918359+00	4
4e13fac9-74fd-4484-bdeb-47bbfd9b94cd	S00000006	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	studies/S00000006/traces/fbdf4394-c325-4108-8dfb-5de09ba0e80b.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Uploaded	\N	\N	\N	\N	\N	\N	t	\N	\N	2026-04-25 19:30:27.094025+00	4	\N	\N	t	2026-04-25 19:34:41.498226+00	4
622bead5-0628-49e8-b204-41475de3a940	S00000006	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	studies/S00000006/traces/7770c7e6-9480-4b40-b047-2d3e52fd50a0.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Uploaded	\N	\N	\N	\N	\N	\N	t	\N	\N	2026-04-25 19:34:46.867894+00	4	\N	\N	t	2026-04-25 19:40:41.22379+00	4
a53dd705-c1c2-4a2e-8bc7-5c4be9dbd1d1	S00000006	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	studies/S00000006/traces/1acf3081-415a-4665-9d59-bba8118b5d2c.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Uploaded	\N	\N	\N	\N	\N	\N	t	\N	\N	2026-04-25 19:40:46.62875+00	4	\N	\N	t	2026-04-25 19:49:17.742211+00	4
eb9856a8-5e62-4cd5-a4ff-9f7fd35f4a71	S00000006	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	studies/S00000006/traces/83550423-da96-4f2e-bb4f-3de24eab37b5.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Uploaded	\N	\N	\N	\N	\N	\N	t	\N	\N	2026-04-25 19:49:23.913776+00	4	\N	\N	t	2026-04-25 19:57:42.457037+00	4
70d981b2-d760-4129-baaa-0583becfbb94	S00000006	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	studies/S00000006/traces/58a31470-2a94-4fc6-9f75-1bee8858491c.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Uploaded	\N	\N	\N	\N	\N	\N	t	\N	\N	2026-04-25 19:57:47.286017+00	4	\N	\N	t	2026-04-25 20:03:46.773784+00	4
0be12a40-fddb-4a8c-bf5d-8f639baf34b0	S00000006	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	studies/S00000006/traces/da92c706-14d1-4a97-9498-89ef7b863c55.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Failed	\N	\N	\N	\N	\N	\N	t	[ValueError] Trace file not found: studies/S00000006/traces/da92c706-14d1-4a97-9498-89ef7b863c55.ab1	\N	2026-04-25 20:03:53.152149+00	4	2026-04-25 20:03:54.186046+00	\N	t	2026-04-25 20:25:01.47757+00	4
5c7a84b5-f26f-4c2a-974c-15052cb28633	S00000006	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	studies/S00000006/traces/da31de51-7559-41a1-b237-3b3427c8af99.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Failed	\N	\N	\N	\N	\N	\N	t	[ValueError] Trace file not found: studies/S00000006/traces/da31de51-7559-41a1-b237-3b3427c8af99.ab1	\N	2026-04-25 20:25:06.884691+00	4	2026-04-25 20:25:07.566025+00	\N	t	2026-04-25 20:41:25.974793+00	4
c128c092-f19a-4e04-ba84-00b902dd537c	S00000006	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	studies/S00000006/traces/649b1bab-50f5-4124-bf51-2fbe2b7d7988.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Failed	\N	\N	\N	\N	\N	\N	t	[ValueError] Trace file not found: studies/S00000006/traces/649b1bab-50f5-4124-bf51-2fbe2b7d7988.ab1	\N	2026-04-25 20:41:31.279649+00	4	2026-04-25 20:41:32.270337+00	\N	t	2026-04-25 20:44:20.979373+00	4
525df105-14f0-427f-8f62-8de00ac49c24	S00000006	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	studies/S00000006/traces/95e4b8b9-6bd2-43d0-9b77-edabc52d71c8.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Processed	46.82	795	80.00	60.00	795	50.00	t	\N	2026-04-25 20:44:26.145414+00	2026-04-25 20:44:25.988417+00	4	2026-04-25 20:44:26.145436+00	\N	t	2026-04-25 20:56:10.400794+00	4
c39e79c3-d99b-4e8b-a235-6e32bda5ce56	S00000006	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	studies/S00000006/traces/b078512a-fe85-4976-82f4-db9e4393fd32.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Failed	\N	\N	\N	\N	\N	\N	t	[ValueError] Trace file not found: studies/S00000006/traces/b078512a-fe85-4976-82f4-db9e4393fd32.ab1	\N	2026-04-25 20:56:15.405654+00	4	2026-04-25 20:56:15.861649+00	\N	t	2026-04-25 21:02:21.768412+00	4
2dba3190-ff19-4106-bd04-a5b239cb4a32	S00000007	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	studies/S00000007/traces/4ee2a046-be6a-47d3-b424-d94fff8a6174.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Failed	\N	\N	\N	\N	\N	\N	t	[ValueError] Trace file not found: studies/S00000007/traces/4ee2a046-be6a-47d3-b424-d94fff8a6174.ab1	\N	2026-04-25 21:13:23.206752+00	4	2026-04-25 21:13:23.881761+00	\N	t	2026-04-25 21:13:44.735209+00	4
8c01800e-3659-40fe-a531-01730bedd36d	S00000055	U00000034	E2E_4f0d90a5	E2E	11_27F.ab1	application/octet-stream	traces/8c01800e-3659-40fe-a531-01730bedd36d/original.ab1	325882	ab9ed4afdee60f5a513aa82f28eca7d011976428900a6f2cede66a461bd3b6a1	AB1	Processed	20.38	1114	80.00	30.00	1114	50.00	t	\N	2026-05-04 01:34:14.725429+00	2026-05-04 01:34:14.523363+00	34	2026-05-04 01:34:14.725429+00	\N	f	\N	\N
53d4b8f2-503a-4ba7-8296-ed9cd0e9d076	S00000019	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	studies/S00000019/traces/873dea41-b0a3-4528-9c01-ed9f88fff4c3.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Processed	46.82	795	80.00	60.00	795	50.00	t	\N	2026-04-29 21:54:56.753643+00	2026-04-29 21:54:56.593124+00	4	2026-04-29 21:56:28.693227+00	4	f	\N	\N
4d996ff1-67cd-40c5-a020-e1653ba1d1c9	S00000052	U00000031	E2E_166b7f9b	E2E	11_27F.ab1	application/octet-stream	traces/4d996ff1-67cd-40c5-a020-e1653ba1d1c9/original.ab1	325882	ab9ed4afdee60f5a513aa82f28eca7d011976428900a6f2cede66a461bd3b6a1	AB1	Processed	20.38	1114	80.00	30.00	1114	50.00	t	\N	2026-05-04 01:13:18.321105+00	2026-05-04 01:13:17.604875+00	31	2026-05-04 01:13:18.321142+00	\N	f	\N	\N
2a15beae-28e7-4dfe-9d4e-8fc42a13bde2	S00000006	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	studies/S00000006/traces/4811da56-f110-4e6d-a335-ec6f536a4f33.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Processed	46.82	795	80.00	60.00	795	50.00	t	\N	2026-04-25 21:02:27.924089+00	2026-04-25 21:02:27.470007+00	4	2026-04-29 00:29:22.067141+00	4	f	\N	\N
d8b94155-9668-4303-8d53-a0dbfbfa0ab6	S00000006	U00000004	example	\N	example.ab1	application/octet-stream	studies/S00000006/traces/945910d6-45c9-40e3-9aa2-49d1a1e78d7a.ab1	271097	1bbaaae7b2fc66c5b2755fefb6273a8bd1def02e24de41d2b549b8d2cd3513f3	AB1	Processed	55.13	425	80.00	60.00	425	50.00	t	\N	2026-04-29 00:36:57.270091+00	2026-04-29 00:36:56.563717+00	4	2026-04-29 00:36:57.270139+00	\N	f	\N	\N
c989100a-c298-4ff6-98ec-6317a6a1920e	S00000053	U00000032	E2E_e34b0835	E2E	11_27F.ab1	application/octet-stream	traces/c989100a-c298-4ff6-98ec-6317a6a1920e/original.ab1	325882	ab9ed4afdee60f5a513aa82f28eca7d011976428900a6f2cede66a461bd3b6a1	AB1	Processed	20.38	1114	80.00	30.00	1114	50.00	t	\N	2026-05-04 01:20:18.506498+00	2026-05-04 01:20:17.889892+00	32	2026-05-04 01:20:18.506536+00	\N	f	\N	\N
d560b3ac-779d-4d34-a737-3d485c486d39	S00000054	U00000033	E2E_93cfeb8e	E2E	11_27F.ab1	application/octet-stream	traces/d560b3ac-779d-4d34-a737-3d485c486d39/original.ab1	325882	ab9ed4afdee60f5a513aa82f28eca7d011976428900a6f2cede66a461bd3b6a1	AB1	Processed	20.38	1114	80.00	30.00	1114	50.00	t	\N	2026-05-04 01:28:29.681703+00	2026-05-04 01:28:29.029968+00	33	2026-05-04 01:28:29.681738+00	\N	f	\N	\N
7922e150-b529-411a-8cc3-095b52c70165	S00000056	U00000035	E2E_b89e90b5	E2E	11_27F.ab1	application/octet-stream	traces/7922e150-b529-411a-8cc3-095b52c70165/original.ab1	325882	ab9ed4afdee60f5a513aa82f28eca7d011976428900a6f2cede66a461bd3b6a1	AB1	Processed	20.38	1114	80.00	30.00	1114	50.00	t	\N	2026-05-04 01:38:00.126631+00	2026-05-04 01:38:00.051809+00	35	2026-05-04 01:38:00.126631+00	\N	f	\N	\N
2bfed79c-84cd-4e7c-8339-817a98185095	S00000057	U00000036	E2E_cf68ca20	E2E	11_27F.ab1	application/octet-stream	traces/2bfed79c-84cd-4e7c-8339-817a98185095/original.ab1	325882	ab9ed4afdee60f5a513aa82f28eca7d011976428900a6f2cede66a461bd3b6a1	AB1	Processed	20.38	1114	80.00	30.00	1114	50.00	t	\N	2026-05-04 04:32:39.25595+00	2026-05-04 04:32:38.651016+00	36	2026-05-04 04:32:39.25595+00	\N	f	\N	\N
6d029961-46ca-41ce-9839-a6d7574971bc	S00000058	U00000037	E2E_8b6a8eea	E2E	11_27F.ab1	application/octet-stream	traces/6d029961-46ca-41ce-9839-a6d7574971bc/original.ab1	325882	ab9ed4afdee60f5a513aa82f28eca7d011976428900a6f2cede66a461bd3b6a1	AB1	Processed	20.38	1114	80.00	30.00	1114	50.00	t	\N	2026-05-04 04:39:04.370322+00	2026-05-04 04:39:03.976344+00	37	2026-05-04 04:39:04.370322+00	\N	f	\N	\N
e1f9c3f0-679e-40ce-afe7-c284b827ba77	S00000009	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	traces/e1f9c3f0-679e-40ce-afe7-c284b827ba77/original.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Processed	46.82	795	80.00	60.00	795	50.00	t	\N	2026-05-05 21:22:13.488029+00	2026-05-05 21:22:13.039931+00	4	2026-05-05 21:22:13.48803+00	\N	f	\N	\N
d92bf671-3dbe-4e07-82b4-4c043158a7cd	S00000009	U00000004	8b8d7f2f-338_20260226_000417	\N	8b8d7f2f-338_20260226_000417.fasta	application/octet-stream	traces/d92bf671-3dbe-4e07-82b4-4c043158a7cd/original.fasta	611	7db08cbe6bebd39dedf17ba0cdd6d711cd5e02156d558d675aeeb01f62a7fe25	FASTA	Processed	0.00	574	50.00	30.00	574	50.00	f	\N	2026-05-05 21:22:13.438551+00	2026-05-05 21:22:12.963781+00	4	2026-05-05 21:22:13.438551+00	\N	t	2026-05-06 20:12:06.980494+00	4
a5590e7b-d427-4dd5-bccc-40d511c1cdcf	S00000009	U00000004	23afad4d-ccf_20260302_022209	\N	23afad4d-ccf_20260302_022209.fasta	application/octet-stream	traces/a5590e7b-d427-4dd5-bccc-40d511c1cdcf/original.fasta	701	c64ce255a0ad9d4ab7c3604432202e04f4761e4a2d9e97d987f1f03088fc5491	FASTA	Processed	0.00	658	50.00	30.00	658	50.00	f	\N	2026-05-05 21:22:13.342249+00	2026-05-05 21:22:12.89092+00	4	2026-05-05 21:22:13.342249+00	\N	t	2026-05-06 20:12:07.46867+00	4
8c7673be-9169-44ec-99a0-9f5c09c4d6cc	S00000009	U00000004	02a3d178-a4f_20260302_000944	\N	02a3d178-a4f_20260302_000944.fasta	application/octet-stream	traces/8c7673be-9169-44ec-99a0-9f5c09c4d6cc/original.fasta	699	84d3647b62511be4b4d83a654eb4ad6fa128a8876311950f58b6a87a977ec028	FASTA	Processed	0.00	658	50.00	30.00	658	50.00	f	\N	2026-05-05 21:22:13.221983+00	2026-05-05 21:22:12.764276+00	4	2026-05-05 21:22:13.222045+00	\N	t	2026-05-06 20:12:07.662761+00	4
2898e93a-4774-4895-9c40-99fe0c1a71ee	S00000007	U00000004	23afad4d-ccf_20260302_022209	\N	23afad4d-ccf_20260302_022209.fasta	application/octet-stream	studies/S00000007/traces/a119607a-ca52-4807-890d-c1572bf70963.fasta	701	c64ce255a0ad9d4ab7c3604432202e04f4761e4a2d9e97d987f1f03088fc5491	FASTA	Failed	\N	\N	\N	\N	\N	\N	f	[ValueError] Trace file not found: studies/S00000007/traces/a119607a-ca52-4807-890d-c1572bf70963.fasta	\N	2026-04-29 22:22:40.239303+00	4	2026-04-29 22:22:40.746436+00	\N	t	2026-05-07 01:19:39.403816+00	4
0e5a4468-183c-47cf-976f-680ed482b702	S00000007	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	studies/S00000007/traces/34b6fce8-78c2-4961-9d56-4feb060b5e69.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Failed	\N	\N	\N	\N	\N	\N	t	[ValueError] Trace file not found: studies/S00000007/traces/34b6fce8-78c2-4961-9d56-4feb060b5e69.ab1	\N	2026-04-25 21:13:49.856821+00	4	2026-04-25 21:13:50.158814+00	\N	t	2026-05-07 01:19:39.736885+00	4
1ec280b3-212c-4a7d-9743-7b578b19bfd1	S00000008	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	traces/1ec280b3-212c-4a7d-9743-7b578b19bfd1/original.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Processed	46.82	795	80.00	60.00	795	50.00	t	\N	2026-04-29 22:30:24.826457+00	2026-04-29 22:30:24.569681+00	4	2026-04-29 22:30:24.826493+00	\N	t	2026-05-07 02:05:44.336392+00	4
8e4ea606-d2b0-4459-a775-1b644e4e5517	S00000008	U00000004	02a3d178-a4f_20260302_000944	\N	02a3d178-a4f_20260302_000944.fasta	application/octet-stream	traces/8e4ea606-d2b0-4459-a775-1b644e4e5517/original.fasta	699	84d3647b62511be4b4d83a654eb4ad6fa128a8876311950f58b6a87a977ec028	FASTA	Failed	\N	\N	\N	\N	\N	\N	f	[ValueError] Trace file not found: traces/8e4ea606-d2b0-4459-a775-1b644e4e5517/original.fasta	\N	2026-04-29 22:29:57.774631+00	4	2026-04-29 22:29:58.50338+00	\N	t	2026-05-07 02:05:44.927571+00	4
e97775dd-294e-4e22-bfc9-b354c4fdc0e1	S00000009	U00000004	8b8d7f2f-338_20260226_000417	\N	8b8d7f2f-338_20260226_000417.fasta	application/octet-stream	traces/e97775dd-294e-4e22-bfc9-b354c4fdc0e1/original.fasta	611	7db08cbe6bebd39dedf17ba0cdd6d711cd5e02156d558d675aeeb01f62a7fe25	FASTA	Processed	0.00	574	50.00	30.00	574	50.00	f	\N	2026-05-06 20:12:14.763579+00	2026-05-06 20:12:14.01158+00	4	2026-05-06 20:12:14.76358+00	\N	t	2026-05-06 20:25:21.387196+00	4
2c551a69-0b78-49c4-ac00-cb1e686b9b0a	S00000009	U00000004	23afad4d-ccf_20260302_022209	\N	23afad4d-ccf_20260302_022209.fasta	application/octet-stream	traces/2c551a69-0b78-49c4-ac00-cb1e686b9b0a/original.fasta	701	c64ce255a0ad9d4ab7c3604432202e04f4761e4a2d9e97d987f1f03088fc5491	FASTA	Processed	0.00	658	50.00	30.00	658	50.00	f	\N	2026-05-06 20:12:14.755072+00	2026-05-06 20:12:13.953139+00	4	2026-05-06 20:12:14.755073+00	\N	t	2026-05-06 20:25:21.744677+00	4
c558df05-33f4-4074-a8da-5defe98e1347	S00000009	U00000004	02a3d178-a4f_20260302_000944	\N	02a3d178-a4f_20260302_000944.fasta	application/octet-stream	traces/c558df05-33f4-4074-a8da-5defe98e1347/original.fasta	699	84d3647b62511be4b4d83a654eb4ad6fa128a8876311950f58b6a87a977ec028	FASTA	Processed	0.00	658	50.00	30.00	658	50.00	f	\N	2026-05-06 20:12:14.742106+00	2026-05-06 20:12:13.872524+00	4	2026-05-06 20:12:14.742144+00	\N	t	2026-05-06 20:25:22.088659+00	4
656cb79a-c7c2-4ef9-af43-aa90507919ad	S00000009	U00000004	02a3d178-a4f_20260302_000944	\N	02a3d178-a4f_20260302_000944.fasta	application/octet-stream	traces/656cb79a-c7c2-4ef9-af43-aa90507919ad/original.fasta	699	84d3647b62511be4b4d83a654eb4ad6fa128a8876311950f58b6a87a977ec028	FASTA	Processed	0.00	658	50.00	30.00	658	50.00	t	\N	2026-05-06 20:25:30.419146+00	2026-05-06 20:25:29.927115+00	4	2026-05-06 20:25:30.419146+00	\N	f	\N	\N
9f067d5a-dacf-4ad7-a7a6-406385b20fbe	S00000009	U00000004	23afad4d-ccf_20260302_022209	\N	23afad4d-ccf_20260302_022209.fasta	application/octet-stream	traces/9f067d5a-dacf-4ad7-a7a6-406385b20fbe/original.fasta	701	c64ce255a0ad9d4ab7c3604432202e04f4761e4a2d9e97d987f1f03088fc5491	FASTA	Processed	0.00	658	50.00	30.00	658	50.00	t	\N	2026-05-06 20:25:30.434482+00	2026-05-06 20:25:29.959337+00	4	2026-05-06 20:25:30.434483+00	\N	f	\N	\N
f06ea4ee-ae86-4c6b-9d8a-dc44912b9f68	S00000007	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	traces/f06ea4ee-ae86-4c6b-9d8a-dc44912b9f68/original.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Processed	46.82	795	80.00	60.00	795	50.00	t	\N	2026-05-07 01:19:48.036288+00	2026-05-07 01:19:47.259232+00	4	2026-05-07 01:21:49.020971+00	4	f	\N	\N
582760fc-168c-40cd-948c-f3d2b00ef96b	S00000008	U00000004	b1d92c8e-4945-497d-8b24-001d64e41c7f	\N	b1d92c8e-4945-497d-8b24-001d64e41c7f.ab1	application/octet-stream	traces/582760fc-168c-40cd-948c-f3d2b00ef96b/original.ab1	209224	78588d824dd967a1e58a9667a8f7ca1687ba38266420bbc5a6562c1893888efd	AB1	Processed	46.82	795	80.00	60.00	795	50.00	t	\N	2026-05-07 02:05:52.752784+00	2026-05-07 02:05:51.649575+00	4	2026-05-07 02:12:08.072352+00	4	f	\N	\N
e4ba8e19-3122-4635-969e-d2461562d506	S00000009	U00000004	8b8d7f2f-338_20260226_000417	\N	8b8d7f2f-338_20260226_000417.fasta	application/octet-stream	traces/e4ba8e19-3122-4635-969e-d2461562d506/original.fasta	611	7db08cbe6bebd39dedf17ba0cdd6d711cd5e02156d558d675aeeb01f62a7fe25	FASTA	Processed	0.00	574	50.00	30.00	574	50.00	t	\N	2026-05-06 20:25:30.444662+00	2026-05-06 20:25:30.019628+00	4	2026-05-06 20:25:30.444662+00	\N	t	2026-05-07 03:01:49.780731+00	4
974f333a-2672-4589-b1d4-54b44b126be2	S00000009	U00000004	8b8d7f2f-338_20260226_000417	\N	8b8d7f2f-338_20260226_000417.fasta	application/octet-stream	traces/974f333a-2672-4589-b1d4-54b44b126be2/original.fasta	611	7db08cbe6bebd39dedf17ba0cdd6d711cd5e02156d558d675aeeb01f62a7fe25	FASTA	Processed	0.00	574	50.00	30.00	574	50.00	t	\N	2026-05-07 03:01:58.162268+00	2026-05-07 03:01:57.845778+00	4	2026-05-07 03:01:58.162268+00	\N	t	2026-05-07 03:14:05.675346+00	4
8d5194e7-1113-490d-ad29-5d32cc780fb8	S00000009	U00000004	8b8d7f2f-338_20260226_000417	\N	8b8d7f2f-338_20260226_000417.fasta	application/octet-stream	traces/8d5194e7-1113-490d-ad29-5d32cc780fb8/original.fasta	611	7db08cbe6bebd39dedf17ba0cdd6d711cd5e02156d558d675aeeb01f62a7fe25	FASTA	Processed	0.00	574	50.00	30.00	574	50.00	t	\N	2026-05-07 03:14:14.103908+00	2026-05-07 03:14:13.293948+00	4	2026-05-07 03:14:14.103964+00	\N	t	2026-05-07 03:18:49.994846+00	4
b4cb57d9-03cc-40be-aa4f-7e27994a0545	S00000009	U00000004	8b8d7f2f-338_20260226_000417	\N	8b8d7f2f-338_20260226_000417.fasta	application/octet-stream	traces/b4cb57d9-03cc-40be-aa4f-7e27994a0545/original.fasta	611	7db08cbe6bebd39dedf17ba0cdd6d711cd5e02156d558d675aeeb01f62a7fe25	FASTA	Processed	0.00	574	50.00	30.00	574	50.00	t	\N	2026-05-07 03:18:57.264011+00	2026-05-07 03:18:56.486094+00	4	2026-05-07 03:18:57.264011+00	\N	t	2026-05-07 03:21:42.578224+00	4
da263bca-6b63-4eb9-8203-4b2d4a851e2e	S00000009	U00000004	8b8d7f2f-338_20260226_000417	\N	8b8d7f2f-338_20260226_000417.fasta	application/octet-stream	traces/da263bca-6b63-4eb9-8203-4b2d4a851e2e/original.fasta	611	7db08cbe6bebd39dedf17ba0cdd6d711cd5e02156d558d675aeeb01f62a7fe25	FASTA	Processed	0.00	574	50.00	30.00	574	50.00	t	\N	2026-05-07 03:21:51.217016+00	2026-05-07 03:21:50.448757+00	4	2026-05-07 03:21:51.217016+00	\N	t	2026-05-07 03:22:42.352574+00	4
3d9b83c6-a4a9-4e4c-a2e8-941ecd5e5883	S00000009	U00000004	02a3d178-a4f_20260302_000944	\N	02a3d178-a4f_20260302_000944.fasta	application/octet-stream	traces/3d9b83c6-a4a9-4e4c-a2e8-941ecd5e5883/original.fasta	699	84d3647b62511be4b4d83a654eb4ad6fa128a8876311950f58b6a87a977ec028	FASTA	Processed	0.00	658	50.00	30.00	658	50.00	t	\N	2026-05-07 03:22:53.130247+00	2026-05-07 03:22:52.068927+00	4	2026-05-07 03:22:53.130291+00	\N	t	2026-05-07 03:28:22.071837+00	4
b2a99f20-89db-4132-b453-542cdc41b966	S00000009	U00000004	8b8d7f2f-338_20260226_000417	\N	8b8d7f2f-338_20260226_000417.fasta	application/octet-stream	traces/b2a99f20-89db-4132-b453-542cdc41b966/original.fasta	611	7db08cbe6bebd39dedf17ba0cdd6d711cd5e02156d558d675aeeb01f62a7fe25	FASTA	Processed	0.00	574	50.00	30.00	574	50.00	t	\N	2026-05-07 03:28:29.89395+00	2026-05-07 03:28:28.855707+00	4	2026-05-07 03:28:29.893993+00	\N	t	2026-05-07 03:28:43.335349+00	4
3a910c67-0d73-4484-a912-e2829d643ddd	S00000009	U00000004	8b8d7f2f-338_20260226_000417	\N	8b8d7f2f-338_20260226_000417.fasta	application/octet-stream	traces/3a910c67-0d73-4484-a912-e2829d643ddd/original.fasta	611	7db08cbe6bebd39dedf17ba0cdd6d711cd5e02156d558d675aeeb01f62a7fe25	FASTA	Processed	0.00	574	50.00	30.00	574	50.00	t	\N	2026-05-07 03:28:52.165157+00	2026-05-07 03:28:51.935332+00	4	2026-05-07 03:28:52.165158+00	\N	t	2026-05-07 03:31:57.698184+00	4
7a495120-d469-4a51-a60b-ca0a0a9f32bc	S00000009	U00000004	8b8d7f2f-338_20260226_000417	\N	8b8d7f2f-338_20260226_000417.fasta	application/octet-stream	traces/7a495120-d469-4a51-a60b-ca0a0a9f32bc/original.fasta	611	7db08cbe6bebd39dedf17ba0cdd6d711cd5e02156d558d675aeeb01f62a7fe25	FASTA	Processed	0.00	574	50.00	30.00	574	50.00	t	\N	2026-05-07 03:32:04.879209+00	2026-05-07 03:32:03.976977+00	4	2026-05-07 03:32:04.879252+00	\N	t	2026-05-07 03:34:05.220147+00	4
55f3b040-0c50-4f81-9291-3234d4625497	S00000009	U00000004	8b8d7f2f-338_20260226_000417	\N	8b8d7f2f-338_20260226_000417.fasta	application/octet-stream	traces/55f3b040-0c50-4f81-9291-3234d4625497/original.fasta	611	7db08cbe6bebd39dedf17ba0cdd6d711cd5e02156d558d675aeeb01f62a7fe25	FASTA	Processed	0.00	574	50.00	30.00	574	50.00	t	\N	2026-05-07 03:34:12.342121+00	2026-05-07 03:34:12.304852+00	4	2026-05-07 03:34:12.342121+00	\N	t	2026-05-07 03:34:34.405695+00	4
660b36dd-9806-427e-8226-17630e76a698	S00000009	U00000004	8b8d7f2f-338_20260226_000417	\N	8b8d7f2f-338_20260226_000417.fasta	application/octet-stream	traces/660b36dd-9806-427e-8226-17630e76a698/original.fasta	611	7db08cbe6bebd39dedf17ba0cdd6d711cd5e02156d558d675aeeb01f62a7fe25	FASTA	Processed	0.00	574	50.00	30.00	574	50.00	t	\N	2026-05-07 03:34:40.663452+00	2026-05-07 03:34:39.862386+00	4	2026-05-07 03:34:40.663453+00	\N	t	2026-05-07 03:36:19.5751+00	4
75af6287-23e6-4ce5-9880-226d5df48420	S00000009	U00000004	8b8d7f2f-338_20260226_000417	\N	8b8d7f2f-338_20260226_000417.fasta	application/octet-stream	traces/75af6287-23e6-4ce5-9880-226d5df48420/original.fasta	611	7db08cbe6bebd39dedf17ba0cdd6d711cd5e02156d558d675aeeb01f62a7fe25	FASTA	Processed	0.00	574	50.00	30.00	574	50.00	t	\N	2026-05-07 03:36:26.123438+00	2026-05-07 03:36:25.23394+00	4	2026-05-07 03:36:26.123472+00	\N	t	2026-05-07 03:38:49.314315+00	4
f045c76c-1f62-4a33-9d53-dd9e52c3b718	S00000009	U00000004	8b8d7f2f-338_20260226_000417	\N	8b8d7f2f-338_20260226_000417.fasta	application/octet-stream	traces/f045c76c-1f62-4a33-9d53-dd9e52c3b718/original.fasta	611	7db08cbe6bebd39dedf17ba0cdd6d711cd5e02156d558d675aeeb01f62a7fe25	FASTA	Processed	0.00	574	50.00	30.00	574	50.00	t	\N	2026-05-07 03:38:55.795951+00	2026-05-07 03:38:55.094643+00	4	2026-05-07 03:38:55.795952+00	\N	t	2026-05-07 03:39:28.782126+00	4
4bf6ac2e-7a0e-4f2b-acb2-7fcfe63bd171	S00000009	U00000004	8b8d7f2f-338_20260226_000417	\N	8b8d7f2f-338_20260226_000417.fasta	application/octet-stream	traces/4bf6ac2e-7a0e-4f2b-acb2-7fcfe63bd171/original.fasta	611	7db08cbe6bebd39dedf17ba0cdd6d711cd5e02156d558d675aeeb01f62a7fe25	FASTA	Processed	0.00	574	50.00	30.00	574	50.00	t	\N	2026-05-07 03:39:37.251726+00	2026-05-07 03:39:36.923177+00	4	2026-05-07 03:39:37.251726+00	\N	t	2026-05-07 03:39:52.858514+00	4
bb5ecf95-20be-4b63-9329-7c40dfed794b	S00000009	U00000004	8b8d7f2f-338_20260226_000417	\N	8b8d7f2f-338_20260226_000417.fasta	application/octet-stream	traces/bb5ecf95-20be-4b63-9329-7c40dfed794b/original.fasta	611	7db08cbe6bebd39dedf17ba0cdd6d711cd5e02156d558d675aeeb01f62a7fe25	FASTA	Processed	0.00	574	50.00	30.00	574	50.00	t	\N	2026-05-07 03:40:02.571748+00	2026-05-07 03:40:01.571906+00	4	2026-05-07 03:40:02.571748+00	\N	t	2026-05-07 03:43:46.668721+00	4
c5032806-aa89-4f95-85fd-bafb9e0993af	S00000009	U00000004	8b8d7f2f-338_20260226_000417	\N	8b8d7f2f-338_20260226_000417.fasta	application/octet-stream	traces/c5032806-aa89-4f95-85fd-bafb9e0993af/original.fasta	611	7db08cbe6bebd39dedf17ba0cdd6d711cd5e02156d558d675aeeb01f62a7fe25	FASTA	Processed	0.00	574	50.00	30.00	574	50.00	t	\N	2026-05-07 03:45:41.229406+00	2026-05-07 03:45:40.495533+00	4	2026-05-07 03:45:41.229407+00	\N	t	2026-05-07 03:52:12.502891+00	4
d2557d7a-5819-4a58-ad64-09a6e153ec98	S00000009	U00000004	8b8d7f2f-338_20260226_000417	\N	8b8d7f2f-338_20260226_000417.fasta	application/octet-stream	traces/d2557d7a-5819-4a58-ad64-09a6e153ec98/original.fasta	611	7db08cbe6bebd39dedf17ba0cdd6d711cd5e02156d558d675aeeb01f62a7fe25	FASTA	Processed	0.00	574	50.00	30.00	574	50.00	t	\N	2026-05-07 03:43:53.078905+00	2026-05-07 03:43:52.768352+00	4	2026-05-07 03:43:53.078905+00	\N	t	2026-05-07 03:45:33.93152+00	4
c6052c8e-8231-48cc-879b-44f84acd5367	S00000009	U00000004	8b8d7f2f-338_20260226_000417	\N	8b8d7f2f-338_20260226_000417.fasta	application/octet-stream	traces/c6052c8e-8231-48cc-879b-44f84acd5367/original.fasta	611	7db08cbe6bebd39dedf17ba0cdd6d711cd5e02156d558d675aeeb01f62a7fe25	FASTA	Processed	0.00	574	50.00	30.00	574	50.00	t	\N	2026-05-07 03:52:19.48461+00	2026-05-07 03:52:18.754722+00	4	2026-05-07 03:52:19.484665+00	\N	t	2026-05-07 03:52:30.529139+00	4
983bed97-30e1-40d4-8f71-d5143cf39743	S00000009	U00000004	8b8d7f2f-338_20260226_000417	\N	8b8d7f2f-338_20260226_000417.fasta	application/octet-stream	traces/983bed97-30e1-40d4-8f71-d5143cf39743/original.fasta	611	7db08cbe6bebd39dedf17ba0cdd6d711cd5e02156d558d675aeeb01f62a7fe25	FASTA	Processed	0.00	574	50.00	30.00	574	50.00	t	\N	2026-05-07 03:52:38.756805+00	2026-05-07 03:52:38.104641+00	4	2026-05-07 03:52:38.756805+00	\N	t	2026-05-07 03:54:53.202235+00	4
6a92e647-0bd2-4286-8cc9-d1a1b012f473	S00000009	U00000004	8b8d7f2f-338_20260226_000417	\N	8b8d7f2f-338_20260226_000417.fasta	application/octet-stream	traces/6a92e647-0bd2-4286-8cc9-d1a1b012f473/original.fasta	611	7db08cbe6bebd39dedf17ba0cdd6d711cd5e02156d558d675aeeb01f62a7fe25	FASTA	Processed	0.00	574	50.00	30.00	574	50.00	t	\N	2026-05-07 03:54:59.304791+00	2026-05-07 03:54:58.335722+00	4	2026-05-07 03:54:59.304791+00	\N	t	2026-05-07 03:59:29.957604+00	4
d548f4e5-e3e7-49d8-8cc9-37c547c3a30e	S00000009	U00000004	8b8d7f2f-338_20260226_000417	\N	8b8d7f2f-338_20260226_000417.fasta	application/octet-stream	traces/d548f4e5-e3e7-49d8-8cc9-37c547c3a30e/original.fasta	611	7db08cbe6bebd39dedf17ba0cdd6d711cd5e02156d558d675aeeb01f62a7fe25	FASTA	Processed	0.00	574	50.00	30.00	574	50.00	t	\N	2026-05-07 03:59:36.915815+00	2026-05-07 03:59:36.247923+00	4	2026-05-07 03:59:36.915849+00	\N	f	\N	\N
\.


--
-- Name: alignment_traces_Id_seq; Type: SEQUENCE SET; Schema: alignments; Owner: -
--

SELECT pg_catalog.setval('alignments."alignment_traces_Id_seq"', 1, false);


--
-- Name: plan_features_id_seq; Type: SEQUENCE SET; Schema: billing; Owner: -
--

SELECT pg_catalog.setval('billing.plan_features_id_seq', 1, false);


--
-- Name: plan_features_id_seq; Type: SEQUENCE SET; Schema: identity; Owner: -
--

SELECT pg_catalog.setval('identity.plan_features_id_seq', 1, false);


--
-- Name: study_views_id_seq; Type: SEQUENCE SET; Schema: identity; Owner: -
--

SELECT pg_catalog.setval('identity.study_views_id_seq', 1, false);


--
-- Name: payment_events_log_id_seq; Type: SEQUENCE SET; Schema: payments; Owner: -
--

SELECT pg_catalog.setval('payments.payment_events_log_id_seq', 1, false);


--
-- Name: plan_features_id_seq; Type: SEQUENCE SET; Schema: plans; Owner: -
--

SELECT pg_catalog.setval('plans.plan_features_id_seq', 9, true);


--
-- Name: study_views_id_seq; Type: SEQUENCE SET; Schema: studies; Owner: -
--

SELECT pg_catalog.setval('studies.study_views_id_seq', 59, true);


--
-- Name: alignment_traces alignment_traces_pkey; Type: CONSTRAINT; Schema: alignments; Owner: -
--

ALTER TABLE ONLY alignments.alignment_traces
    ADD CONSTRAINT alignment_traces_pkey PRIMARY KEY ("Id");


--
-- Name: alignments alignments_pkey; Type: CONSTRAINT; Schema: alignments; Owner: -
--

ALTER TABLE ONLY alignments.alignments
    ADD CONSTRAINT alignments_pkey PRIMARY KEY (id);


--
-- Name: payment_methods pk_payment_methods; Type: CONSTRAINT; Schema: billing; Owner: -
--

ALTER TABLE ONLY billing.payment_methods
    ADD CONSTRAINT pk_payment_methods PRIMARY KEY (id);


--
-- Name: plan_features plan_features_pkey; Type: CONSTRAINT; Schema: billing; Owner: -
--

ALTER TABLE ONLY billing.plan_features
    ADD CONSTRAINT plan_features_pkey PRIMARY KEY (id);


--
-- Name: plans plans_name_key; Type: CONSTRAINT; Schema: billing; Owner: -
--

ALTER TABLE ONLY billing.plans
    ADD CONSTRAINT plans_name_key UNIQUE (name);


--
-- Name: plans plans_pkey; Type: CONSTRAINT; Schema: billing; Owner: -
--

ALTER TABLE ONLY billing.plans
    ADD CONSTRAINT plans_pkey PRIMARY KEY (id);


--
-- Name: subscriptions subscriptions_pkey; Type: CONSTRAINT; Schema: billing; Owner: -
--

ALTER TABLE ONLY billing.subscriptions
    ADD CONSTRAINT subscriptions_pkey PRIMARY KEY (id);


--
-- Name: external_logins PK_external_logins; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.external_logins
    ADD CONSTRAINT "PK_external_logins" PRIMARY KEY (id);


--
-- Name: pipeline_executions PK_pipeline_executions; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.pipeline_executions
    ADD CONSTRAINT "PK_pipeline_executions" PRIMARY KEY (id);


--
-- Name: pipeline_step_executions PK_pipeline_step_executions; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.pipeline_step_executions
    ADD CONSTRAINT "PK_pipeline_step_executions" PRIMARY KEY (id);


--
-- Name: pipeline_steps PK_pipeline_steps; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.pipeline_steps
    ADD CONSTRAINT "PK_pipeline_steps" PRIMARY KEY (id);


--
-- Name: pipelines PK_pipelines; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.pipelines
    ADD CONSTRAINT "PK_pipelines" PRIMARY KEY (id);


--
-- Name: plan_features PK_plan_features; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.plan_features
    ADD CONSTRAINT "PK_plan_features" PRIMARY KEY (id);


--
-- Name: plans PK_plans; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.plans
    ADD CONSTRAINT "PK_plans" PRIMARY KEY (id);


--
-- Name: profiles PK_profiles; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.profiles
    ADD CONSTRAINT "PK_profiles" PRIMARY KEY (id);


--
-- Name: refresh_tokens PK_refresh_tokens; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.refresh_tokens
    ADD CONSTRAINT "PK_refresh_tokens" PRIMARY KEY (token);


--
-- Name: sequence_edits PK_sequence_edits; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.sequence_edits
    ADD CONSTRAINT "PK_sequence_edits" PRIMARY KEY (id);


--
-- Name: studies PK_studies; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.studies
    ADD CONSTRAINT "PK_studies" PRIMARY KEY (id);


--
-- Name: study_invitations PK_study_invitations; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.study_invitations
    ADD CONSTRAINT "PK_study_invitations" PRIMARY KEY (id);


--
-- Name: study_members PK_study_members; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.study_members
    ADD CONSTRAINT "PK_study_members" PRIMARY KEY (id);


--
-- Name: study_papers PK_study_papers; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.study_papers
    ADD CONSTRAINT "PK_study_papers" PRIMARY KEY (id);


--
-- Name: study_stars PK_study_stars; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.study_stars
    ADD CONSTRAINT "PK_study_stars" PRIMARY KEY (id);


--
-- Name: study_views PK_study_views; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.study_views
    ADD CONSTRAINT "PK_study_views" PRIMARY KEY (id);


--
-- Name: subscriptions PK_subscriptions; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.subscriptions
    ADD CONSTRAINT "PK_subscriptions" PRIMARY KEY (id);


--
-- Name: trace_annotations PK_trace_annotations; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.trace_annotations
    ADD CONSTRAINT "PK_trace_annotations" PRIMARY KEY (id);


--
-- Name: traces PK_traces; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.traces
    ADD CONSTRAINT "PK_traces" PRIMARY KEY (id);


--
-- Name: two_factor_codes PK_two_factor_codes; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.two_factor_codes
    ADD CONSTRAINT "PK_two_factor_codes" PRIMARY KEY (id);


--
-- Name: users PK_users; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.users
    ADD CONSTRAINT "PK_users" PRIMARY KEY (id);


--
-- Name: payment_events_log payment_events_log_pkey; Type: CONSTRAINT; Schema: payments; Owner: -
--

ALTER TABLE ONLY payments.payment_events_log
    ADD CONSTRAINT payment_events_log_pkey PRIMARY KEY (id);


--
-- Name: payment_methods payment_methods_pkey; Type: CONSTRAINT; Schema: payments; Owner: -
--

ALTER TABLE ONLY payments.payment_methods
    ADD CONSTRAINT payment_methods_pkey PRIMARY KEY (id);


--
-- Name: stripe_customers stripe_customers_pkey; Type: CONSTRAINT; Schema: payments; Owner: -
--

ALTER TABLE ONLY payments.stripe_customers
    ADD CONSTRAINT stripe_customers_pkey PRIMARY KEY (user_id);


--
-- Name: stripe_customers stripe_customers_stripe_customer_id_key; Type: CONSTRAINT; Schema: payments; Owner: -
--

ALTER TABLE ONLY payments.stripe_customers
    ADD CONSTRAINT stripe_customers_stripe_customer_id_key UNIQUE (stripe_customer_id);


--
-- Name: pipeline_executions PK_pipeline_executions; Type: CONSTRAINT; Schema: pipelines; Owner: -
--

ALTER TABLE ONLY pipelines.pipeline_executions
    ADD CONSTRAINT "PK_pipeline_executions" PRIMARY KEY (id);


--
-- Name: pipeline_step_executions PK_pipeline_step_executions; Type: CONSTRAINT; Schema: pipelines; Owner: -
--

ALTER TABLE ONLY pipelines.pipeline_step_executions
    ADD CONSTRAINT "PK_pipeline_step_executions" PRIMARY KEY (id);


--
-- Name: pipeline_steps PK_pipeline_steps; Type: CONSTRAINT; Schema: pipelines; Owner: -
--

ALTER TABLE ONLY pipelines.pipeline_steps
    ADD CONSTRAINT "PK_pipeline_steps" PRIMARY KEY (id);


--
-- Name: pipelines PK_pipelines; Type: CONSTRAINT; Schema: pipelines; Owner: -
--

ALTER TABLE ONLY pipelines.pipelines
    ADD CONSTRAINT "PK_pipelines" PRIMARY KEY (id);


--
-- Name: plan_features PK_plan_features; Type: CONSTRAINT; Schema: plans; Owner: -
--

ALTER TABLE ONLY plans.plan_features
    ADD CONSTRAINT "PK_plan_features" PRIMARY KEY (id);


--
-- Name: plans PK_plans; Type: CONSTRAINT; Schema: plans; Owner: -
--

ALTER TABLE ONLY plans.plans
    ADD CONSTRAINT "PK_plans" PRIMARY KEY (id);


--
-- Name: profiles PK_profiles; Type: CONSTRAINT; Schema: profiles; Owner: -
--

ALTER TABLE ONLY profiles.profiles
    ADD CONSTRAINT "PK_profiles" PRIMARY KEY (id);


--
-- Name: __EFMigrationsHistory PK___EFMigrationsHistory; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."__EFMigrationsHistory"
    ADD CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId");


--
-- Name: invitations invitations_pkey; Type: CONSTRAINT; Schema: studies; Owner: -
--

ALTER TABLE ONLY studies.invitations
    ADD CONSTRAINT invitations_pkey PRIMARY KEY (id);


--
-- Name: members members_pkey; Type: CONSTRAINT; Schema: studies; Owner: -
--

ALTER TABLE ONLY studies.members
    ADD CONSTRAINT members_pkey PRIMARY KEY (study_id, user_id);


--
-- Name: studies studies_pkey; Type: CONSTRAINT; Schema: studies; Owner: -
--

ALTER TABLE ONLY studies.studies
    ADD CONSTRAINT studies_pkey PRIMARY KEY (id);


--
-- Name: study_invitations study_invitations_pkey; Type: CONSTRAINT; Schema: studies; Owner: -
--

ALTER TABLE ONLY studies.study_invitations
    ADD CONSTRAINT study_invitations_pkey PRIMARY KEY (id);


--
-- Name: study_invitations study_invitations_token_key; Type: CONSTRAINT; Schema: studies; Owner: -
--

ALTER TABLE ONLY studies.study_invitations
    ADD CONSTRAINT study_invitations_token_key UNIQUE (token);


--
-- Name: study_members study_members_pkey; Type: CONSTRAINT; Schema: studies; Owner: -
--

ALTER TABLE ONLY studies.study_members
    ADD CONSTRAINT study_members_pkey PRIMARY KEY (id);


--
-- Name: study_members study_members_study_id_user_id_key; Type: CONSTRAINT; Schema: studies; Owner: -
--

ALTER TABLE ONLY studies.study_members
    ADD CONSTRAINT study_members_study_id_user_id_key UNIQUE (study_id, user_id);


--
-- Name: study_papers study_papers_pkey; Type: CONSTRAINT; Schema: studies; Owner: -
--

ALTER TABLE ONLY studies.study_papers
    ADD CONSTRAINT study_papers_pkey PRIMARY KEY (id);


--
-- Name: study_stars study_stars_pkey; Type: CONSTRAINT; Schema: studies; Owner: -
--

ALTER TABLE ONLY studies.study_stars
    ADD CONSTRAINT study_stars_pkey PRIMARY KEY (id);


--
-- Name: study_stars study_stars_study_id_user_id_key; Type: CONSTRAINT; Schema: studies; Owner: -
--

ALTER TABLE ONLY studies.study_stars
    ADD CONSTRAINT study_stars_study_id_user_id_key UNIQUE (study_id, user_id);


--
-- Name: study_views study_views_pkey; Type: CONSTRAINT; Schema: studies; Owner: -
--

ALTER TABLE ONLY studies.study_views
    ADD CONSTRAINT study_views_pkey PRIMARY KEY (id);


--
-- Name: subscriptions PK_subscriptions; Type: CONSTRAINT; Schema: subscriptions; Owner: -
--

ALTER TABLE ONLY subscriptions.subscriptions
    ADD CONSTRAINT "PK_subscriptions" PRIMARY KEY (id);


--
-- Name: trace_trims PK_trace_trims; Type: CONSTRAINT; Schema: traces; Owner: -
--

ALTER TABLE ONLY traces.trace_trims
    ADD CONSTRAINT "PK_trace_trims" PRIMARY KEY (id);


--
-- Name: annotations annotations_pkey; Type: CONSTRAINT; Schema: traces; Owner: -
--

ALTER TABLE ONLY traces.annotations
    ADD CONSTRAINT annotations_pkey PRIMARY KEY (id);


--
-- Name: sequence_edits pk_sequence_edits; Type: CONSTRAINT; Schema: traces; Owner: -
--

ALTER TABLE ONLY traces.sequence_edits
    ADD CONSTRAINT pk_sequence_edits PRIMARY KEY (id);


--
-- Name: trace_annotations pk_trace_annotations; Type: CONSTRAINT; Schema: traces; Owner: -
--

ALTER TABLE ONLY traces.trace_annotations
    ADD CONSTRAINT pk_trace_annotations PRIMARY KEY (id);


--
-- Name: traces pk_traces; Type: CONSTRAINT; Schema: traces; Owner: -
--

ALTER TABLE ONLY traces.traces
    ADD CONSTRAINT pk_traces PRIMARY KEY (id);


--
-- Name: idx_alignment_traces_alignment; Type: INDEX; Schema: alignments; Owner: -
--

CREATE INDEX idx_alignment_traces_alignment ON alignments.alignment_traces USING btree (alignment_id);


--
-- Name: idx_alignment_traces_trace; Type: INDEX; Schema: alignments; Owner: -
--

CREATE INDEX idx_alignment_traces_trace ON alignments.alignment_traces USING btree (trace_id);


--
-- Name: idx_alignments_status; Type: INDEX; Schema: alignments; Owner: -
--

CREATE INDEX idx_alignments_status ON alignments.alignments USING btree (status_id);


--
-- Name: idx_alignments_study; Type: INDEX; Schema: alignments; Owner: -
--

CREATE INDEX idx_alignments_study ON alignments.alignments USING btree (study_id);


--
-- Name: idx_alignments_study_status; Type: INDEX; Schema: alignments; Owner: -
--

CREATE INDEX idx_alignments_study_status ON alignments.alignments USING btree (study_id, status_id);


--
-- Name: ix_alignments_pending_queue; Type: INDEX; Schema: alignments; Owner: -
--

CREATE INDEX ix_alignments_pending_queue ON alignments.alignments USING btree (status_id, created_at);


--
-- Name: idx_plan_features_plan; Type: INDEX; Schema: billing; Owner: -
--

CREATE INDEX idx_plan_features_plan ON billing.plan_features USING btree (plan_id);


--
-- Name: idx_plans_display_order; Type: INDEX; Schema: billing; Owner: -
--

CREATE INDEX idx_plans_display_order ON billing.plans USING btree (display_order);


--
-- Name: idx_plans_is_active; Type: INDEX; Schema: billing; Owner: -
--

CREATE INDEX idx_plans_is_active ON billing.plans USING btree (is_active);


--
-- Name: idx_plans_is_default; Type: INDEX; Schema: billing; Owner: -
--

CREATE INDEX idx_plans_is_default ON billing.plans USING btree (is_default);


--
-- Name: idx_subscriptions_plan; Type: INDEX; Schema: billing; Owner: -
--

CREATE INDEX idx_subscriptions_plan ON billing.subscriptions USING btree (plan_id);


--
-- Name: idx_subscriptions_status; Type: INDEX; Schema: billing; Owner: -
--

CREATE INDEX idx_subscriptions_status ON billing.subscriptions USING btree (status);


--
-- Name: idx_subscriptions_user; Type: INDEX; Schema: billing; Owner: -
--

CREATE INDEX idx_subscriptions_user ON billing.subscriptions USING btree (user_id);


--
-- Name: idx_subscriptions_user_status; Type: INDEX; Schema: billing; Owner: -
--

CREATE INDEX idx_subscriptions_user_status ON billing.subscriptions USING btree (user_id, status);


--
-- Name: ix_payment_methods_stripe_id; Type: INDEX; Schema: billing; Owner: -
--

CREATE UNIQUE INDEX ix_payment_methods_stripe_id ON billing.payment_methods USING btree (stripe_payment_method_id);


--
-- Name: ix_payment_methods_user_default; Type: INDEX; Schema: billing; Owner: -
--

CREATE INDEX ix_payment_methods_user_default ON billing.payment_methods USING btree (user_id, is_default);


--
-- Name: ix_payment_methods_user_id; Type: INDEX; Schema: billing; Owner: -
--

CREATE INDEX ix_payment_methods_user_id ON billing.payment_methods USING btree (user_id);


--
-- Name: IX_external_logins_UserId; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_external_logins_UserId" ON identity.external_logins USING btree ("UserId");


--
-- Name: IX_external_logins_provider_provider_key; Type: INDEX; Schema: identity; Owner: -
--

CREATE UNIQUE INDEX "IX_external_logins_provider_provider_key" ON identity.external_logins USING btree (provider, provider_key);


--
-- Name: IX_pipeline_executions_pipeline_id; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_pipeline_executions_pipeline_id" ON identity.pipeline_executions USING btree (pipeline_id);


--
-- Name: IX_pipeline_executions_status; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_pipeline_executions_status" ON identity.pipeline_executions USING btree (status);


--
-- Name: IX_pipeline_executions_trace_id; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_pipeline_executions_trace_id" ON identity.pipeline_executions USING btree (trace_id);


--
-- Name: IX_pipeline_executions_trace_id_status; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_pipeline_executions_trace_id_status" ON identity.pipeline_executions USING btree (trace_id, status);


--
-- Name: IX_pipeline_step_executions_execution_id_order; Type: INDEX; Schema: identity; Owner: -
--

CREATE UNIQUE INDEX "IX_pipeline_step_executions_execution_id_order" ON identity.pipeline_step_executions USING btree (execution_id, "order");


--
-- Name: IX_pipeline_steps_pipeline_id_order; Type: INDEX; Schema: identity; Owner: -
--

CREATE UNIQUE INDEX "IX_pipeline_steps_pipeline_id_order" ON identity.pipeline_steps USING btree (pipeline_id, "order");


--
-- Name: IX_pipelines_name; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_pipelines_name" ON identity.pipelines USING btree (name);


--
-- Name: IX_pipelines_owner_id; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_pipelines_owner_id" ON identity.pipelines USING btree (owner_id);


--
-- Name: IX_pipelines_status; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_pipelines_status" ON identity.pipelines USING btree (status);


--
-- Name: IX_pipelines_study_id; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_pipelines_study_id" ON identity.pipelines USING btree (study_id);


--
-- Name: IX_plan_features_plan_id; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_plan_features_plan_id" ON identity.plan_features USING btree (plan_id);


--
-- Name: IX_plans_display_order; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_plans_display_order" ON identity.plans USING btree (display_order);


--
-- Name: IX_plans_is_active; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_plans_is_active" ON identity.plans USING btree (is_active);


--
-- Name: IX_plans_is_default; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_plans_is_default" ON identity.plans USING btree (is_default);


--
-- Name: IX_plans_name; Type: INDEX; Schema: identity; Owner: -
--

CREATE UNIQUE INDEX "IX_plans_name" ON identity.plans USING btree (name);


--
-- Name: IX_profiles_research_field; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_profiles_research_field" ON identity.profiles USING btree (research_field);


--
-- Name: IX_profiles_user_id; Type: INDEX; Schema: identity; Owner: -
--

CREATE UNIQUE INDEX "IX_profiles_user_id" ON identity.profiles USING btree (user_id);


--
-- Name: IX_refresh_tokens_UserId; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_refresh_tokens_UserId" ON identity.refresh_tokens USING btree ("UserId");


--
-- Name: IX_refresh_tokens_token; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_refresh_tokens_token" ON identity.refresh_tokens USING btree (token);


--
-- Name: IX_sequence_edits_trace_id_position; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_sequence_edits_trace_id_position" ON identity.sequence_edits USING btree (trace_id, "position");


--
-- Name: IX_studies_is_featured; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_studies_is_featured" ON identity.studies USING btree (is_featured);


--
-- Name: IX_studies_owner_id; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_studies_owner_id" ON identity.studies USING btree (owner_id);


--
-- Name: IX_studies_research_field; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_studies_research_field" ON identity.studies USING btree (research_field);


--
-- Name: IX_studies_status; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_studies_status" ON identity.studies USING btree (status);


--
-- Name: IX_study_invitations_email; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_study_invitations_email" ON identity.study_invitations USING btree (email);


--
-- Name: IX_study_invitations_status; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_study_invitations_status" ON identity.study_invitations USING btree (status);


--
-- Name: IX_study_invitations_study_id; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_study_invitations_study_id" ON identity.study_invitations USING btree (study_id);


--
-- Name: IX_study_invitations_study_id_email; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_study_invitations_study_id_email" ON identity.study_invitations USING btree (study_id, email);


--
-- Name: IX_study_invitations_token; Type: INDEX; Schema: identity; Owner: -
--

CREATE UNIQUE INDEX "IX_study_invitations_token" ON identity.study_invitations USING btree (token);


--
-- Name: IX_study_members_study_id_user_id; Type: INDEX; Schema: identity; Owner: -
--

CREATE UNIQUE INDEX "IX_study_members_study_id_user_id" ON identity.study_members USING btree (study_id, user_id);


--
-- Name: IX_study_papers_study_id; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_study_papers_study_id" ON identity.study_papers USING btree (study_id);


--
-- Name: IX_study_stars_study_id; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_study_stars_study_id" ON identity.study_stars USING btree (study_id);


--
-- Name: IX_study_stars_study_id_user_id; Type: INDEX; Schema: identity; Owner: -
--

CREATE UNIQUE INDEX "IX_study_stars_study_id_user_id" ON identity.study_stars USING btree (study_id, user_id);


--
-- Name: IX_study_stars_user_id; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_study_stars_user_id" ON identity.study_stars USING btree (user_id);


--
-- Name: IX_study_views_study_id; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_study_views_study_id" ON identity.study_views USING btree (study_id);


--
-- Name: IX_study_views_study_id_ip_hash_viewed_at; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_study_views_study_id_ip_hash_viewed_at" ON identity.study_views USING btree (study_id, ip_hash, viewed_at);


--
-- Name: IX_study_views_study_id_viewed_at; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_study_views_study_id_viewed_at" ON identity.study_views USING btree (study_id, viewed_at);


--
-- Name: IX_subscriptions_plan_id; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_subscriptions_plan_id" ON identity.subscriptions USING btree (plan_id);


--
-- Name: IX_subscriptions_status; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_subscriptions_status" ON identity.subscriptions USING btree (status);


--
-- Name: IX_subscriptions_user_id; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_subscriptions_user_id" ON identity.subscriptions USING btree (user_id);


--
-- Name: IX_subscriptions_user_id_status; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_subscriptions_user_id_status" ON identity.subscriptions USING btree (user_id, status);


--
-- Name: IX_trace_annotations_trace_id_is_shared; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_trace_annotations_trace_id_is_shared" ON identity.trace_annotations USING btree (trace_id, is_shared);


--
-- Name: IX_trace_annotations_trace_id_start_position_end_position; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_trace_annotations_trace_id_start_position_end_position" ON identity.trace_annotations USING btree (trace_id, start_position, end_position);


--
-- Name: IX_traces_format; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_traces_format" ON identity.traces USING btree (format);


--
-- Name: IX_traces_status; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_traces_status" ON identity.traces USING btree (status);


--
-- Name: IX_traces_study_id; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_traces_study_id" ON identity.traces USING btree (study_id);


--
-- Name: IX_two_factor_codes_UserId; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_two_factor_codes_UserId" ON identity.two_factor_codes USING btree ("UserId");


--
-- Name: IX_users_email; Type: INDEX; Schema: identity; Owner: -
--

CREATE UNIQUE INDEX "IX_users_email" ON identity.users USING btree (email);


--
-- Name: IX_users_username; Type: INDEX; Schema: identity; Owner: -
--

CREATE UNIQUE INDEX "IX_users_username" ON identity.users USING btree (username);


--
-- Name: idx_external_logins_provider; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX idx_external_logins_provider ON identity.external_logins USING btree (provider, provider_key);


--
-- Name: idx_external_logins_user; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX idx_external_logins_user ON identity.external_logins USING btree ("UserId");


--
-- Name: idx_refresh_tokens_active; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX idx_refresh_tokens_active ON identity.refresh_tokens USING btree ("UserId", is_revoked) WHERE (is_revoked = false);


--
-- Name: idx_refresh_tokens_token; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX idx_refresh_tokens_token ON identity.refresh_tokens USING btree (token);


--
-- Name: idx_refresh_tokens_user; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX idx_refresh_tokens_user ON identity.refresh_tokens USING btree ("UserId");


--
-- Name: idx_two_factor_codes_expires; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX idx_two_factor_codes_expires ON identity.two_factor_codes USING btree (expires_at);


--
-- Name: idx_two_factor_codes_user; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX idx_two_factor_codes_user ON identity.two_factor_codes USING btree ("UserId");


--
-- Name: idx_users_email; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX idx_users_email ON identity.users USING btree (email);


--
-- Name: idx_users_is_active; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX idx_users_is_active ON identity.users USING btree (is_active) WHERE (is_active = true);


--
-- Name: idx_users_username; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX idx_users_username ON identity.users USING btree (username);


--
-- Name: idx_payment_events_pm; Type: INDEX; Schema: payments; Owner: -
--

CREATE INDEX idx_payment_events_pm ON payments.payment_events_log USING btree (payment_method_id) WHERE (payment_method_id IS NOT NULL);


--
-- Name: idx_payment_events_type; Type: INDEX; Schema: payments; Owner: -
--

CREATE INDEX idx_payment_events_type ON payments.payment_events_log USING btree (event_type);


--
-- Name: idx_payment_events_user; Type: INDEX; Schema: payments; Owner: -
--

CREATE INDEX idx_payment_events_user ON payments.payment_events_log USING btree (user_id);


--
-- Name: idx_payment_methods_status; Type: INDEX; Schema: payments; Owner: -
--

CREATE INDEX idx_payment_methods_status ON payments.payment_methods USING btree (status);


--
-- Name: idx_payment_methods_stripe_pm_id; Type: INDEX; Schema: payments; Owner: -
--

CREATE UNIQUE INDEX idx_payment_methods_stripe_pm_id ON payments.payment_methods USING btree (stripe_payment_method_id);


--
-- Name: idx_payment_methods_user_default; Type: INDEX; Schema: payments; Owner: -
--

CREATE INDEX idx_payment_methods_user_default ON payments.payment_methods USING btree (user_id, is_default) WHERE (is_default = true);


--
-- Name: idx_payment_methods_user_id; Type: INDEX; Schema: payments; Owner: -
--

CREATE INDEX idx_payment_methods_user_id ON payments.payment_methods USING btree (user_id);


--
-- Name: IX_pipeline_executions_pipeline_id; Type: INDEX; Schema: pipelines; Owner: -
--

CREATE INDEX "IX_pipeline_executions_pipeline_id" ON pipelines.pipeline_executions USING btree (pipeline_id);


--
-- Name: IX_pipeline_executions_status; Type: INDEX; Schema: pipelines; Owner: -
--

CREATE INDEX "IX_pipeline_executions_status" ON pipelines.pipeline_executions USING btree (status);


--
-- Name: IX_pipeline_executions_trace_id; Type: INDEX; Schema: pipelines; Owner: -
--

CREATE INDEX "IX_pipeline_executions_trace_id" ON pipelines.pipeline_executions USING btree (trace_id);


--
-- Name: IX_pipeline_executions_trace_id_status; Type: INDEX; Schema: pipelines; Owner: -
--

CREATE INDEX "IX_pipeline_executions_trace_id_status" ON pipelines.pipeline_executions USING btree (trace_id, status);


--
-- Name: IX_pipeline_step_executions_execution_id_order; Type: INDEX; Schema: pipelines; Owner: -
--

CREATE UNIQUE INDEX "IX_pipeline_step_executions_execution_id_order" ON pipelines.pipeline_step_executions USING btree (execution_id, "order");


--
-- Name: IX_pipeline_steps_pipeline_id_order; Type: INDEX; Schema: pipelines; Owner: -
--

CREATE UNIQUE INDEX "IX_pipeline_steps_pipeline_id_order" ON pipelines.pipeline_steps USING btree (pipeline_id, "order");


--
-- Name: IX_pipelines_name; Type: INDEX; Schema: pipelines; Owner: -
--

CREATE INDEX "IX_pipelines_name" ON pipelines.pipelines USING btree (name);


--
-- Name: IX_pipelines_owner_id; Type: INDEX; Schema: pipelines; Owner: -
--

CREATE INDEX "IX_pipelines_owner_id" ON pipelines.pipelines USING btree (owner_id);


--
-- Name: IX_pipelines_status; Type: INDEX; Schema: pipelines; Owner: -
--

CREATE INDEX "IX_pipelines_status" ON pipelines.pipelines USING btree (status);


--
-- Name: IX_pipelines_study_id; Type: INDEX; Schema: pipelines; Owner: -
--

CREATE INDEX "IX_pipelines_study_id" ON pipelines.pipelines USING btree (study_id);


--
-- Name: IX_plan_features_plan_id; Type: INDEX; Schema: plans; Owner: -
--

CREATE INDEX "IX_plan_features_plan_id" ON plans.plan_features USING btree (plan_id);


--
-- Name: IX_plans_display_order; Type: INDEX; Schema: plans; Owner: -
--

CREATE INDEX "IX_plans_display_order" ON plans.plans USING btree (display_order);


--
-- Name: IX_plans_is_active; Type: INDEX; Schema: plans; Owner: -
--

CREATE INDEX "IX_plans_is_active" ON plans.plans USING btree (is_active);


--
-- Name: IX_plans_is_default; Type: INDEX; Schema: plans; Owner: -
--

CREATE INDEX "IX_plans_is_default" ON plans.plans USING btree (is_default);


--
-- Name: IX_plans_name; Type: INDEX; Schema: plans; Owner: -
--

CREATE UNIQUE INDEX "IX_plans_name" ON plans.plans USING btree (name);


--
-- Name: IX_profiles_research_field; Type: INDEX; Schema: profiles; Owner: -
--

CREATE INDEX "IX_profiles_research_field" ON profiles.profiles USING btree (research_field);


--
-- Name: IX_profiles_user_id; Type: INDEX; Schema: profiles; Owner: -
--

CREATE UNIQUE INDEX "IX_profiles_user_id" ON profiles.profiles USING btree (user_id);


--
-- Name: idx_profiles_institution; Type: INDEX; Schema: profiles; Owner: -
--

CREATE INDEX idx_profiles_institution ON profiles.profiles USING btree (institution_name) WHERE (institution_name IS NOT NULL);


--
-- Name: idx_profiles_orcid; Type: INDEX; Schema: profiles; Owner: -
--

CREATE INDEX idx_profiles_orcid ON profiles.profiles USING btree (orcid_id) WHERE (orcid_id IS NOT NULL);


--
-- Name: idx_profiles_research_field; Type: INDEX; Schema: profiles; Owner: -
--

CREATE INDEX idx_profiles_research_field ON profiles.profiles USING btree (research_field) WHERE (research_field IS NOT NULL);


--
-- Name: idx_profiles_user_id; Type: INDEX; Schema: profiles; Owner: -
--

CREATE INDEX idx_profiles_user_id ON profiles.profiles USING btree (user_id);


--
-- Name: idx_studies_is_featured; Type: INDEX; Schema: studies; Owner: -
--

CREATE INDEX idx_studies_is_featured ON studies.studies USING btree (is_featured);


--
-- Name: idx_studies_owner_id; Type: INDEX; Schema: studies; Owner: -
--

CREATE INDEX idx_studies_owner_id ON studies.studies USING btree (owner_id);


--
-- Name: idx_studies_research_field; Type: INDEX; Schema: studies; Owner: -
--

CREATE INDEX idx_studies_research_field ON studies.studies USING btree (research_field);


--
-- Name: idx_studies_status; Type: INDEX; Schema: studies; Owner: -
--

CREATE INDEX idx_studies_status ON studies.studies USING btree (status);


--
-- Name: idx_study_invitations_email; Type: INDEX; Schema: studies; Owner: -
--

CREATE INDEX idx_study_invitations_email ON studies.study_invitations USING btree (email);


--
-- Name: idx_study_invitations_status; Type: INDEX; Schema: studies; Owner: -
--

CREATE INDEX idx_study_invitations_status ON studies.study_invitations USING btree (status);


--
-- Name: idx_study_invitations_study_id; Type: INDEX; Schema: studies; Owner: -
--

CREATE INDEX idx_study_invitations_study_id ON studies.study_invitations USING btree (study_id);


--
-- Name: idx_study_invitations_token; Type: INDEX; Schema: studies; Owner: -
--

CREATE INDEX idx_study_invitations_token ON studies.study_invitations USING btree (token);


--
-- Name: idx_study_members_study_id; Type: INDEX; Schema: studies; Owner: -
--

CREATE INDEX idx_study_members_study_id ON studies.study_members USING btree (study_id);


--
-- Name: idx_study_members_user_id; Type: INDEX; Schema: studies; Owner: -
--

CREATE INDEX idx_study_members_user_id ON studies.study_members USING btree (user_id);


--
-- Name: idx_study_papers_study_id; Type: INDEX; Schema: studies; Owner: -
--

CREATE INDEX idx_study_papers_study_id ON studies.study_papers USING btree (study_id);


--
-- Name: idx_study_stars_study_id; Type: INDEX; Schema: studies; Owner: -
--

CREATE INDEX idx_study_stars_study_id ON studies.study_stars USING btree (study_id);


--
-- Name: idx_study_stars_user_id; Type: INDEX; Schema: studies; Owner: -
--

CREATE INDEX idx_study_stars_user_id ON studies.study_stars USING btree (user_id);


--
-- Name: idx_study_views_dedup; Type: INDEX; Schema: studies; Owner: -
--

CREATE INDEX idx_study_views_dedup ON studies.study_views USING btree (study_id, ip_hash, viewed_at);


--
-- Name: idx_study_views_study_id; Type: INDEX; Schema: studies; Owner: -
--

CREATE INDEX idx_study_views_study_id ON studies.study_views USING btree (study_id);


--
-- Name: idx_study_views_study_viewed; Type: INDEX; Schema: studies; Owner: -
--

CREATE INDEX idx_study_views_study_viewed ON studies.study_views USING btree (study_id, viewed_at);


--
-- Name: IX_subscriptions_plan_id; Type: INDEX; Schema: subscriptions; Owner: -
--

CREATE INDEX "IX_subscriptions_plan_id" ON subscriptions.subscriptions USING btree (plan_id);


--
-- Name: IX_subscriptions_status; Type: INDEX; Schema: subscriptions; Owner: -
--

CREATE INDEX "IX_subscriptions_status" ON subscriptions.subscriptions USING btree (status);


--
-- Name: IX_subscriptions_user_id; Type: INDEX; Schema: subscriptions; Owner: -
--

CREATE INDEX "IX_subscriptions_user_id" ON subscriptions.subscriptions USING btree (user_id);


--
-- Name: IX_subscriptions_user_id_status; Type: INDEX; Schema: subscriptions; Owner: -
--

CREATE INDEX "IX_subscriptions_user_id_status" ON subscriptions.subscriptions USING btree (user_id, status);


--
-- Name: IX_trace_trims_trace_id_start_position; Type: INDEX; Schema: traces; Owner: -
--

CREATE INDEX "IX_trace_trims_trace_id_start_position" ON traces.trace_trims USING btree (trace_id, start_position);


--
-- Name: ix_sequence_edits_trace_id_position; Type: INDEX; Schema: traces; Owner: -
--

CREATE INDEX ix_sequence_edits_trace_id_position ON traces.sequence_edits USING btree (trace_id, "position");


--
-- Name: ix_trace_annotations_trace_id_is_shared; Type: INDEX; Schema: traces; Owner: -
--

CREATE INDEX ix_trace_annotations_trace_id_is_shared ON traces.trace_annotations USING btree (trace_id, is_shared);


--
-- Name: ix_trace_annotations_trace_id_positions; Type: INDEX; Schema: traces; Owner: -
--

CREATE INDEX ix_trace_annotations_trace_id_positions ON traces.trace_annotations USING btree (trace_id, start_position, end_position);


--
-- Name: ix_traces_format; Type: INDEX; Schema: traces; Owner: -
--

CREATE INDEX ix_traces_format ON traces.traces USING btree (format);


--
-- Name: ix_traces_status; Type: INDEX; Schema: traces; Owner: -
--

CREATE INDEX ix_traces_status ON traces.traces USING btree (status);


--
-- Name: ix_traces_study_id; Type: INDEX; Schema: traces; Owner: -
--

CREATE INDEX ix_traces_study_id ON traces.traces USING btree (study_id);


--
-- Name: alignment_traces alignment_traces_alignment_id_fkey; Type: FK CONSTRAINT; Schema: alignments; Owner: -
--

ALTER TABLE ONLY alignments.alignment_traces
    ADD CONSTRAINT alignment_traces_alignment_id_fkey FOREIGN KEY (alignment_id) REFERENCES alignments.alignments(id) ON DELETE CASCADE;


--
-- Name: plan_features plan_features_plan_id_fkey; Type: FK CONSTRAINT; Schema: billing; Owner: -
--

ALTER TABLE ONLY billing.plan_features
    ADD CONSTRAINT plan_features_plan_id_fkey FOREIGN KEY (plan_id) REFERENCES billing.plans(id) ON DELETE CASCADE;


--
-- Name: external_logins FK_external_logins_users_UserId; Type: FK CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.external_logins
    ADD CONSTRAINT "FK_external_logins_users_UserId" FOREIGN KEY ("UserId") REFERENCES identity.users(id) ON DELETE CASCADE;


--
-- Name: pipeline_step_executions FK_pipeline_step_executions_pipeline_executions_execution_id; Type: FK CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.pipeline_step_executions
    ADD CONSTRAINT "FK_pipeline_step_executions_pipeline_executions_execution_id" FOREIGN KEY (execution_id) REFERENCES identity.pipeline_executions(id);


--
-- Name: pipeline_steps FK_pipeline_steps_pipelines_pipeline_id; Type: FK CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.pipeline_steps
    ADD CONSTRAINT "FK_pipeline_steps_pipelines_pipeline_id" FOREIGN KEY (pipeline_id) REFERENCES identity.pipelines(id);


--
-- Name: plan_features FK_plan_features_plans_plan_id; Type: FK CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.plan_features
    ADD CONSTRAINT "FK_plan_features_plans_plan_id" FOREIGN KEY (plan_id) REFERENCES identity.plans(id) ON DELETE CASCADE;


--
-- Name: refresh_tokens FK_refresh_tokens_users_UserId; Type: FK CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.refresh_tokens
    ADD CONSTRAINT "FK_refresh_tokens_users_UserId" FOREIGN KEY ("UserId") REFERENCES identity.users(id) ON DELETE CASCADE;


--
-- Name: sequence_edits FK_sequence_edits_traces_trace_id; Type: FK CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.sequence_edits
    ADD CONSTRAINT "FK_sequence_edits_traces_trace_id" FOREIGN KEY (trace_id) REFERENCES identity.traces(id);


--
-- Name: study_members FK_study_members_studies_study_id; Type: FK CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.study_members
    ADD CONSTRAINT "FK_study_members_studies_study_id" FOREIGN KEY (study_id) REFERENCES identity.studies(id);


--
-- Name: study_papers FK_study_papers_studies_study_id; Type: FK CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.study_papers
    ADD CONSTRAINT "FK_study_papers_studies_study_id" FOREIGN KEY (study_id) REFERENCES identity.studies(id);


--
-- Name: trace_annotations FK_trace_annotations_traces_trace_id; Type: FK CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.trace_annotations
    ADD CONSTRAINT "FK_trace_annotations_traces_trace_id" FOREIGN KEY (trace_id) REFERENCES identity.traces(id);


--
-- Name: two_factor_codes FK_two_factor_codes_users_UserId; Type: FK CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.two_factor_codes
    ADD CONSTRAINT "FK_two_factor_codes_users_UserId" FOREIGN KEY ("UserId") REFERENCES identity.users(id) ON DELETE CASCADE;


--
-- Name: pipeline_step_executions FK_pipeline_step_executions_pipeline_executions_execution_id; Type: FK CONSTRAINT; Schema: pipelines; Owner: -
--

ALTER TABLE ONLY pipelines.pipeline_step_executions
    ADD CONSTRAINT "FK_pipeline_step_executions_pipeline_executions_execution_id" FOREIGN KEY (execution_id) REFERENCES pipelines.pipeline_executions(id);


--
-- Name: pipeline_steps FK_pipeline_steps_pipelines_pipeline_id; Type: FK CONSTRAINT; Schema: pipelines; Owner: -
--

ALTER TABLE ONLY pipelines.pipeline_steps
    ADD CONSTRAINT "FK_pipeline_steps_pipelines_pipeline_id" FOREIGN KEY (pipeline_id) REFERENCES pipelines.pipelines(id);


--
-- Name: plan_features FK_plan_features_plans_plan_id; Type: FK CONSTRAINT; Schema: plans; Owner: -
--

ALTER TABLE ONLY plans.plan_features
    ADD CONSTRAINT "FK_plan_features_plans_plan_id" FOREIGN KEY (plan_id) REFERENCES plans.plans(id) ON DELETE CASCADE;


--
-- Name: study_invitations study_invitations_study_id_fkey; Type: FK CONSTRAINT; Schema: studies; Owner: -
--

ALTER TABLE ONLY studies.study_invitations
    ADD CONSTRAINT study_invitations_study_id_fkey FOREIGN KEY (study_id) REFERENCES studies.studies(id) ON DELETE CASCADE;


--
-- Name: study_members study_members_study_id_fkey; Type: FK CONSTRAINT; Schema: studies; Owner: -
--

ALTER TABLE ONLY studies.study_members
    ADD CONSTRAINT study_members_study_id_fkey FOREIGN KEY (study_id) REFERENCES studies.studies(id) ON DELETE CASCADE;


--
-- Name: study_papers study_papers_study_id_fkey; Type: FK CONSTRAINT; Schema: studies; Owner: -
--

ALTER TABLE ONLY studies.study_papers
    ADD CONSTRAINT study_papers_study_id_fkey FOREIGN KEY (study_id) REFERENCES studies.studies(id) ON DELETE CASCADE;


--
-- Name: study_stars study_stars_study_id_fkey; Type: FK CONSTRAINT; Schema: studies; Owner: -
--

ALTER TABLE ONLY studies.study_stars
    ADD CONSTRAINT study_stars_study_id_fkey FOREIGN KEY (study_id) REFERENCES studies.studies(id) ON DELETE CASCADE;


--
-- Name: study_views study_views_study_id_fkey; Type: FK CONSTRAINT; Schema: studies; Owner: -
--

ALTER TABLE ONLY studies.study_views
    ADD CONSTRAINT study_views_study_id_fkey FOREIGN KEY (study_id) REFERENCES studies.studies(id) ON DELETE CASCADE;


--
-- Name: trace_trims FK_trace_trims_traces_trace_id; Type: FK CONSTRAINT; Schema: traces; Owner: -
--

ALTER TABLE ONLY traces.trace_trims
    ADD CONSTRAINT "FK_trace_trims_traces_trace_id" FOREIGN KEY (trace_id) REFERENCES traces.traces(id) ON DELETE CASCADE;


--
-- Name: sequence_edits fk_sequence_edits_traces_trace_id; Type: FK CONSTRAINT; Schema: traces; Owner: -
--

ALTER TABLE ONLY traces.sequence_edits
    ADD CONSTRAINT fk_sequence_edits_traces_trace_id FOREIGN KEY (trace_id) REFERENCES traces.traces(id) ON DELETE CASCADE;


--
-- Name: trace_annotations fk_trace_annotations_traces_trace_id; Type: FK CONSTRAINT; Schema: traces; Owner: -
--

ALTER TABLE ONLY traces.trace_annotations
    ADD CONSTRAINT fk_trace_annotations_traces_trace_id FOREIGN KEY (trace_id) REFERENCES traces.traces(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict oUJzyqhmB8QHp17rvbnpV6nFp3zDoNgHAbbEVHtf9kjbTRqhcmZ6O8jfzSmzq9s

