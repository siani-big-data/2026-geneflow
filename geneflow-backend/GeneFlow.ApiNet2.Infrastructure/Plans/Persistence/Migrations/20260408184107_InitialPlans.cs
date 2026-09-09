using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GeneFlow.ApiNet2.Infrastructure.Plans.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialPlans : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "plans");

        migrationBuilder.CreateTable(
            name: "plans",
            schema: "plans",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
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
            name: "plan_features",
            schema: "plans",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                featuREDACTED = table.Column<int>(type: "integer", nullable: false),
                featuREDACTED = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_plan_features", x => x.id);
                table.ForeignKey(
                    name: "FK_plan_features_plans_plan_id",
                    column: x => x.plan_id,
                    principalSchema: "plans",
                    principalTable: "plans",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_plan_features_plan_id",
            schema: "plans",
            table: "plan_features",
            column: "plan_id");

        migrationBuilder.CreateIndex(
            name: "IX_plans_display_order",
            schema: "plans",
            table: "plans",
            column: "display_order");

        migrationBuilder.CreateIndex(
            name: "IX_plans_is_active",
            schema: "plans",
            table: "plans",
            column: "is_active");

        migrationBuilder.CreateIndex(
            name: "IX_plans_is_default",
            schema: "plans",
            table: "plans",
            column: "is_default");

        migrationBuilder.CreateIndex(
            name: "IX_plans_name",
            schema: "plans",
            table: "plans",
            column: "name",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "plan_features",
            schema: "plans");

        migrationBuilder.DropTable(
            name: "plans",
            schema: "plans");
    }
}
