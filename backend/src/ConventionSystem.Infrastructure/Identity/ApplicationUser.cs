using Microsoft.AspNetCore.Identity;

namespace ConventionSystem.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public UserType UserType { get; set; } = UserType.TenantUser;
    public Guid? TenantId { get; set; }
    public Guid? PersonId { get; set; }
}
