using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCancelledColumnsFromCoOrganiserInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "co_organiser_invitations");

            migrationBuilder.DropColumn(
                name: "cancelled_by_id",
                table: "co_organiser_invitations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_at",
                table: "co_organiser_invitations",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "cancelled_by_id",
                table: "co_organiser_invitations",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
