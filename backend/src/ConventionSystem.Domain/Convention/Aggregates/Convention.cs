using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Events;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;

namespace ConventionSystem.Domain.Convention.Aggregates;

public sealed class Convention : AggregateRoot
{
    private readonly List<ConventionAdministrator> _administrators = [];

    public ConventionId Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public EditionId? ActiveEditionId { get; private set; }
    public IReadOnlyList<ConventionAdministrator> Administrators => _administrators.AsReadOnly();

    private Convention() { }

    public Convention(ConventionId id, string name, string slug)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Namn får inte vara tomt.", nameof(name));
        if (!System.Text.RegularExpressions.Regex.IsMatch(slug, @"^[a-z0-9-]+$"))
            throw new ArgumentException("Slug får bara innehålla gemener, siffror och bindestreck.", nameof(slug));

        Id = id;
        Name = name;
        Slug = slug;

        RaiseDomainEvent(new ConventionCreated(Id, Name, Slug, DateTimeOffset.UtcNow));
    }

    public Person RegisterPerson(string name, string email, string? phone = null)
    {
        var person = new Person(PersonId.New(), Id, name, email, phone);
        RaiseDomainEvent(new PersonRegistered(person.Id, Id, DateTimeOffset.UtcNow));
        return person;
    }

    public bool IsAdministrator(PersonId personId)
        => _administrators.Any(a => a.PersonId == personId);

    public ConventionAdministrator AddAdministrator(PersonId personId, PersonId performedById)
    {
        if (_administrators.Any(a => a.PersonId == personId))
            throw new InvalidOperationException("Personen är redan administratör för denna konvention.");

        var admin = new ConventionAdministrator(personId, performedById);
        _administrators.Add(admin);
        RaiseDomainEvent(new AdministratorAdded(Id, personId, performedById, DateTimeOffset.UtcNow));
        return admin;
    }

    public Person CreatePerson(string name, string email, string? phone = null)
    {
        var person = new Person(PersonId.New(), Id, name, email, phone);
        RaiseDomainEvent(new PersonCreated(person.Id, Id, DateTimeOffset.UtcNow));
        return person;
    }

    public void UpdatePersonDetails(Person person, string name, string email, string? phone)
    {
        if (person.ConventionId != Id)
            throw new InvalidOperationException("Personen tillhör inte denna konvention.");
        person.Update(name, email, phone);
        RaiseDomainEvent(new PersonUpdated(person.Id, Id, DateTimeOffset.UtcNow));
    }

    public void DeactivatePerson(Person person)
    {
        if (person.ConventionId != Id)
            throw new InvalidOperationException("Personen tillhör inte denna konvention.");
        if (!person.IsActive)
            throw new InvalidOperationException("Personen är redan inaktiverad.");
        person.Deactivate();
        RaiseDomainEvent(new PersonDeactivated(person.Id, Id, DateTimeOffset.UtcNow));
    }

    public void ReactivatePerson(Person person)
    {
        if (person.ConventionId != Id)
            throw new InvalidOperationException("Personen tillhör inte denna konvention.");
        if (person.IsActive)
            throw new InvalidOperationException("Personen är redan aktiv.");
        person.Reactivate();
        RaiseDomainEvent(new PersonReactivated(person.Id, Id, DateTimeOffset.UtcNow));
    }

    public void SetActiveEdition(EditionId editionId)
    {
        ActiveEditionId = editionId;
    }

    public Edition CreateEdition(string name, DatePeriod period, PersonId staffCoordinatorId, PersonId eventCoordinatorId)
        => new(EditionId.New(), Id, name, period, staffCoordinatorId, eventCoordinatorId);
}
