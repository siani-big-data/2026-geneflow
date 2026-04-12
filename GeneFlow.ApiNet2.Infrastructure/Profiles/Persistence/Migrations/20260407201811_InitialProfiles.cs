using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeneFlow.ApiNet2.Infrastructure.Profiles.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "profiles");

            migrationBuilder.CreateTable(
                name: "profiles",
                schema: "profiles",
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

            migrationBuilder.CreateIndex(
                name: "IX_profiles_research_field",
                schema: "profiles",
                table: "profiles",
                column: "research_field");

            migrationBuilder.CreateIndex(
                name: "IX_profiles_user_id",
                schema: "profiles",
                table: "profiles",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "profiles",
                schema: "profiles");
        }
    }
}
