using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GeneFlow.ApiNet2.Infrastructure.Studies.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialStudies : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "studies");

        migrationBuilder.CreateTable(
            name: "studies",
            schema: "studies",
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
            schema: "studies",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
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
            schema: "studies",
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
            schema: "studies",
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
            name: "study_members",
            schema: "studies",
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
                    principalSchema: "studies",
                    principalTable: "studies",
                    principalColumn: "id");
            });

        migrationBuilder.CreateTable(
            name: "study_papers",
            schema: "studies",
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
                    principalSchema: "studies",
                    principalTable: "studies",
                    principalColumn: "id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_studies_is_featured",
            schema: "studies",
            table: "studies",
            column: "is_featured");

        migrationBuilder.CreateIndex(
            name: "IX_studies_owner_id",
            schema: "studies",
            table: "studies",
            column: "owner_id");

        migrationBuilder.CreateIndex(
            name: "IX_studies_research_field",
            schema: "studies",
            table: "studies",
            column: "research_field");

        migrationBuilder.CreateIndex(
            name: "IX_studies_status",
            schema: "studies",
            table: "studies",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "IX_study_invitations_email",
            schema: "studies",
            table: "study_invitations",
            column: "email");

        migrationBuilder.CreateIndex(
            name: "IX_study_invitations_status",
            schema: "studies",
            table: "study_invitations",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "IX_study_invitations_study_id",
            schema: "studies",
            table: "study_invitations",
            column: "study_id");

        migrationBuilder.CreateIndex(
            name: "IX_study_invitations_study_id_email",
            schema: "studies",
            table: "study_invitations",
            columns: new[] { "study_id", "email" });

        migrationBuilder.CreateIndex(
            name: "IX_study_invitations_token",
            schema: "studies",
            table: "study_invitations",
            column: "token",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_study_members_study_id_user_id",
            schema: "studies",
            table: "study_members",
            columns: new[] { "study_id", "user_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_study_papers_study_id",
            schema: "studies",
            table: "study_papers",
            column: "study_id");

        migrationBuilder.CreateIndex(
            name: "IX_study_stars_study_id",
            schema: "studies",
            table: "study_stars",
            column: "study_id");

        migrationBuilder.CreateIndex(
            name: "IX_study_stars_study_id_user_id",
            schema: "studies",
            table: "study_stars",
            columns: new[] { "study_id", "user_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_study_stars_user_id",
            schema: "studies",
            table: "study_stars",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "IX_study_views_study_id",
            schema: "studies",
            table: "study_views",
            column: "study_id");

        migrationBuilder.CreateIndex(
            name: "IX_study_views_study_id_ip_hash_viewed_at",
            schema: "studies",
            table: "study_views",
            columns: new[] { "study_id", "ip_hash", "viewed_at" });

        migrationBuilder.CreateIndex(
            name: "IX_study_views_study_id_viewed_at",
            schema: "studies",
            table: "study_views",
            columns: new[] { "study_id", "viewed_at" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "study_invitations",
            schema: "studies");

        migrationBuilder.DropTable(
            name: "study_members",
            schema: "studies");

        migrationBuilder.DropTable(
            name: "study_papers",
            schema: "studies");

        migrationBuilder.DropTable(
            name: "study_stars",
            schema: "studies");

        migrationBuilder.DropTable(
            name: "study_views",
            schema: "studies");

        migrationBuilder.DropTable(
            name: "studies",
            schema: "studies");
    }
}
