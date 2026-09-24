using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniPilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectInvitationEmailFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "InvitationExpiresAtUtc",
                table: "project_members",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InvitationSentAtUtc",
                table: "project_members",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvitationTokenHash",
                table: "project_members",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_members_InvitationTokenHash",
                table: "project_members",
                column: "InvitationTokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_project_members_InvitationTokenHash",
                table: "project_members");

            migrationBuilder.DropColumn(
                name: "InvitationExpiresAtUtc",
                table: "project_members");

            migrationBuilder.DropColumn(
                name: "InvitationSentAtUtc",
                table: "project_members");

            migrationBuilder.DropColumn(
                name: "InvitationTokenHash",
                table: "project_members");
        }
    }
}
