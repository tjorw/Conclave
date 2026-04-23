using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEditionScheduleDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "edition_schedule_days",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    end_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    EditionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_edition_schedule_days", x => x.Id);
                    table.ForeignKey(
                        name: "FK_edition_schedule_days_editions_EditionId",
                        column: x => x.EditionId,
                        principalTable: "editions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                WITH edition_days AS
                (
                    SELECT Id AS EditionId, tenant_id, start_date AS [date], end_date
                    FROM editions
                    UNION ALL
                    SELECT EditionId, tenant_id, DATEADD(day, 1, [date]), end_date
                    FROM edition_days
                    WHERE [date] < end_date
                )
                INSERT INTO edition_schedule_days (Id, date, start_time, end_time, EditionId, tenant_id)
                SELECT NEWID(), [date], NULL, NULL, EditionId, tenant_id
                FROM edition_days
                OPTION (MAXRECURSION 0)
                """);

            migrationBuilder.CreateIndex(
                name: "IX_edition_schedule_days_edition_id_date",
                table: "edition_schedule_days",
                columns: new[] { "EditionId", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_edition_schedule_days_tenant_id",
                table: "edition_schedule_days",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "edition_schedule_days");
        }
    }
}
