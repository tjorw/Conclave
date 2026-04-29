using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCoOrganiserInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "co_organiser_count",
                table: "events",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "co_organiser_limit",
                table: "events",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "co_organiser_invitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    normalized_email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    redeemed_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    redeemed_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    cancelled_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_co_organiser_invitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_co_organiser_invitations_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_co_organiser_invitations_code",
                table: "co_organiser_invitations",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_co_organiser_invitations_event_id",
                table: "co_organiser_invitations",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_co_organiser_invitations_tenant_id",
                table: "co_organiser_invitations",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "co_organiser_invitations");

            migrationBuilder.DropColumn(
                name: "co_organiser_count",
                table: "events");

            migrationBuilder.DropColumn(
                name: "co_organiser_limit",
                table: "events");
        }
    }
}
