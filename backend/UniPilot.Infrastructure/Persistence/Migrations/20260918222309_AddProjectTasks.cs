using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniPilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "project_tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectRequirementId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DueDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_tasks_academic_projects_AcademicProjectId",
                        column: x => x.AcademicProjectId,
                        principalTable: "academic_projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_tasks_project_requirements_ProjectRequirementId",
                        column: x => x.ProjectRequirementId,
                        principalTable: "project_requirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_project_tasks_AcademicProjectId",
                table: "project_tasks",
                column: "AcademicProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_project_tasks_AcademicProjectId_Status_Position",
                table: "project_tasks",
                columns: new[] { "AcademicProjectId", "Status", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_project_tasks_ProjectRequirementId",
                table: "project_tasks",
                column: "ProjectRequirementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "project_tasks");
        }
    }
}
