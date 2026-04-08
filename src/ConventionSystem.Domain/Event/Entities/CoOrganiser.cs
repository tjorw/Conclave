using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Domain.Event.Entities;

public sealed class CoOrganiser
{
    public PersonId PersonId { get; private set; }
    public DateTimeOffset AddedAt { get; private set; }

    private CoOrganiser() { }

    internal CoOrganiser(PersonId personId)
    {
        PersonId = personId;
        AddedAt = DateTimeOffset.UtcNow;
    }
}
