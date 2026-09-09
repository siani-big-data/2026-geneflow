using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeneFlow.ApiNet2.Infrastructure.Identity.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialUserContext : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "identity");

        migrationBuilder.CreateTable(
            name: "users",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                password_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                email_verified = table.Column<bool>(type: "boolean", nullable: false),
                email_verification_token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                email_verification_token_expiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                password_reset_token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                password_reset_token_expiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                failed_login_attempts = table.Column<int>(type: "integer", nullable: false),
                lockout_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                totp_secret = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                totp_secret_created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                roles = table.Column<string>(type: "jsonb", nullable: false),
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
                table.PrimaryKey("PK_users", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "external_logins",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                provider_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                provider_display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                linked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UserId = table.Column<string>(type: "character varying(10)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_external_logins", x => x.id);
                table.ForeignKey(
                    name: "FK_external_logins_users_UserId",
                    column: x => x.UserId,
                    principalSchema: "identity",
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "refresh_tokens",
            schema: "identity",
            columns: table => new
            {
                token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                is_revoked = table.Column<bool>(type: "boolean", nullable: false),
                revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                replaced_by_token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                UserId = table.Column<string>(type: "character varying(10)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_refresh_tokens", x => x.token);
                table.ForeignKey(
                    name: "FK_refresh_tokens_users_UserId",
                    column: x => x.UserId,
                    principalSchema: "identity",
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "two_factor_codes",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                is_used = table.Column<bool>(type: "boolean", nullable: false),
                used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UserId = table.Column<string>(type: "character varying(10)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_two_factor_codes", x => x.id);
                table.ForeignKey(
                    name: "FK_two_factor_codes_users_UserId",
                    column: x => x.UserId,
                    principalSchema: "identity",
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_external_logins_provider_provider_key",
            schema: "identity",
            table: "external_logins",
            columns: new[] { "provider", "provider_key" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_external_logins_UserId",
            schema: "identity",
            table: "external_logins",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_refresh_tokens_token",
            schema: "identity",
            table: "refresh_tokens",
            column: "token");

        migrationBuilder.CreateIndex(
            name: "IX_refresh_tokens_UserId",
            schema: "identity",
            table: "refresh_tokens",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_two_factor_codes_UserId",
            schema: "identity",
            table: "two_factor_codes",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_users_email",
            schema: "identity",
            table: "users",
            column: "email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_users_username",
            schema: "identity",
            table: "users",
            column: "username",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "external_logins",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "refresh_tokens",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "two_factor_codes",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "users",
            schema: "identity");
    }
}
