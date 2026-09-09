using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeneFlow.ApiNet2.Infrastructure.Studies.Persistence.Migrations;

/// <inheritdoc />
public partial class AddStudyOwnerType : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_study_papers_studies_study_id",
            schema: "studies",
            table: "study_papers");

        migrationBuilder.AddColumn<string>(
            name: "owner_type",
            schema: "studies",
            table: "studies",
            type: "character varying(10)",
            maxLength: 10,
            nullable: false,
            defaultValue: "User");

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

        migrationBuilder.DropColumn(
            name: "owner_type",
            schema: "studies",
            table: "studies");

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
