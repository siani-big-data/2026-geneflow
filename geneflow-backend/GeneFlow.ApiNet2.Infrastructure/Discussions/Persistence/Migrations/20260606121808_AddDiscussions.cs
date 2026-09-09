using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeneFlow.ApiNet2.Infrastructure.Discussions.Persistence.Migrations;

/// <inheritdoc />
public partial class AddDiscussions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "discussions");

        migrationBuilder.CreateTable(
            name: "comments",
            schema: "discussions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                parent_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                parent_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                author_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                body_markdown = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                edited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_comments", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "discussions",
            schema: "discussions",
            columns: table => new
            {
                id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                study_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                author_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                is_locked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                locked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                locked_by = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
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
                table.PrimaryKey("PK_discussions", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "reactions",
            schema: "discussions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                comment_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                emoji = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_reactions", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_comments_author_id",
            schema: "discussions",
            table: "comments",
            column: "author_id");

        migrationBuilder.CreateIndex(
            name: "IX_comments_parent_type_parent_id_created_at",
            schema: "discussions",
            table: "comments",
            columns: new[] { "parent_type", "parent_id", "created_at" });

        migrationBuilder.CreateIndex(
            name: "IX_discussions_author_id",
            schema: "discussions",
            table: "discussions",
            column: "author_id");

        migrationBuilder.CreateIndex(
            name: "IX_discussions_study_id",
            schema: "discussions",
            table: "discussions",
            column: "study_id");

        migrationBuilder.CreateIndex(
            name: "IX_discussions_study_id_created_at",
            schema: "discussions",
            table: "discussions",
            columns: new[] { "study_id", "created_at" });

        migrationBuilder.CreateIndex(
            name: "IX_reactions_comment_id",
            schema: "discussions",
            table: "reactions",
            column: "comment_id");

        migrationBuilder.CreateIndex(
            name: "IX_reactions_comment_id_user_id_emoji",
            schema: "discussions",
            table: "reactions",
            columns: new[] { "comment_id", "user_id", "emoji" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "comments",
            schema: "discussions");

        migrationBuilder.DropTable(
            name: "discussions",
            schema: "discussions");

        migrationBuilder.DropTable(
            name: "reactions",
            schema: "discussions");
    }
}
