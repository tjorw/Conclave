using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventCommentWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "acknowledged_at",
                table: "event_comments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "acknowledged_by_id",
                table: "event_comments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "handled_at",
                table: "event_comments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "handled_by_id",
                table: "event_comments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "handling_comment",
                table: "event_comments",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "requires_handling",
                table: "event_comments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "event_comments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "acknowledged_at",
                table: "event_comments");

            migrationBuilder.DropColumn(
                name: "acknowledged_by_id",
                table: "event_comments");

            migrationBuilder.DropColumn(
                name: "handled_at",
                table: "event_comments");

            migrationBuilder.DropColumn(
                name: "handled_by_id",
                table: "event_comments");

            migrationBuilder.DropColumn(
                name: "handling_comment",
                table: "event_comments");

            migrationBuilder.DropColumn(
                name: "requires_handling",
                table: "event_comments");

            migrationBuilder.DropColumn(
                name: "status",
                table: "event_comments");
        }
    }
}
