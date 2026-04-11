using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Domain.Convention.Entities;

public sealed class ConventionAdministrator
{
    public PersonId PersonId { get; private set; }
    public PersonId AddedById { get; private set; }
    public DateTimeOffset AddedAt { get; private set; }

    private ConventionAdministrator() { }

    internal ConventionAdministrator(PersonId personId, PersonId addedById)
    {
        PersonId = personId;
        AddedById = addedById;
        AddedAt = DateTimeOffset.UtcNow;
    }
}
