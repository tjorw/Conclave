using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEventVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_events_event_versions_draft_version_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "FK_events_event_versions_published_version_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "FK_session_requests_event_versions_EventVersionId",
                table: "session_requests");

            migrationBuilder.DropTable(
                name: "event_versions");

            migrationBuilder.DropIndex(
                name: "IX_events_draft_version_id",
                table: "events");

            migrationBuilder.DropIndex(
                name: "IX_events_published_version_id",
                table: "events");

            migrationBuilder.DropColumn(
                name: "draft_version_id",
                table: "events");

            migrationBuilder.DropColumn(
                name: "published_version_id",
                table: "events");

            migrationBuilder.DropColumn(
                name: "version_id",
                table: "event_comments");

            migrationBuilder.RenameColumn(
                name: "EventVersionId",
                table: "session_requests",
                newName: "EventId");

            migrationBuilder.RenameIndex(
                name: "IX_session_requests_event_version_id",
                table: "session_requests",
                newName: "IX_session_requests_event_id");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "events",
                type: "nvarchar(max)",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "events",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "drop_in_rules",
                table: "events",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "registration_type",
                table: "events",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_session_requests_events_EventId",
                table: "session_requests",
                column: "EventId",
                principalTable: "events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_session_requests_events_EventId",
                table: "session_requests");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "events");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "events");

            migrationBuilder.DropColumn(
                name: "drop_in_rules",
                table: "events");

            migrationBuilder.DropColumn(
                name: "registration_type",
                table: "events");

            migrationBuilder.RenameColumn(
                name: "EventId",
                table: "session_requests",
                newName: "EventVersionId");

            migrationBuilder.RenameIndex(
                name: "IX_session_requests_event_id",
                table: "session_requests",
                newName: "IX_session_requests_event_version_id");

            migrationBuilder.AddColumn<Guid>(
                name: "draft_version_id",
                table: "events",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "published_version_id",
                table: "events",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "version_id",
                table: "event_comments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "event_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newsequentialid()"),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    drop_in_rules = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    registration_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_event_versions_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_events_draft_version_id",
                table: "events",
                column: "draft_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_events_published_version_id",
                table: "events",
                column: "published_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_event_versions_event_id",
                table: "event_versions",
                column: "event_id");

            migrationBuilder.AddForeignKey(
                name: "FK_events_event_versions_draft_version_id",
                table: "events",
                column: "draft_version_id",
                principalTable: "event_versions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_events_event_versions_published_version_id",
                table: "events",
                column: "published_version_id",
                principalTable: "event_versions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_session_requests_event_versions_EventVersionId",
                table: "session_requests",
                column: "EventVersionId",
                principalTable: "event_versions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
