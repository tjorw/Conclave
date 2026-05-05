using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConventionBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "convention_brandings",
                columns: table => new
                {
                    convention_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    primary_color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    accent_color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    logo_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    favicon_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    font_family = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    custom_css = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_convention_brandings", x => x.convention_id);
                    table.ForeignKey(
                        name: "FK_convention_brandings_conventions_convention_id",
                        column: x => x.convention_id,
                        principalTable: "conventions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_convention_brandings_tenant_id",
                table: "convention_brandings",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "convention_brandings");
        }
    }
}
