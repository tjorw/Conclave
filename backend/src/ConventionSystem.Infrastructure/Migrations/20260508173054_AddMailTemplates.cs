using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mail_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    convention_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    template_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    body_markdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_customized = table.Column<bool>(type: "bit", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mail_templates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mail_templates_convention_id_template_type",
                table: "mail_templates",
                columns: new[] { "convention_id", "template_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mail_templates_tenant_id",
                table: "mail_templates",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mail_templates");
        }
    }
}
