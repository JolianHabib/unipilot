using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniPilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "project_requirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourcePageNumber = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_requirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_requirements_academic_projects_AcademicProjectId",
                        column: x => x.AcademicProjectId,
                        principalTable: "academic_projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_requirements_project_documents_ProjectDocumentId",
                        column: x => x.ProjectDocumentId,
                        principalTable: "project_documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_project_requirements_AcademicProjectId",
                table: "project_requirements",
                column: "AcademicProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_project_requirements_ProjectDocumentId",
                table: "project_requirements",
                column: "ProjectDocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "project_requirements");
        }
    }
}
