using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    [Migration("20260422120000_SimplifyScheduleRequests")]
    public partial class SimplifyScheduleRequests : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "schedule_request_text",
                table: "events",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.DropTable(
                name: "session_requests");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "session_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    requested_duration_minutes = table.Column<int>(type: "int", nullable: false),
                    requested_seats = table.Column<int>(type: "int", nullable: false),
                    start_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_session_requests_events_EventId",
                        column: x => x.EventId,
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_session_requests_event_id",
                table: "session_requests",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_session_requests_tenant_id",
                table: "session_requests",
                column: "tenant_id");

            migrationBuilder.DropColumn(
                name: "schedule_request_text",
                table: "events");
        }
    }
}
