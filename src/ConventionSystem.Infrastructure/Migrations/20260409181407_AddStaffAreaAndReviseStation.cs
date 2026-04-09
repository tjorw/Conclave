using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffAreaAndReviseStation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "responsible_id",
                table: "stations",
                newName: "staff_area_id");

            migrationBuilder.RenameIndex(
                name: "IX_stations_EditionId",
                table: "stations",
                newName: "IX_stations_edition_id");

            migrationBuilder.CreateTable(
                name: "staff_areas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newsequentialid()"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    responsible_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_areas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_staff_areas_editions_EditionId",
                        column: x => x.EditionId,
                        principalTable: "editions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stations_staff_area_id",
                table: "stations",
                column: "staff_area_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_areas_edition_id",
                table: "staff_areas",
                column: "EditionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staff_areas");

            migrationBuilder.DropIndex(
                name: "IX_stations_staff_area_id",
                table: "stations");

            migrationBuilder.RenameColumn(
                name: "staff_area_id",
                table: "stations",
                newName: "responsible_id");

            migrationBuilder.RenameIndex(
                name: "IX_stations_edition_id",
                table: "stations",
                newName: "IX_stations_EditionId");
        }
    }
}
