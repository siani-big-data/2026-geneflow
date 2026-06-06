using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeneFlow.ApiNet2.Infrastructure.Search.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "search");

            migrationBuilder.CreateTable(
                name: "search_index",
                schema: "search",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    object_type = table.Column<int>(type: "integer", nullable: false),
                    object_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    owner_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    body = table.Column<string>(type: "text", nullable: true),
                    tags = table.Column<string>(type: "text", nullable: true),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_search_index", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_search_index_is_public",
                schema: "search",
                table: "search_index",
                column: "is_public");

            migrationBuilder.CreateIndex(
                name: "ix_search_index_object_type_id",
                schema: "search",
                table: "search_index",
                columns: new[] { "object_type", "object_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_search_index_owner_id",
                schema: "search",
                table: "search_index",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_search_index_updated_at",
                schema: "search",
                table: "search_index",
                column: "updated_at");

            // Server-generated tsvector column combining title (A), body (B),
            // and tags (C). 'simple' dictionary keeps the index language-agnostic
            // — we lean on PostgreSQL's lexer rather than stemming so user input
            // matches exact tokens (good for code/scientific terms).
            migrationBuilder.Sql(@"
ALTER TABLE search.search_index
ADD COLUMN tsv tsvector
GENERATED ALWAYS AS (
    setweight(to_tsvector('simple', coalesce(title, '')), 'A') ||
    setweight(to_tsvector('simple', coalesce(body,  '')), 'B') ||
    setweight(to_tsvector('simple', coalesce(tags,  '')), 'C')
) STORED;");

            // GIN index powers the @@ tsquery operator used in SearchAsync.
            migrationBuilder.Sql(@"
CREATE INDEX ix_search_index_tsv
ON search.search_index
USING GIN (tsv);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS search.ix_search_index_tsv;");
            migrationBuilder.Sql("ALTER TABLE search.search_index DROP COLUMN IF EXISTS tsv;");

            migrationBuilder.DropTable(
                name: "search_index",
                schema: "search");
        }
    }
}
