using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueEmailConstraintToPersons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_persons_convention_id_email",
                table: "persons");

            migrationBuilder.CreateIndex(
                name: "UQ_persons_convention_id_email",
                table: "persons",
                columns: new[] { "convention_id", "Email" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_persons_convention_id_email",
                table: "persons");

            migrationBuilder.CreateIndex(
                name: "IX_persons_convention_id_email",
                table: "persons",
                columns: new[] { "convention_id", "Email" });
        }
    }
}
