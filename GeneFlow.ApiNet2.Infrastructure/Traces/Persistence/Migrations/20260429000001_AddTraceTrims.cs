using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeneFlow.ApiNet2.Infrastructure.Traces.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTraceTrims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create trace_trims table
            migrationBuilder.CreateTable(
                name: "trace_trims",
                schema: "traces",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trim_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    trim_end = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    start_position = table.Column<int>(type: "integer", nullable: false),
                    end_position = table.Column<int>(type: "integer", nullable: false),
                    algorithm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    applied_by = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    applied_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    trace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trace_trims", x => x.id);
                    table.ForeignKey(
                        name: "FK_trace_trims_traces_trace_id",
                        column: x => x.trace_id,
                        principalSchema: "traces",
                        principalTable: "traces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create index for efficient querying
            migrationBuilder.CreateIndex(
                name: "IX_trace_trims_trace_id_start_position",
                schema: "traces",
                table: "trace_trims",
                columns: new[] { "trace_id", "start_position" });

            // Migrate existing trim data from traces table to trace_trims
            migrationBuilder.Sql(@"
                INSERT INTO traces.trace_trims (id, trace_id, trim_type, trim_end, start_position, end_position, algorithm, applied_by, applied_at, is_active)
                SELECT
                    gen_random_uuid(),
                    id,
                    'Manual',
                    'FivePrime',
                    trim_start_5_prime,
                    trim_end_5_prime,
                    COALESCE(trim_algorithm, 'Manual'),
                    COALESCE(trimmed_by, 'system'),
                    COALESCE(trimmed_at, NOW()),
                    TRUE
                FROM traces.traces
                WHERE trim_start_5_prime IS NOT NULL AND trim_end_5_prime IS NOT NULL AND trim_end_5_prime > trim_start_5_prime;
            ");

            migrationBuilder.Sql(@"
                INSERT INTO traces.trace_trims (id, trace_id, trim_type, trim_end, start_position, end_position, algorithm, applied_by, applied_at, is_active)
                SELECT
                    gen_random_uuid(),
                    id,
                    'Manual',
                    'ThreePrime',
                    trim_start_3_prime,
                    trim_end_3_prime,
                    COALESCE(trim_algorithm, 'Manual'),
                    COALESCE(trimmed_by, 'system'),
                    COALESCE(trimmed_at, NOW()),
                    TRUE
                FROM traces.traces
                WHERE trim_start_3_prime IS NOT NULL AND trim_end_3_prime IS NOT NULL AND trim_end_3_prime > trim_start_3_prime;
            ");

            // Drop old trim columns from traces table
            migrationBuilder.DropColumn(
                name: "trim_start_5_prime",
                schema: "traces",
                table: "traces");

            migrationBuilder.DropColumn(
                name: "trim_end_5_prime",
                schema: "traces",
                table: "traces");

            migrationBuilder.DropColumn(
                name: "trim_start_3_prime",
                schema: "traces",
                table: "traces");

            migrationBuilder.DropColumn(
                name: "trim_end_3_prime",
                schema: "traces",
                table: "traces");

            migrationBuilder.DropColumn(
                name: "trim_algorithm",
                schema: "traces",
                table: "traces");

            migrationBuilder.DropColumn(
                name: "trimmed_by",
                schema: "traces",
                table: "traces");

            migrationBuilder.DropColumn(
                name: "trimmed_at",
                schema: "traces",
                table: "traces");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-add old trim columns to traces table
            migrationBuilder.AddColumn<int>(
                name: "trim_start_5_prime",
                schema: "traces",
                table: "traces",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "trim_end_5_prime",
                schema: "traces",
                table: "traces",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "trim_start_3_prime",
                schema: "traces",
                table: "traces",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "trim_end_3_prime",
                schema: "traces",
                table: "traces",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "trim_algorithm",
                schema: "traces",
                table: "traces",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "trimmed_by",
                schema: "traces",
                table: "traces",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "trimmed_at",
                schema: "traces",
                table: "traces",
                type: "timestamp with time zone",
                nullable: true);

            // Drop trace_trims table
            migrationBuilder.DropTable(
                name: "trace_trims",
                schema: "traces");
        }
    }
}
