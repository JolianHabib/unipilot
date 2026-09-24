using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniPilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompletePendingProjectInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "project_members",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "InvitedEmail",
                table: "project_members",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_members_AcademicProjectId_InvitedEmail",
                table: "project_members",
                columns: new[] { "AcademicProjectId", "InvitedEmail" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_project_members_AcademicProjectId_InvitedEmail",
                table: "project_members");

            migrationBuilder.DropColumn(
                name: "InvitedEmail",
                table: "project_members");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "project_members",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
