using Microsoft.AspNetCore.Identity;

namespace ConventionSystem.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public ICollection<ConventionUserLink> ConventionLinks { get; } = [];
}
