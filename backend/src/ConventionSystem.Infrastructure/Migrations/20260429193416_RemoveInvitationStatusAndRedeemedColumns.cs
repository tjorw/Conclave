using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInvitationStatusAndRedeemedColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "redeemed_at",
                table: "co_organiser_invitations");

            migrationBuilder.DropColumn(
                name: "redeemed_by_id",
                table: "co_organiser_invitations");

            migrationBuilder.DropColumn(
                name: "status",
                table: "co_organiser_invitations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "redeemed_at",
                table: "co_organiser_invitations",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "redeemed_by_id",
                table: "co_organiser_invitations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "co_organiser_invitations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
