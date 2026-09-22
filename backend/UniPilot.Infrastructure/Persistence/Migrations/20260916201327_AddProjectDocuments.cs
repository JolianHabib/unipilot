using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniPilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "project_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ProcessingStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PageCount = table.Column<int>(type: "integer", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UploadedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_documents_academic_projects_AcademicProjectId",
                        column: x => x.AcademicProjectId,
                        principalTable: "academic_projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_pages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PageNumber = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_pages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_document_pages_project_documents_ProjectDocumentId",
                        column: x => x.ProjectDocumentId,
                        principalTable: "project_documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_document_pages_ProjectDocumentId",
                table: "document_pages",
                column: "ProjectDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_document_pages_ProjectDocumentId_PageNumber",
                table: "document_pages",
                columns: new[] { "ProjectDocumentId", "PageNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_documents_AcademicProjectId",
                table: "project_documents",
                column: "AcademicProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_project_documents_AcademicProjectId_ContentHash",
                table: "project_documents",
                columns: new[] { "AcademicProjectId", "ContentHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_pages");

            migrationBuilder.DropTable(
                name: "project_documents");
        }
    }
}
