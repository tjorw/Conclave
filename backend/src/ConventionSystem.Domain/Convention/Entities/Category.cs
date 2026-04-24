using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Domain.Convention.Entities;

public sealed class Category : Entity<CategoryId>
{
    public PersonId ResponsibleId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? OrganizerInstructions { get; private set; }
    public string? PublicDescription { get; private set; }

    private Category() { }

    internal Category(CategoryId id, PersonId responsibleId, string name,
        string? organizerInstructions, string? publicDescription)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Namn får inte vara tomt.", nameof(name));
        ResponsibleId = responsibleId;
        Name = name;
        OrganizerInstructions = organizerInstructions;
        PublicDescription = publicDescription;
    }

    internal void ChangeResponsible(PersonId personId) => ResponsibleId = personId;

    internal void Update(string name, string? organizerInstructions, string? publicDescription, PersonId responsibleId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Namn får inte vara tomt.", nameof(name));
        Name = name;
        OrganizerInstructions = organizerInstructions;
        PublicDescription = publicDescription;
        ResponsibleId = responsibleId;
    }
}
