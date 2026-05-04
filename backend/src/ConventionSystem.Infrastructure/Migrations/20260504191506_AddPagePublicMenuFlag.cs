using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPagePublicMenuFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "show_in_public_menu",
                table: "pages",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "show_in_public_menu",
                table: "pages");
        }
    }
}
