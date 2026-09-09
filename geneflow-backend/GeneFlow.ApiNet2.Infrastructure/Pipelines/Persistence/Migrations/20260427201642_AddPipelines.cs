using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeneFlow.ApiNet2.Infrastructure.Pipelines.Persistence.Migrations;

/// <inheritdoc />
public partial class AddPipelines : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "pipelines");

        migrationBuilder.CreateTable(
            name: "pipeline_executions",
            schema: "pipelines",
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
            schema: "pipelines",
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
            name: "pipeline_step_executions",
            schema: "pipelines",
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
                    principalSchema: "pipelines",
                    principalTable: "pipeline_executions",
                    principalColumn: "id");
            });

        migrationBuilder.CreateTable(
            name: "pipeline_steps",
            schema: "pipelines",
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
                    principalSchema: "pipelines",
                    principalTable: "pipelines",
                    principalColumn: "id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_pipeline_executions_pipeline_id",
            schema: "pipelines",
            table: "pipeline_executions",
            column: "pipeline_id");

        migrationBuilder.CreateIndex(
            name: "IX_pipeline_executions_status",
            schema: "pipelines",
            table: "pipeline_executions",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "IX_pipeline_executions_trace_id",
            schema: "pipelines",
            table: "pipeline_executions",
            column: "trace_id");

        migrationBuilder.CreateIndex(
            name: "IX_pipeline_executions_trace_id_status",
            schema: "pipelines",
            table: "pipeline_executions",
            columns: new[] { "trace_id", "status" });

        migrationBuilder.CreateIndex(
            name: "IX_pipeline_step_executions_execution_id_order",
            schema: "pipelines",
            table: "pipeline_step_executions",
            columns: new[] { "execution_id", "order" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_pipeline_steps_pipeline_id_order",
            schema: "pipelines",
            table: "pipeline_steps",
            columns: new[] { "pipeline_id", "order" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_pipelines_name",
            schema: "pipelines",
            table: "pipelines",
            column: "name");

        migrationBuilder.CreateIndex(
            name: "IX_pipelines_owner_id",
            schema: "pipelines",
            table: "pipelines",
            column: "owner_id");

        migrationBuilder.CreateIndex(
            name: "IX_pipelines_status",
            schema: "pipelines",
            table: "pipelines",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "IX_pipelines_study_id",
            schema: "pipelines",
            table: "pipelines",
            column: "study_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "pipeline_step_executions",
            schema: "pipelines");

        migrationBuilder.DropTable(
            name: "pipeline_steps",
            schema: "pipelines");

        migrationBuilder.DropTable(
            name: "pipeline_executions",
            schema: "pipelines");

        migrationBuilder.DropTable(
            name: "pipelines",
            schema: "pipelines");
    }
}
