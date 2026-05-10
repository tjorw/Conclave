using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryAndTagTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "category_translations",
                columns: table => new
                {
                    category_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    locale = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_translations", x => new { x.category_id, x.locale });
                    table.ForeignKey(
                        name: "FK_category_translations_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "program_tag_translations",
                columns: table => new
                {
                    edition_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tag_name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    locale = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    translated_name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_tag_translations", x => new { x.edition_id, x.tag_name, x.locale });
                    table.ForeignKey(
                        name: "FK_program_tag_translations_editions_edition_id",
                        column: x => x.edition_id,
                        principalTable: "editions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_category_translations_category_id",
                table: "category_translations",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_translations_tenant_id",
                table: "category_translations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_program_tag_translations_edition_id",
                table: "program_tag_translations",
                column: "edition_id");

            migrationBuilder.CreateIndex(
                name: "IX_program_tag_translations_tenant_id",
                table: "program_tag_translations",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "category_translations");

            migrationBuilder.DropTable(
                name: "program_tag_translations");
        }
    }
}
