using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectCalculationService.DataAccess.Migrations;

public partial class AddProjectOptimizations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "project_optimizations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CommunicationType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                TemplateId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                StartX = table.Column<int>(type: "integer", nullable: false),
                StartY = table.Column<int>(type: "integer", nullable: false),
                EndX = table.Column<int>(type: "integer", nullable: false),
                EndY = table.Column<int>(type: "integer", nullable: false),
                SelectedVariant = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                ResultJson = table.Column<string>(type: "jsonb", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_project_optimizations", x => x.Id);
                table.ForeignKey(
                    name: "FK_project_optimizations_projects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_project_optimizations_ProjectId",
            table: "project_optimizations",
            column: "ProjectId");

        migrationBuilder.CreateIndex(
            name: "IX_project_optimizations_ProjectId_CreatedAt",
            table: "project_optimizations",
            columns: new[] { "ProjectId", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "project_optimizations");
    }
}

