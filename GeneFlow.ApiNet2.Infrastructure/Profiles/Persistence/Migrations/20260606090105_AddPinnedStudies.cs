using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeneFlow.ApiNet2.Infrastructure.Profiles.Persistence.Migrations;

/// <inheritdoc />
public partial class AddPinnedStudies : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "pinned_studies",
            schema: "profiles",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                study_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                order = table.Column<int>(type: "integer", nullable: false),
                pinned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_pinned_studies", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_pinned_studies_user_id",
            schema: "profiles",
            table: "pinned_studies",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "IX_pinned_studies_user_id_study_id",
            schema: "profiles",
            table: "pinned_studies",
            columns: new[] { "user_id", "study_id" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "pinned_studies",
            schema: "profiles");
    }
}
