using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Domain.Convention.Entities;

public sealed class Station : Entity<StationId>
{
    public PersonId ResponsibleId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private Station() { }

    internal Station(StationId id, PersonId responsibleId, string name, string? description)
        : base(id)
    {
        ResponsibleId = responsibleId;
        Name = name;
        Description = description;
    }
}
