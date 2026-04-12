using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeneFlow.ApiNet2.Infrastructure.Subscriptions.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConvertSubscriptionIdToPrefixedString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clear existing data (required for type conversion)
            migrationBuilder.Sql("DELETE FROM subscriptions.subscriptions;");

            migrationBuilder.AlterColumn<string>(
                name: "user_id",
                schema: "subscriptions",
                table: "subscriptions",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "plan_id",
                schema: "subscriptions",
                table: "subscriptions",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "id",
                schema: "subscriptions",
                table: "subscriptions",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Clear existing data (required for type conversion)
            migrationBuilder.Sql("DELETE FROM subscriptions.subscriptions;");

            migrationBuilder.AlterColumn<long>(
                name: "user_id",
                schema: "subscriptions",
                table: "subscriptions",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(9)",
                oldMaxLength: 9);

            migrationBuilder.AlterColumn<Guid>(
                name: "plan_id",
                schema: "subscriptions",
                table: "subscriptions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(9)",
                oldMaxLength: 9);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "subscriptions",
                table: "subscriptions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(9)",
                oldMaxLength: 9);
        }
    }
}
