using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryDescriptionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "categories");

            migrationBuilder.AddColumn<string>(
                name: "organizer_instructions",
                table: "categories",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "public_description",
                table: "categories",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "organizer_instructions",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "public_description",
                table: "categories");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "categories",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}
