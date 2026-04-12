using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeneFlow.ApiNet2.Infrastructure.Plans.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConvertPlanIdToPrefixedString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, drop the foreign key constraint
            migrationBuilder.DropForeignKey(
                name: "FK_plan_features_plans_plan_id",
                schema: "plans",
                table: "plan_features");

            // Clear existing data (required for type conversion)
            migrationBuilder.Sql("DELETE FROM plans.plan_features;");
            migrationBuilder.Sql("DELETE FROM plans.plans;");

            // Now alter the columns
            migrationBuilder.AlterColumn<string>(
                name: "id",
                schema: "plans",
                table: "plans",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "plan_id",
                schema: "plans",
                table: "plan_features",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            // Recreate the foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_plan_features_plans_plan_id",
                schema: "plans",
                table: "plan_features",
                column: "plan_id",
                principalSchema: "plans",
                principalTable: "plans",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the foreign key constraint
            migrationBuilder.DropForeignKey(
                name: "FK_plan_features_plans_plan_id",
                schema: "plans",
                table: "plan_features");

            // Clear existing data (required for type conversion)
            migrationBuilder.Sql("DELETE FROM plans.plan_features;");
            migrationBuilder.Sql("DELETE FROM plans.plans;");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "plans",
                table: "plans",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(9)",
                oldMaxLength: 9);

            migrationBuilder.AlterColumn<Guid>(
                name: "plan_id",
                schema: "plans",
                table: "plan_features",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(9)");

            // Recreate the foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_plan_features_plans_plan_id",
                schema: "plans",
                table: "plan_features",
                column: "plan_id",
                principalSchema: "plans",
                principalTable: "plans",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
