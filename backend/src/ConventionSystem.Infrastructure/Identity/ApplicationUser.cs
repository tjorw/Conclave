using Microsoft.AspNetCore.Identity;

namespace ConventionSystem.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public Guid? PersonId { get; set; }
}
