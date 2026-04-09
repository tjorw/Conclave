using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_visitor_registrations_edition_id",
                table: "visitor_registrations",
                column: "edition_id");

            migrationBuilder.CreateIndex(
                name: "IX_visitor_registrations_person_id",
                table: "visitor_registrations",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_edition_id",
                table: "tickets",
                column: "edition_id");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_person_id",
                table: "tickets",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_ticket_type_id",
                table: "tickets",
                column: "ticket_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_types_edition_id",
                table: "ticket_types",
                column: "edition_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_applications_edition_id",
                table: "staff_applications",
                column: "edition_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_applications_person_id",
                table: "staff_applications",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "IX_shifts_station_id",
                table: "shifts",
                column: "station_id");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_venue_id",
                table: "sessions",
                column: "venue_id");

            migrationBuilder.CreateIndex(
                name: "IX_session_registrations_person_id",
                table: "session_registrations",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "IX_session_registrations_session_id",
                table: "session_registrations",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_persons_convention_id_email",
                table: "persons",
                columns: new[] { "convention_id", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_events_category_id",
                table: "events",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_events_edition_id",
                table: "events",
                column: "edition_id");

            migrationBuilder.CreateIndex(
                name: "IX_editions_convention_id",
                table: "editions",
                column: "convention_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_visitor_registrations_edition_id",
                table: "visitor_registrations");

            migrationBuilder.DropIndex(
                name: "IX_visitor_registrations_person_id",
                table: "visitor_registrations");

            migrationBuilder.DropIndex(
                name: "IX_tickets_edition_id",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_tickets_person_id",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_tickets_ticket_type_id",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_ticket_types_edition_id",
                table: "ticket_types");

            migrationBuilder.DropIndex(
                name: "IX_staff_applications_edition_id",
                table: "staff_applications");

            migrationBuilder.DropIndex(
                name: "IX_staff_applications_person_id",
                table: "staff_applications");

            migrationBuilder.DropIndex(
                name: "IX_shifts_station_id",
                table: "shifts");

            migrationBuilder.DropIndex(
                name: "IX_sessions_venue_id",
                table: "sessions");

            migrationBuilder.DropIndex(
                name: "IX_session_registrations_person_id",
                table: "session_registrations");

            migrationBuilder.DropIndex(
                name: "IX_session_registrations_session_id",
                table: "session_registrations");

            migrationBuilder.DropIndex(
                name: "IX_persons_convention_id_email",
                table: "persons");

            migrationBuilder.DropIndex(
                name: "IX_events_category_id",
                table: "events");

            migrationBuilder.DropIndex(
                name: "IX_events_edition_id",
                table: "events");

            migrationBuilder.DropIndex(
                name: "IX_editions_convention_id",
                table: "editions");
        }
    }
}
