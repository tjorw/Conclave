using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Domain.Convention.Entities;

public sealed class Category : Entity<CategoryId>
{
    public PersonId ResponsibleId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private Category() { }

    internal Category(CategoryId id, PersonId responsibleId, string name, string? description)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Namn får inte vara tomt.", nameof(name));
        ResponsibleId = responsibleId;
        Name = name;
        Description = description;
    }

    internal void ChangeResponsible(PersonId personId) => ResponsibleId = personId;
}
