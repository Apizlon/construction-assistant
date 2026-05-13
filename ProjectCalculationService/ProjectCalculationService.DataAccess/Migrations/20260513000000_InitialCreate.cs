using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectCalculationService.DataAccess.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "projects",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_projects", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "shared_projects",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                SharedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_shared_projects", x => x.Id);
                table.ForeignKey(
                    name: "FK_shared_projects_projects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "project_step_answers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                StepCode = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                AnswerType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                SelectedOptionCode = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                ValueJson = table.Column<string>(type: "jsonb", nullable: true),
                Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_project_step_answers", x => x.Id);
                table.ForeignKey(
                    name: "FK_project_step_answers_projects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "viewer_comments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                CommentText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                CommentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_viewer_comments", x => x.Id);
                table.ForeignKey(
                    name: "FK_viewer_comments_projects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "shared_project_viewers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                ShareId = table.Column<Guid>(type: "uuid", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_shared_project_viewers", x => x.Id);
                table.ForeignKey(
                    name: "FK_shared_project_viewers_shared_projects_ShareId",
                    column: x => x.ShareId,
                    principalTable: "shared_projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_projects_OwnerUserId",
            table: "projects",
            column: "OwnerUserId");

        migrationBuilder.CreateIndex(
            name: "IX_project_step_answers_ProjectId_StepCode",
            table: "project_step_answers",
            columns: new[] { "ProjectId", "StepCode" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_shared_projects_Code",
            table: "shared_projects",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_shared_projects_ProjectId_IsActive",
            table: "shared_projects",
            columns: new[] { "ProjectId", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "IX_shared_project_viewers_UserId_ShareId",
            table: "shared_project_viewers",
            columns: new[] { "UserId", "ShareId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_shared_project_viewers_UserId_IsActive",
            table: "shared_project_viewers",
            columns: new[] { "UserId", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "IX_shared_projects_ProjectId",
            table: "shared_projects",
            column: "ProjectId");

        migrationBuilder.CreateIndex(
            name: "IX_project_step_answers_ProjectId",
            table: "project_step_answers",
            column: "ProjectId");

        migrationBuilder.CreateIndex(
            name: "IX_viewer_comments_ProjectId_CommentDate",
            table: "viewer_comments",
            columns: new[] { "ProjectId", "CommentDate" });

        migrationBuilder.CreateIndex(
            name: "IX_viewer_comments_ProjectId",
            table: "viewer_comments",
            column: "ProjectId");

        migrationBuilder.CreateIndex(
            name: "IX_shared_project_viewers_ShareId",
            table: "shared_project_viewers",
            column: "ShareId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "project_step_answers");
        migrationBuilder.DropTable(name: "shared_project_viewers");
        migrationBuilder.DropTable(name: "viewer_comments");
        migrationBuilder.DropTable(name: "shared_projects");
        migrationBuilder.DropTable(name: "projects");
    }
}

