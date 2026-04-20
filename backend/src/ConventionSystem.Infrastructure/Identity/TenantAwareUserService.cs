using ConventionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Identity;

public sealed class TenantAwareUserService(ApplicationIdentityDbContext db)
{
    public Task<ApplicationUser?> FindTenantUserAsync(string email, Guid tenantId, CancellationToken ct = default)
    {
        var normalizedEmail = email.ToUpperInvariant();

        return db.Users
            .Where(u => u.UserType == UserType.TenantUser
                     && u.NormalizedEmail == normalizedEmail
                     && u.TenantId == tenantId)
            .SingleOrDefaultAsync(ct);
    }

    public Task<ApplicationUser?> FindSystemAdminAsync(string email, CancellationToken ct = default)
    {
        var normalizedEmail = email.ToUpperInvariant();

        return db.Users
            .Where(u => u.UserType == UserType.SystemAdmin
                     && u.NormalizedEmail == normalizedEmail)
            .SingleOrDefaultAsync(ct);
    }

    public Task<string?> FindTenantUserIdByPersonAsync(Guid tenantId, Guid personId, CancellationToken ct = default)
    {
        return db.Users
            .Where(u => u.UserType == UserType.TenantUser
                     && u.TenantId == tenantId
                     && u.PersonId == personId)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(ct);
    }
}
