using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Events;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Domain.Registration.Aggregates;

public sealed class Team : AggregateRoot
{
    public TeamId Id { get; private set; }
    public EditionId EditionId { get; private set; }
    public PersonId CaptainPersonId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    private Team() { }

    public Team(TeamId id, EditionId editionId, PersonId captainPersonId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Lagnamn får inte vara tomt.", nameof(name));
        if (name.Length > 200)
            throw new ArgumentException("Lagnamn får inte vara längre än 200 tecken.", nameof(name));

        Id = id;
        EditionId = editionId;
        CaptainPersonId = captainPersonId;
        Name = name.Trim();
        CreatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new TeamCreated(id, editionId, captainPersonId, Name, CreatedAt));
    }
}
