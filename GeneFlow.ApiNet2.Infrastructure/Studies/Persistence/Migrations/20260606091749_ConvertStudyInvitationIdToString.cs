using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeneFlow.ApiNet2.Infrastructure.Studies.Persistence.Migrations;

/// <inheritdoc />
public partial class ConvertStudyInvitationIdToString : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_study_papers_studies_study_id",
            schema: "studies",
            table: "study_papers");

        migrationBuilder.AlterColumn<string>(
            name: "study_id",
            schema: "studies",
            table: "study_members",
            type: "character varying(10)",
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "character varying(10)",
            oldNullable: true);

        // Convert id column from uuid to varchar(10) with explicit cast.
        // Existing uuid values would exceed 10 chars; table is expected to be empty.
        migrationBuilder.Sql(@"
                ALTER TABLE studies.study_invitations
                ALTER COLUMN id TYPE character varying(10) USING id::text;
            ");

        migrationBuilder.AddForeignKey(
            name: "FK_study_papers_studies_study_id",
            schema: "studies",
            table: "study_papers",
            column: "study_id",
            principalSchema: "studies",
            principalTable: "studies",
            principalColumn: "id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_study_papers_studies_study_id",
            schema: "studies",
            table: "study_papers");

        migrationBuilder.AlterColumn<string>(
            name: "study_id",
            schema: "studies",
            table: "study_members",
            type: "character varying(10)",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(10)");

        migrationBuilder.Sql(@"
                ALTER TABLE studies.study_invitations
                ALTER COLUMN id TYPE uuid USING id::uuid;
            ");

        migrationBuilder.AddForeignKey(
            name: "FK_study_papers_studies_study_id",
            schema: "studies",
            table: "study_papers",
            column: "study_id",
            principalSchema: "studies",
            principalTable: "studies",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    }
}
