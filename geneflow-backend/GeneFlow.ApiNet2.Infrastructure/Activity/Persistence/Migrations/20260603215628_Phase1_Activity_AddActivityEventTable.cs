using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeneFlow.ApiNet2.Infrastructure.Activity.Persistence.Migrations;

/// <inheritdoc />
public partial class Phase1_Activity_AddActivityEventTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "activity");

        migrationBuilder.CreateTable(
            name: "activity_events",
            schema: "activity",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                actor_user_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                verb = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                object_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                object_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                study_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                visibility = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                payload_json = table.Column<string>(type: "jsonb", nullable: false),
                source_event_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                source_message_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_activity_events", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_activity_events_actor_occurred_at_id",
            schema: "activity",
            table: "activity_events",
            columns: new[] { "actor_user_id", "occurred_at", "id" },
            descending: new[] { false, true, true });

        migrationBuilder.CreateIndex(
            name: "ix_activity_events_occurred_at_id",
            schema: "activity",
            table: "activity_events",
            columns: new[] { "occurred_at", "id" },
            descending: new bool[0]);

        migrationBuilder.CreateIndex(
            name: "ix_activity_events_study_occurred_at_id",
            schema: "activity",
            table: "activity_events",
            columns: new[] { "study_id", "occurred_at", "id" },
            descending: new[] { false, true, true });

        migrationBuilder.CreateIndex(
            name: "ux_activity_events_source_message_id",
            schema: "activity",
            table: "activity_events",
            column: "source_message_id",
            unique: true,
            filter: "source_message_id IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "activity_events",
            schema: "activity");
    }
}
