using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "final_price",
                table: "tickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "promotion_code_redemption_id",
                table: "tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "promotion_codes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    edition_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    discount_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    discount_value = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    max_redemptions = table.Column<int>(type: "int", nullable: true),
                    valid_from = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    valid_until = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    allowed_ticket_type_ids = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_codes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "promotion_code_redemptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    promotion_code_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ticket_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    person_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ticket_type_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    discount_applied = table.Column<int>(type: "int", nullable: false),
                    final_price = table.Column<int>(type: "int", nullable: false),
                    redeemed_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_code_redemptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_promotion_code_redemptions_promotion_codes_promotion_code_id",
                        column: x => x.promotion_code_id,
                        principalTable: "promotion_codes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_promotion_code_redemptions_promotion_code_id",
                table: "promotion_code_redemptions",
                column: "promotion_code_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_codes_edition_id_code",
                table: "promotion_codes",
                columns: new[] { "edition_id", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "promotion_code_redemptions");

            migrationBuilder.DropTable(
                name: "promotion_codes");

            migrationBuilder.DropColumn(
                name: "final_price",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "promotion_code_redemption_id",
                table: "tickets");
        }
    }
}
