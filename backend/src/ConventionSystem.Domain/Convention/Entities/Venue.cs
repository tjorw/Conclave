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
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Namn får inte vara tomt.", nameof(name));
        if (string.IsNullOrWhiteSpace(building))
            throw new ArgumentException("Byggnad får inte vara tom.", nameof(building));
        Name = name;
        Building = building;
        Description = description;
    }

    internal void Update(string name, string building, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Namn får inte vara tomt.", nameof(name));
        if (string.IsNullOrWhiteSpace(building))
            throw new ArgumentException("Byggnad får inte vara tom.", nameof(building));
        Name = name;
        Building = building;
        Description = description;
    }
}
