using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddI18nTranslationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "edition_locales",
                columns: table => new
                {
                    edition_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    locale = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    is_primary = table.Column<bool>(type: "bit", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_edition_locales", x => new { x.edition_id, x.locale });
                    table.ForeignKey(
                        name: "FK_edition_locales_editions_edition_id",
                        column: x => x.edition_id,
                        principalTable: "editions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_translations",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    locale = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_translations", x => new { x.event_id, x.locale });
                    table.ForeignKey(
                        name: "FK_event_translations_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "page_translations",
                columns: table => new
                {
                    page_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    locale = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    content = table.Column<string>(type: "nvarchar(max)", maxLength: 20000, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_page_translations", x => new { x.page_id, x.locale });
                    table.ForeignKey(
                        name: "FK_page_translations_pages_page_id",
                        column: x => x.page_id,
                        principalTable: "pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_edition_locales_edition_id",
                table: "edition_locales",
                column: "edition_id");

            migrationBuilder.CreateIndex(
                name: "IX_edition_locales_tenant_id",
                table: "edition_locales",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_event_translations_event_id",
                table: "event_translations",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_event_translations_tenant_id",
                table: "event_translations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_page_translations_page_id",
                table: "page_translations",
                column: "page_id");

            migrationBuilder.CreateIndex(
                name: "IX_page_translations_tenant_id",
                table: "page_translations",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "edition_locales");

            migrationBuilder.DropTable(
                name: "event_translations");

            migrationBuilder.DropTable(
                name: "page_translations");
        }
    }
}
