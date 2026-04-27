using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReceptionStaff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "edition_reception_staff",
                columns: table => new
                {
                    person_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    edition_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    added_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    added_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_edition_reception_staff", x => new { x.edition_id, x.person_id });
                    table.ForeignKey(
                        name: "FK_edition_reception_staff_editions_edition_id",
                        column: x => x.edition_id,
                        principalTable: "editions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_edition_reception_staff_edition_id",
                table: "edition_reception_staff",
                column: "edition_id");

            migrationBuilder.CreateIndex(
                name: "IX_edition_reception_staff_tenant_id",
                table: "edition_reception_staff",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "edition_reception_staff");
        }
    }
}
