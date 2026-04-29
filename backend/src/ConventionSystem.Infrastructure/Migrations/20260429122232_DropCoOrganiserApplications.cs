using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropCoOrganiserApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "co_organiser_applications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "co_organiser_applications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    approved_person_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    normalized_email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    requested_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    review_comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    reviewed_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_co_organiser_applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_co_organiser_applications_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_co_organiser_applications_event_email_status",
                table: "co_organiser_applications",
                columns: new[] { "event_id", "normalized_email", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_co_organiser_applications_event_id",
                table: "co_organiser_applications",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_co_organiser_applications_tenant_id",
                table: "co_organiser_applications",
                column: "tenant_id");
        }
    }
}
