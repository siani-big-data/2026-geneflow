using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeneFlow.ApiNet2.Infrastructure.Traces.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialTraces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "traces");

            migrationBuilder.CreateTable(
                name: "traces",
                schema: "traces",
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
                name: "sequence_edits",
                schema: "traces",
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
                        principalSchema: "traces",
                        principalTable: "traces",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "trace_annotations",
                schema: "traces",
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
                        principalSchema: "traces",
                        principalTable: "traces",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_sequence_edits_trace_id_position",
                schema: "traces",
                table: "sequence_edits",
                columns: new[] { "trace_id", "position" });

            migrationBuilder.CreateIndex(
                name: "IX_trace_annotations_trace_id_is_shared",
                schema: "traces",
                table: "trace_annotations",
                columns: new[] { "trace_id", "is_shared" });

            migrationBuilder.CreateIndex(
                name: "IX_trace_annotations_trace_id_start_position_end_position",
                schema: "traces",
                table: "trace_annotations",
                columns: new[] { "trace_id", "start_position", "end_position" });

            migrationBuilder.CreateIndex(
                name: "IX_traces_format",
                schema: "traces",
                table: "traces",
                column: "format");

            migrationBuilder.CreateIndex(
                name: "IX_traces_status",
                schema: "traces",
                table: "traces",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_traces_study_id",
                schema: "traces",
                table: "traces",
                column: "study_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sequence_edits",
                schema: "traces");

            migrationBuilder.DropTable(
                name: "trace_annotations",
                schema: "traces");

            migrationBuilder.DropTable(
                name: "traces",
                schema: "traces");
        }
    }
}
