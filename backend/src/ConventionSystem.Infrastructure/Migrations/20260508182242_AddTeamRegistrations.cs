using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamRegistrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "registration_mode",
                table: "events",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Individual");

            migrationBuilder.AddColumn<int>(
                name: "team_size_max",
                table: "events",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "team_size_min",
                table: "events",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "team_event_registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    team_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    edition_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_event_registrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    edition_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    captain_person_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_team_event_registrations_event_id",
                table: "team_event_registrations",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_team_event_registrations_team_id",
                table: "team_event_registrations",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "IX_team_event_registrations_tenant_id",
                table: "team_event_registrations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "UX_team_event_registrations_team_event",
                table: "team_event_registrations",
                columns: new[] { "team_id", "event_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teams_captain_person_id",
                table: "teams",
                column: "captain_person_id");

            migrationBuilder.CreateIndex(
                name: "IX_teams_edition_id",
                table: "teams",
                column: "edition_id");

            migrationBuilder.CreateIndex(
                name: "IX_teams_tenant_id",
                table: "teams",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "team_event_registrations");

            migrationBuilder.DropTable(
                name: "teams");

            migrationBuilder.DropColumn(
                name: "registration_mode",
                table: "events");

            migrationBuilder.DropColumn(
                name: "team_size_max",
                table: "events");

            migrationBuilder.DropColumn(
                name: "team_size_min",
                table: "events");
        }
    }
}
