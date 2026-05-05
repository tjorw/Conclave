using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventFeaturedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "featured_sort_order",
                table: "events",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_featured",
                table: "events",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_events_edition_id_is_featured",
                table: "events",
                columns: new[] { "edition_id", "is_featured" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_events_edition_id_is_featured",
                table: "events");

            migrationBuilder.DropColumn(
                name: "featured_sort_order",
                table: "events");

            migrationBuilder.DropColumn(
                name: "is_featured",
                table: "events");
        }
    }
}
