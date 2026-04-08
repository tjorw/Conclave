using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Domain.Convention.Entities;

public sealed class Venue : Entity<VenueId>
{
    public string Name { get; private set; } = string.Empty;
    public string Building { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private Venue() { }

    internal Venue(VenueId id, string name, string building, string? description)
        : base(id)
    {
        Name = name;
        Building = building;
        Description = description;
    }
}
