using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Domain.Convention.Entities;

public sealed class Station : Entity<StationId>
{
    public StaffAreaId StaffAreaId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private Station() { }

    internal Station(StationId id, StaffAreaId staffAreaId, string name, string? description)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Namn får inte vara tomt.", nameof(name));
        StaffAreaId = staffAreaId;
        Name = name;
        Description = description;
    }
}
