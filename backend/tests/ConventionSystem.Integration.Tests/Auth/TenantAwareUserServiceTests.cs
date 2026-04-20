using ConventionSystem.Domain.Tenancy.Aggregates;
using ConventionSystem.Domain.Tenancy.Ids;
using ConventionSystem.Infrastructure.Identity;
using ConventionSystem.Infrastructure.Persistence;
using ConventionSystem.Integration.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ConventionSystem.Integration.Tests.Auth;

public sealed class TenantAwareUserServiceTests(ConventionSystemFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task FindTenantUserAsync_SameEmailDifferentTenants_ReturnsCorrectUserPerTenant()
    {
        const string email = "shared@test.se";

        Guid tenantAId;
        Guid tenantBId;

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var conventionDb = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
            var identityDb = scope.ServiceProvider.GetRequiredService<ApplicationIdentityDbContext>();

            var tenantA = new Tenant(TenantId.New(), $"ta-{Guid.NewGuid():N}", "Tenant A");
            var tenantB = new Tenant(TenantId.New(), $"tb-{Guid.NewGuid():N}", "Tenant B");

            conventionDb.Tenants.Add(tenantA);
            conventionDb.Tenants.Add(tenantB);
            await conventionDb.SaveChangesAsync();

            tenantAId = tenantA.Id.Value;
            tenantBId = tenantB.Id.Value;

            identityDb.Users.AddRange(
                new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = email,
                    NormalizedUserName = email.ToUpperInvariant(),
                    Email = email,
                    NormalizedEmail = email.ToUpperInvariant(),
                    UserType = UserType.TenantUser,
                    TenantId = tenantAId,
                    EmailConfirmed = true
                },
                new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = email,
                    NormalizedUserName = email.ToUpperInvariant(),
                    Email = email,
                    NormalizedEmail = email.ToUpperInvariant(),
                    UserType = UserType.TenantUser,
                    TenantId = tenantBId,
                    EmailConfirmed = true
                });

            await identityDb.SaveChangesAsync();
        }

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<TenantAwareUserService>();

            var userA = await service.FindTenantUserAsync(email, tenantAId);
            var userB = await service.FindTenantUserAsync(email, tenantBId);

            Assert.NotNull(userA);
            Assert.NotNull(userB);
            Assert.Equal(tenantAId, userA!.TenantId);
            Assert.Equal(tenantBId, userB!.TenantId);
        }
    }

    [Fact]
    public async Task FindTenantUserAsync_SystemAdminEmail_ReturnsNull()
    {
        const string email = "sysadmin@test.se";

        Guid tenantId;
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var conventionDb = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
            var identityDb = scope.ServiceProvider.GetRequiredService<ApplicationIdentityDbContext>();

            var tenant = new Tenant(TenantId.New(), $"tc-{Guid.NewGuid():N}", "Tenant C");
            conventionDb.Tenants.Add(tenant);
            await conventionDb.SaveChangesAsync();
            tenantId = tenant.Id.Value;

            identityDb.Users.Add(new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                UserType = UserType.SystemAdmin,
                TenantId = null,
                EmailConfirmed = true
            });

            await identityDb.SaveChangesAsync();
        }

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<TenantAwareUserService>();
            var user = await service.FindTenantUserAsync(email, tenantId);
            Assert.Null(user);
        }
    }

    [Fact]
    public async Task FindSystemAdminAsync_TenantUserEmail_ReturnsNull()
    {
        const string email = "tenant@test.se";

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var identityDb = scope.ServiceProvider.GetRequiredService<ApplicationIdentityDbContext>();

            identityDb.Users.Add(new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                UserType = UserType.TenantUser,
                TenantId = Guid.CreateVersion7(),
                EmailConfirmed = true
            });

            await identityDb.SaveChangesAsync();
        }

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<TenantAwareUserService>();
            var user = await service.FindSystemAdminAsync(email);
            Assert.Null(user);
        }
    }
}