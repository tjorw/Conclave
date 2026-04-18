using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReviseTicketTypeValidDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_publicly_visible",
                table: "ticket_types");

            migrationBuilder.DropColumn(
                name: "is_sellable",
                table: "ticket_types");

            migrationBuilder.AddColumn<string>(
                name: "allowed_categories",
                table: "ticket_types",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "valid_days",
                table: "ticket_types",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "allowed_categories",
                table: "ticket_types");

            migrationBuilder.DropColumn(
                name: "valid_days",
                table: "ticket_types");

            migrationBuilder.AddColumn<bool>(
                name: "is_publicly_visible",
                table: "ticket_types",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_sellable",
                table: "ticket_types",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
