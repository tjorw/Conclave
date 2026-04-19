using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConventionSystem.Infrastructure.Migrations.Identity
{
    /// <inheritdoc />
    public partial class AddIdentityUserTypeAndTenantScoping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "EmailIndex",
                schema: "identity",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                schema: "identity",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                schema: "identity",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "user_type",
                schema: "identity",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE u
                SET [user_type] = 1,
                    [tenant_id] = NULL
                FROM [identity].[AspNetUsers] u
                WHERE EXISTS (
                    SELECT 1
                    FROM [identity].[AspNetUserClaims] c
                    WHERE c.[UserId] = u.[Id]
                      AND c.[ClaimType] = 'is_system_admin'
                      AND c.[ClaimValue] = 'true');
                """);

            migrationBuilder.Sql("""
                UPDATE u
                SET [tenant_id] = p.[tenant_id]
                FROM [identity].[AspNetUsers] u
                INNER JOIN [persons] p ON p.[Id] = u.[person_id]
                WHERE u.[user_type] = 0
                  AND u.[tenant_id] IS NULL;
                """);

            migrationBuilder.Sql("""
                DECLARE @DefaultTenantId uniqueidentifier;
                SELECT TOP (1) @DefaultTenantId = [Id]
                FROM [tenants]
                ORDER BY [created_at];

                IF @DefaultTenantId IS NULL
                    SET @DefaultTenantId = '00000000-0000-0000-0000-000000000000';

                UPDATE [identity].[AspNetUsers]
                SET [tenant_id] = @DefaultTenantId
                WHERE [user_type] = 0
                  AND [tenant_id] IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "identity",
                table: "AspNetUsers",
                column: "NormalizedUserName");

            migrationBuilder.CreateIndex(
                name: "UX_users_systemadmin_email",
                schema: "identity",
                table: "AspNetUsers",
                column: "NormalizedEmail",
                unique: true,
                filter: "[user_type] = 1 AND [NormalizedEmail] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_users_tenant_email",
                schema: "identity",
                table: "AspNetUsers",
                columns: new[] { "NormalizedEmail", "tenant_id" },
                unique: true,
                filter: "[user_type] = 0 AND [NormalizedEmail] IS NOT NULL AND [tenant_id] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                schema: "identity",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "UX_users_systemadmin_email",
                schema: "identity",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "UX_users_tenant_email",
                schema: "identity",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                schema: "identity",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "user_type",
                schema: "identity",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "identity",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "identity",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");
        }
    }
}
