using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeneFlow.ApiNet2.Infrastructure.Pipelines.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixPipelineExecutionTraceIdColumnType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop indexes that depend on trace_id before altering the column type.
            migrationBuilder.DropIndex(
                name: "IX_pipeline_executions_trace_id",
                schema: "pipelines",
                table: "pipeline_executions");

            migrationBuilder.DropIndex(
                name: "IX_pipeline_executions_trace_id_status",
                schema: "pipelines",
                table: "pipeline_executions");

            // Alter trace_id from character varying(10) to uuid.
            // Existing rows (if any) must contain valid UUIDs because the previous insert
            // path attempted to write 36-char UUIDs into a varchar(10), which always failed.
            // The pipeline_executions table is therefore empty in any environment that
            // had this bug, so the USING cast is safe.
            migrationBuilder.Sql(
                "ALTER TABLE pipelines.pipeline_executions " +
                "ALTER COLUMN trace_id TYPE uuid USING trace_id::uuid;");

            // Recreate the indexes with the new column type.
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_pipeline_executions_trace_id",
                schema: "pipelines",
                table: "pipeline_executions");

            migrationBuilder.DropIndex(
                name: "IX_pipeline_executions_trace_id_status",
                schema: "pipelines",
                table: "pipeline_executions");

            migrationBuilder.Sql(
                "ALTER TABLE pipelines.pipeline_executions " +
                "ALTER COLUMN trace_id TYPE character varying(10) USING trace_id::text;");

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
        }
    }
}
