using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Domain.Registration.Entities;

public sealed class TicketPerk : Entity<TicketPerkId>
{
    public string Description { get; private set; } = string.Empty;

    private TicketPerk() { }

    internal TicketPerk(TicketPerkId id, string description)
        : base(id)
    {
        Description = description;
    }
}
