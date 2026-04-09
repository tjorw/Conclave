using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Domain.Convention.Entities;

public sealed class StaffArea : Entity<StaffAreaId>
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public PersonId ResponsibleId { get; private set; }

    private StaffArea() { }

    internal StaffArea(StaffAreaId id, string name, string? description, PersonId responsibleId)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Namn får inte vara tomt.", nameof(name));
        Name = name;
        Description = description;
        ResponsibleId = responsibleId;
    }

    internal void ChangeResponsible(PersonId personId) => ResponsibleId = personId;
}
