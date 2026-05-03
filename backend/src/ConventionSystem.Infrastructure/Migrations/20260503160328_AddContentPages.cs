using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContentPages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    convention_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    edition_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    content = table.Column<string>(type: "nvarchar(max)", maxLength: 20000, nullable: true),
                    is_published = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pages_convention_id_edition_id_slug",
                table: "pages",
                columns: new[] { "convention_id", "edition_id", "slug" },
                unique: true,
                filter: "[edition_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_pages_convention_id_slug",
                table: "pages",
                columns: new[] { "convention_id", "slug" },
                unique: true,
                filter: "[edition_id] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_pages_tenant_id",
                table: "pages",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pages");
        }
    }
}
