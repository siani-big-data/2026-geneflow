using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeneFlow.ApiNet2.Infrastructure.PaymentMethods.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialPaymentMethods : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "billing");

        migrationBuilder.CreateTable(
            name: "payment_methods",
            schema: "billing",
            columns: table => new
            {
                id = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                user_id = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                stripe_payment_method_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                brand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                last4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                expiry_month = table.Column<int>(type: "integer", nullable: false),
                expiry_year = table.Column<int>(type: "integer", nullable: false),
                is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_payment_methods", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_payment_methods_user_id",
            schema: "billing",
            table: "payment_methods",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_payment_methods_stripe_id",
            schema: "billing",
            table: "payment_methods",
            column: "stripe_payment_method_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_payment_methods_user_default",
            schema: "billing",
            table: "payment_methods",
            columns: new[] { "user_id", "is_default" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "payment_methods",
            schema: "billing");
    }
}
