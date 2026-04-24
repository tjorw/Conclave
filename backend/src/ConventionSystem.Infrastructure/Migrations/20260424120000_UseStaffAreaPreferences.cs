using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UseStaffAreaPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_staff_application_stations_staff_applications_StaffApplicationId",
                table: "staff_application_stations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_staff_application_stations",
                table: "staff_application_stations");

            migrationBuilder.RenameTable(
                name: "staff_application_stations",
                newName: "staff_application_staff_areas");

            migrationBuilder.RenameIndex(
                name: "IX_staff_application_stations_tenant_id",
                table: "staff_application_staff_areas",
                newName: "IX_staff_application_staff_areas_tenant_id");

            migrationBuilder.RenameIndex(
                name: "IX_staff_application_stations_StaffApplicationId",
                table: "staff_application_staff_areas",
                newName: "IX_staff_application_staff_areas_StaffApplicationId");

            migrationBuilder.RenameColumn(
                name: "station_id",
                table: "staff_application_staff_areas",
                newName: "staff_area_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_staff_application_staff_areas",
                table: "staff_application_staff_areas",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_staff_application_staff_areas_staff_applications_StaffApplicationId",
                table: "staff_application_staff_areas",
                column: "StaffApplicationId",
                principalTable: "staff_applications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql("""
                UPDATE preferences
                SET staff_area_id = stations.staff_area_id
                FROM staff_application_staff_areas AS preferences
                INNER JOIN stations ON stations.Id = preferences.staff_area_id
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_staff_application_staff_areas_staff_applications_StaffApplicationId",
                table: "staff_application_staff_areas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_staff_application_staff_areas",
                table: "staff_application_staff_areas");

            migrationBuilder.Sql("""
                UPDATE preferences
                SET staff_area_id = stations.Id
                FROM staff_application_staff_areas AS preferences
                INNER JOIN stations ON stations.staff_area_id = preferences.staff_area_id
                WHERE stations.Id = (
                    SELECT TOP(1) inner_stations.Id
                    FROM stations AS inner_stations
                    WHERE inner_stations.staff_area_id = preferences.staff_area_id
                    ORDER BY inner_stations.Id
                )
                """);

            migrationBuilder.RenameColumn(
                name: "staff_area_id",
                table: "staff_application_staff_areas",
                newName: "station_id");

            migrationBuilder.RenameIndex(
                name: "IX_staff_application_staff_areas_tenant_id",
                table: "staff_application_staff_areas",
                newName: "IX_staff_application_stations_tenant_id");

            migrationBuilder.RenameIndex(
                name: "IX_staff_application_staff_areas_StaffApplicationId",
                table: "staff_application_staff_areas",
                newName: "IX_staff_application_stations_StaffApplicationId");

            migrationBuilder.RenameTable(
                name: "staff_application_staff_areas",
                newName: "staff_application_stations");

            migrationBuilder.AddPrimaryKey(
                name: "PK_staff_application_stations",
                table: "staff_application_stations",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_staff_application_stations_staff_applications_StaffApplicationId",
                table: "staff_application_stations",
                column: "StaffApplicationId",
                principalTable: "staff_applications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
