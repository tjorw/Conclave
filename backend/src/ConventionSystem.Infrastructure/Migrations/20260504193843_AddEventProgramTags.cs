using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventProgramTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "event_program_tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newsequentialid()"),
                    name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_program_tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_event_program_tags_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_event_program_tags_event_id_name",
                table: "event_program_tags",
                columns: new[] { "event_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_event_program_tags_tenant_id",
                table: "event_program_tags",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_program_tags");
        }
    }
}
