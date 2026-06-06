using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeneFlow.ApiNet2.Infrastructure.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFollows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_follows",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    follower_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    followee_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    followed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_follows", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_follows_followee_id",
                schema: "identity",
                table: "user_follows",
                column: "followee_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_follows_follower_id",
                schema: "identity",
                table: "user_follows",
                column: "follower_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_follows_follower_id_followee_id",
                schema: "identity",
                table: "user_follows",
                columns: new[] { "follower_id", "followee_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_follows",
                schema: "identity");
        }
    }
}
