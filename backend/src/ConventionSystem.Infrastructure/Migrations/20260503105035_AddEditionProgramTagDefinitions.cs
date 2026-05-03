using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEditionProgramTagDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "edition_program_tag_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newsequentialid()"),
                    name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    edition_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_edition_program_tag_definitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_edition_program_tag_definitions_editions_edition_id",
                        column: x => x.edition_id,
                        principalTable: "editions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_edition_program_tag_definitions_edition_id_name",
                table: "edition_program_tag_definitions",
                columns: new[] { "edition_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_edition_program_tag_definitions_tenant_id",
                table: "edition_program_tag_definitions",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "edition_program_tag_definitions");
        }
    }
}
