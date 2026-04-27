using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyTicketDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "ticket_types",
                type: "nvarchar(max)",
                maxLength: 10000,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE tt
                SET description = perks.Markdown
                FROM ticket_types tt
                CROSS APPLY (
                    SELECT STRING_AGG(CONCAT('- ', REPLACE(REPLACE(p.Description, CHAR(13), ''), CHAR(10), ' ')), CHAR(10))
                        WITHIN GROUP (ORDER BY p.Description) AS Markdown
                    FROM ticket_perks p
                    WHERE p.TicketTypeId = tt.Id
                ) perks
                WHERE perks.Markdown IS NOT NULL;
                """);

            migrationBuilder.DropTable(
                name: "ticket_perks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ticket_perks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket_perks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ticket_perks_ticket_types_TicketTypeId",
                        column: x => x.TicketTypeId,
                        principalTable: "ticket_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ticket_perks_tenant_id",
                table: "ticket_perks",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_perks_TicketTypeId",
                table: "ticket_perks",
                column: "TicketTypeId");

            migrationBuilder.Sql("""
                INSERT INTO ticket_perks (Id, Description, tenant_id, TicketTypeId)
                SELECT NEWID(), LEFT(description, 500), tenant_id, Id
                FROM ticket_types
                WHERE description IS NOT NULL AND LEN(LTRIM(RTRIM(description))) > 0;
                """);

            migrationBuilder.DropColumn(
                name: "description",
                table: "ticket_types");
        }
    }
}
