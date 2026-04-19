using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultitenancyTenantScoping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "visitor_registrations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "venues",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "ticket_types",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "ticket_perks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "stations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "staff_assignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "staff_areas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "staff_applications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "staff_application_stations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "staff_application_availabilities",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "shifts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "sessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "session_watches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "session_requests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "session_registrations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "promotion_codes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "promotion_code_redemptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "persons",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "events",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "event_comments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "editions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "domain_event_log",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "conventions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "convention_administrators",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "co_organisers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "categories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subdomain = table.Column<string>(type: "nvarchar(63)", maxLength: 63, nullable: false),
                    display_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.Id);
                });

            migrationBuilder.Sql(
                """
                DECLARE @DefaultTenantId uniqueidentifier = NEWID();

                INSERT INTO [tenants] ([Id], [Subdomain], [display_name], [Status], [created_at])
                VALUES (@DefaultTenantId, 'default', 'Default Tenant', 'Active', SYSUTCDATETIME());

                DECLARE @Tables TABLE ([Name] sysname);
                INSERT INTO @Tables ([Name])
                VALUES
                    ('visitor_registrations'),
                    ('venues'),
                    ('tickets'),
                    ('ticket_types'),
                    ('ticket_perks'),
                    ('stations'),
                    ('staff_assignments'),
                    ('staff_areas'),
                    ('staff_applications'),
                    ('staff_application_stations'),
                    ('staff_application_availabilities'),
                    ('shifts'),
                    ('sessions'),
                    ('session_watches'),
                    ('session_requests'),
                    ('session_registrations'),
                    ('promotion_codes'),
                    ('promotion_code_redemptions'),
                    ('persons'),
                    ('events'),
                    ('event_comments'),
                    ('editions'),
                    ('domain_event_log'),
                    ('conventions'),
                    ('convention_administrators'),
                    ('co_organisers'),
                    ('categories');

                DECLARE @TableName sysname;
                DECLARE @Sql nvarchar(max);

                DECLARE cur_update CURSOR LOCAL FAST_FORWARD FOR
                    SELECT [Name] FROM @Tables;

                OPEN cur_update;
                FETCH NEXT FROM cur_update INTO @TableName;

                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @Sql = N'UPDATE [' + @TableName + N'] SET [tenant_id] = @DefaultTenantId WHERE [tenant_id] IS NULL;';
                    EXEC sp_executesql @Sql, N'@DefaultTenantId uniqueidentifier', @DefaultTenantId;

                    FETCH NEXT FROM cur_update INTO @TableName;
                END

                CLOSE cur_update;
                DEALLOCATE cur_update;

                DECLARE cur_alter CURSOR LOCAL FAST_FORWARD FOR
                    SELECT [Name] FROM @Tables;

                OPEN cur_alter;
                FETCH NEXT FROM cur_alter INTO @TableName;

                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @Sql = N'ALTER TABLE [' + @TableName + N'] ALTER COLUMN [tenant_id] uniqueidentifier NOT NULL;';
                    EXEC sp_executesql @Sql;

                    FETCH NEXT FROM cur_alter INTO @TableName;
                END

                CLOSE cur_alter;
                DEALLOCATE cur_alter;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_visitor_registrations_tenant_id",
                table: "visitor_registrations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_venues_tenant_id",
                table: "venues",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_tenant_id",
                table: "tickets",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_types_tenant_id",
                table: "ticket_types",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_perks_tenant_id",
                table: "ticket_perks",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_stations_tenant_id",
                table: "stations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_assignments_tenant_id",
                table: "staff_assignments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_areas_tenant_id",
                table: "staff_areas",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_applications_tenant_id",
                table: "staff_applications",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_application_stations_tenant_id",
                table: "staff_application_stations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_application_availabilities_tenant_id",
                table: "staff_application_availabilities",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_shifts_tenant_id",
                table: "shifts",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_tenant_id",
                table: "sessions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_session_watches_tenant_id",
                table: "session_watches",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_session_requests_tenant_id",
                table: "session_requests",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_session_registrations_tenant_id",
                table: "session_registrations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_codes_tenant_id",
                table: "promotion_codes",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_code_redemptions_tenant_id",
                table: "promotion_code_redemptions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_persons_tenant_id",
                table: "persons",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_events_tenant_id",
                table: "events",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_event_comments_tenant_id",
                table: "event_comments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_editions_tenant_id",
                table: "editions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_domain_event_log_tenant_id",
                table: "domain_event_log",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_conventions_tenant_id",
                table: "conventions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_convention_administrators_tenant_id",
                table: "convention_administrators",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_co_organisers_tenant_id",
                table: "co_organisers",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_categories_tenant_id",
                table: "categories",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_Subdomain",
                table: "tenants",
                column: "Subdomain",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.DropIndex(
                name: "IX_visitor_registrations_tenant_id",
                table: "visitor_registrations");

            migrationBuilder.DropIndex(
                name: "IX_venues_tenant_id",
                table: "venues");

            migrationBuilder.DropIndex(
                name: "IX_tickets_tenant_id",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_ticket_types_tenant_id",
                table: "ticket_types");

            migrationBuilder.DropIndex(
                name: "IX_ticket_perks_tenant_id",
                table: "ticket_perks");

            migrationBuilder.DropIndex(
                name: "IX_stations_tenant_id",
                table: "stations");

            migrationBuilder.DropIndex(
                name: "IX_staff_assignments_tenant_id",
                table: "staff_assignments");

            migrationBuilder.DropIndex(
                name: "IX_staff_areas_tenant_id",
                table: "staff_areas");

            migrationBuilder.DropIndex(
                name: "IX_staff_applications_tenant_id",
                table: "staff_applications");

            migrationBuilder.DropIndex(
                name: "IX_staff_application_stations_tenant_id",
                table: "staff_application_stations");

            migrationBuilder.DropIndex(
                name: "IX_staff_application_availabilities_tenant_id",
                table: "staff_application_availabilities");

            migrationBuilder.DropIndex(
                name: "IX_shifts_tenant_id",
                table: "shifts");

            migrationBuilder.DropIndex(
                name: "IX_sessions_tenant_id",
                table: "sessions");

            migrationBuilder.DropIndex(
                name: "IX_session_watches_tenant_id",
                table: "session_watches");

            migrationBuilder.DropIndex(
                name: "IX_session_requests_tenant_id",
                table: "session_requests");

            migrationBuilder.DropIndex(
                name: "IX_session_registrations_tenant_id",
                table: "session_registrations");

            migrationBuilder.DropIndex(
                name: "IX_promotion_codes_tenant_id",
                table: "promotion_codes");

            migrationBuilder.DropIndex(
                name: "IX_promotion_code_redemptions_tenant_id",
                table: "promotion_code_redemptions");

            migrationBuilder.DropIndex(
                name: "IX_persons_tenant_id",
                table: "persons");

            migrationBuilder.DropIndex(
                name: "IX_events_tenant_id",
                table: "events");

            migrationBuilder.DropIndex(
                name: "IX_event_comments_tenant_id",
                table: "event_comments");

            migrationBuilder.DropIndex(
                name: "IX_editions_tenant_id",
                table: "editions");

            migrationBuilder.DropIndex(
                name: "IX_domain_event_log_tenant_id",
                table: "domain_event_log");

            migrationBuilder.DropIndex(
                name: "IX_conventions_tenant_id",
                table: "conventions");

            migrationBuilder.DropIndex(
                name: "IX_convention_administrators_tenant_id",
                table: "convention_administrators");

            migrationBuilder.DropIndex(
                name: "IX_co_organisers_tenant_id",
                table: "co_organisers");

            migrationBuilder.DropIndex(
                name: "IX_categories_tenant_id",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "visitor_registrations");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "venues");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "ticket_types");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "ticket_perks");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "stations");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "staff_assignments");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "staff_areas");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "staff_applications");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "staff_application_stations");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "staff_application_availabilities");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "shifts");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "session_watches");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "session_requests");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "session_registrations");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "promotion_codes");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "promotion_code_redemptions");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "persons");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "events");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "event_comments");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "editions");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "domain_event_log");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "conventions");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "convention_administrators");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "co_organisers");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "categories");
        }
    }
}


