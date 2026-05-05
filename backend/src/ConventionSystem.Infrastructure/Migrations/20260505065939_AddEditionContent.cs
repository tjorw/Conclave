using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEditionContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "edition_content",
                columns: table => new
                {
                    key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    edition_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_edition_content", x => new { x.edition_id, x.key });
                    table.ForeignKey(
                        name: "FK_edition_content_editions_edition_id",
                        column: x => x.edition_id,
                        principalTable: "editions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_edition_content_edition_id",
                table: "edition_content",
                column: "edition_id");

            migrationBuilder.CreateIndex(
                name: "IX_edition_content_tenant_id",
                table: "edition_content",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "edition_content");
        }
    }
}
