using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GeneFlow.ApiNet2.Infrastructure.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPipelines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pipeline_executions",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    pipeline_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    trace_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    started_by = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    total_steps = table.Column<int>(type: "integer", nullable: false),
                    completed_steps = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pipeline_executions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pipelines",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    study_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    owner_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pipelines", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "plans",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    monthly_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    annual_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    max_studies = table.Column<int>(type: "integer", nullable: false),
                    max_traces_per_month = table.Column<int>(type: "integer", nullable: false),
                    max_members_per_study = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "profiles",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    user_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    bio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    professional_role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    institution_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    institution_department = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    research_field = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    orcid_id = table.Column<string>(type: "character varying(19)", maxLength: 19, nullable: true),
                    website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    photo_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    photo_thumbnail_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    photo_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "studies",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    owner_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    research_field = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    allow_public_comments = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    allow_data_download = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    requiREDACTED = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    views_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    stars_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    institution = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    principal_investigator = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    tags = table.Column<string[]>(type: "text[]", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_studies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "study_invitations",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    study_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    invited_by = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_study_invitations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "study_stars",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    study_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    user_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    starred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_study_stars", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "study_views",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    study_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    user_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ip_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    viewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_study_views", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    user_id = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    plan_id = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    plan_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    billing_cycle = table.Column<int>(type: "integer", nullable: false),
                    period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    auto_renew = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    trial_end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "traces",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    study_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    uploaded_by = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    storage_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    checksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    format = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    average_quality_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    total_bases = table.Column<int>(type: "integer", nullable: true),
                    quality_above_q20_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    quality_above_q30_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    trimmed_length = table.Column<int>(type: "integer", nullable: true),
                    gc_content_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    trim_start_5_prime = table.Column<int>(type: "integer", nullable: true),
                    trim_end_5_prime = table.Column<int>(type: "integer", nullable: true),
                    trim_start_3_prime = table.Column<int>(type: "integer", nullable: true),
                    trim_end_3_prime = table.Column<int>(type: "integer", nullable: true),
                    trim_algorithm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    trimmed_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    trimmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    has_chromatogram_data = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    failuREDACTED = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_traces", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pipeline_step_executions",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pipeline_step_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    step_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    result_summary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    result_data = table.Column<string>(type: "jsonb", nullable: true),
                    execution_id = table.Column<string>(type: "character varying(10)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pipeline_step_executions", x => x.id);
                    table.ForeignKey(
                        name: "FK_pipeline_step_executions_pipeline_executions_execution_id",
                        column: x => x.execution_id,
                        principalSchema: "identity",
                        principalTable: "pipeline_executions",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "pipeline_steps",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    configuration = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false, defaultValue: "{}"),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    pipeline_id = table.Column<string>(type: "character varying(10)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pipeline_steps", x => x.id);
                    table.ForeignKey(
                        name: "FK_pipeline_steps_pipelines_pipeline_id",
                        column: x => x.pipeline_id,
                        principalSchema: "identity",
                        principalTable: "pipelines",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "plan_features",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    plan_id = table.Column<string>(type: "character varying(9)", nullable: false),
                    featuREDACTED = table.Column<int>(type: "integer", nullable: false),
                    featuREDACTED = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_features", x => x.id);
                    table.ForeignKey(
                        name: "FK_plan_features_plans_plan_id",
                        column: x => x.plan_id,
                        principalSchema: "identity",
                        principalTable: "plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "study_members",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    invited_by = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    study_id = table.Column<string>(type: "character varying(10)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_study_members", x => x.id);
                    table.ForeignKey(
                        name: "FK_study_members_studies_study_id",
                        column: x => x.study_id,
                        principalSchema: "identity",
                        principalTable: "studies",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "study_papers",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    authors = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    doi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    @abstract = table.Column<string>(name: "abstract", type: "character varying(5000)", maxLength: 5000, nullable: true),
                    journal = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    publication_year = table.Column<int>(type: "integer", nullable: true),
                    file_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    study_id = table.Column<string>(type: "character varying(10)", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_study_papers", x => x.id);
                    table.ForeignKey(
                        name: "FK_study_papers_studies_study_id",
                        column: x => x.study_id,
                        principalSchema: "identity",
                        principalTable: "studies",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "sequence_edits",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    edit_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    original_base = table.Column<char>(type: "character(1)", maxLength: 1, nullable: true),
                    new_base = table.Column<char>(type: "character(1)", maxLength: 1, nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    edited_by = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    edited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    trace_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sequence_edits", x => x.id);
                    table.ForeignKey(
                        name: "FK_sequence_edits_traces_trace_id",
                        column: x => x.trace_id,
                        principalSchema: "identity",
                        principalTable: "traces",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "trace_annotations",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    start_position = table.Column<int>(type: "integer", nullable: false),
                    end_position = table.Column<int>(type: "integer", nullable: false),
                    strand = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_shared = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    metadata = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    trace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trace_annotations", x => x.id);
                    table.ForeignKey(
                        name: "FK_trace_annotations_traces_trace_id",
                        column: x => x.trace_id,
                        principalSchema: "identity",
                        principalTable: "traces",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_pipeline_executions_pipeline_id",
                schema: "identity",
                table: "pipeline_executions",
                column: "pipeline_id");

            migrationBuilder.CreateIndex(
                name: "IX_pipeline_executions_status",
                schema: "identity",
                table: "pipeline_executions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_pipeline_executions_trace_id",
                schema: "identity",
                table: "pipeline_executions",
                column: "trace_id");

            migrationBuilder.CreateIndex(
                name: "IX_pipeline_executions_trace_id_status",
                schema: "identity",
                table: "pipeline_executions",
                columns: new[] { "trace_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_pipeline_step_executions_execution_id_order",
                schema: "identity",
                table: "pipeline_step_executions",
                columns: new[] { "execution_id", "order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pipeline_steps_pipeline_id_order",
                schema: "identity",
                table: "pipeline_steps",
                columns: new[] { "pipeline_id", "order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pipelines_name",
                schema: "identity",
                table: "pipelines",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_pipelines_owner_id",
                schema: "identity",
                table: "pipelines",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_pipelines_status",
                schema: "identity",
                table: "pipelines",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_pipelines_study_id",
                schema: "identity",
                table: "pipelines",
                column: "study_id");

            migrationBuilder.CreateIndex(
                name: "IX_plan_features_plan_id",
                schema: "identity",
                table: "plan_features",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_plans_display_order",
                schema: "identity",
                table: "plans",
                column: "display_order");

            migrationBuilder.CreateIndex(
                name: "IX_plans_is_active",
                schema: "identity",
                table: "plans",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_plans_is_default",
                schema: "identity",
                table: "plans",
                column: "is_default");

            migrationBuilder.CreateIndex(
                name: "IX_plans_name",
                schema: "identity",
                table: "plans",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_profiles_research_field",
                schema: "identity",
                table: "profiles",
                column: "research_field");

            migrationBuilder.CreateIndex(
                name: "IX_profiles_user_id",
                schema: "identity",
                table: "profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sequence_edits_trace_id_position",
                schema: "identity",
                table: "sequence_edits",
                columns: new[] { "trace_id", "position" });

            migrationBuilder.CreateIndex(
                name: "IX_studies_is_featured",
                schema: "identity",
                table: "studies",
                column: "is_featured");

            migrationBuilder.CreateIndex(
                name: "IX_studies_owner_id",
                schema: "identity",
                table: "studies",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_studies_research_field",
                schema: "identity",
                table: "studies",
                column: "research_field");

            migrationBuilder.CreateIndex(
                name: "IX_studies_status",
                schema: "identity",
                table: "studies",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_study_invitations_email",
                schema: "identity",
                table: "study_invitations",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_study_invitations_status",
                schema: "identity",
                table: "study_invitations",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_study_invitations_study_id",
                schema: "identity",
                table: "study_invitations",
                column: "study_id");

            migrationBuilder.CreateIndex(
                name: "IX_study_invitations_study_id_email",
                schema: "identity",
                table: "study_invitations",
                columns: new[] { "study_id", "email" });

            migrationBuilder.CreateIndex(
                name: "IX_study_invitations_token",
                schema: "identity",
                table: "study_invitations",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_study_members_study_id_user_id",
                schema: "identity",
                table: "study_members",
                columns: new[] { "study_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_study_papers_study_id",
                schema: "identity",
                table: "study_papers",
                column: "study_id");

            migrationBuilder.CreateIndex(
                name: "IX_study_stars_study_id",
                schema: "identity",
                table: "study_stars",
                column: "study_id");

            migrationBuilder.CreateIndex(
                name: "IX_study_stars_study_id_user_id",
                schema: "identity",
                table: "study_stars",
                columns: new[] { "study_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_study_stars_user_id",
                schema: "identity",
                table: "study_stars",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_study_views_study_id",
                schema: "identity",
                table: "study_views",
                column: "study_id");

            migrationBuilder.CreateIndex(
                name: "IX_study_views_study_id_ip_hash_viewed_at",
                schema: "identity",
                table: "study_views",
                columns: new[] { "study_id", "ip_hash", "viewed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_study_views_study_id_viewed_at",
                schema: "identity",
                table: "study_views",
                columns: new[] { "study_id", "viewed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_plan_id",
                schema: "identity",
                table: "subscriptions",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_status",
                schema: "identity",
                table: "subscriptions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_user_id",
                schema: "identity",
                table: "subscriptions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_user_id_status",
                schema: "identity",
                table: "subscriptions",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_trace_annotations_trace_id_is_shared",
                schema: "identity",
                table: "trace_annotations",
                columns: new[] { "trace_id", "is_shared" });

            migrationBuilder.CreateIndex(
                name: "IX_trace_annotations_trace_id_start_position_end_position",
                schema: "identity",
                table: "trace_annotations",
                columns: new[] { "trace_id", "start_position", "end_position" });

            migrationBuilder.CreateIndex(
                name: "IX_traces_format",
                schema: "identity",
                table: "traces",
                column: "format");

            migrationBuilder.CreateIndex(
                name: "IX_traces_status",
                schema: "identity",
                table: "traces",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_traces_study_id",
                schema: "identity",
                table: "traces",
                column: "study_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pipeline_step_executions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "pipeline_steps",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "plan_features",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "profiles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "sequence_edits",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "study_invitations",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "study_members",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "study_papers",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "study_stars",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "study_views",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "subscriptions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "trace_annotations",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "pipeline_executions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "pipelines",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "plans",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "studies",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "traces",
                schema: "identity");
        }
    }
}
