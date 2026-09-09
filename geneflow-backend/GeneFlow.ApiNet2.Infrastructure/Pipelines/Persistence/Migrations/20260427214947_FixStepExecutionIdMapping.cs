using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeneFlow.ApiNet2.Infrastructure.Pipelines.Persistence.Migrations;

/// <inheritdoc />
public partial class FixStepExecutionIdMapping : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_pipeline_step_executions_pipeline_executions_execution_id",
            schema: "pipelines",
            table: "pipeline_step_executions");

        migrationBuilder.DropForeignKey(
            name: "FK_pipeline_steps_pipelines_pipeline_id",
            schema: "pipelines",
            table: "pipeline_steps");

        migrationBuilder.AddForeignKey(
            name: "FK_pipeline_step_executions_pipeline_executions_execution_id",
            schema: "pipelines",
            table: "pipeline_step_executions",
            column: "execution_id",
            principalSchema: "pipelines",
            principalTable: "pipeline_executions",
            principalColumn: "id");

        migrationBuilder.AddForeignKey(
            name: "FK_pipeline_steps_pipelines_pipeline_id",
            schema: "pipelines",
            table: "pipeline_steps",
            column: "pipeline_id",
            principalSchema: "pipelines",
            principalTable: "pipelines",
            principalColumn: "id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_pipeline_step_executions_pipeline_executions_execution_id",
            schema: "pipelines",
            table: "pipeline_step_executions");

        migrationBuilder.DropForeignKey(
            name: "FK_pipeline_steps_pipelines_pipeline_id",
            schema: "pipelines",
            table: "pipeline_steps");

        migrationBuilder.AddForeignKey(
            name: "FK_pipeline_step_executions_pipeline_executions_execution_id",
            schema: "pipelines",
            table: "pipeline_step_executions",
            column: "execution_id",
            principalSchema: "pipelines",
            principalTable: "pipeline_executions",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_pipeline_steps_pipelines_pipeline_id",
            schema: "pipelines",
            table: "pipeline_steps",
            column: "pipeline_id",
            principalSchema: "pipelines",
            principalTable: "pipelines",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    }
}
