using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_session_requests_EventVersionId",
                table: "session_requests",
                newName: "IX_session_requests_event_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_events_lead_organiser_id",
                table: "events",
                column: "lead_organiser_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_events_lead_organiser_id",
                table: "events");

            migrationBuilder.RenameIndex(
                name: "IX_session_requests_event_version_id",
                table: "session_requests",
                newName: "IX_session_requests_EventVersionId");
        }
    }
}
