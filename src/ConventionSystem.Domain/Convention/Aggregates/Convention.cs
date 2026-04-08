using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;

namespace ConventionSystem.Domain.Convention.Aggregates;

public sealed class Convention : AggregateRoot
{
    private readonly List<ConventionAdministrator> _administrators = [];

    public ConventionId Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
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
    }

    public Person RegisterPerson(string name, string email, string? phone = null)
        => new(PersonId.New(), Id, name, email, phone);

    public ConventionAdministrator AddAdministrator(PersonId personId, PersonId performedById)
    {
        if (_administrators.Any(a => a.PersonId == personId))
            throw new InvalidOperationException("Personen är redan administratör för denna konvention.");

        var admin = new ConventionAdministrator(personId, performedById);
        _administrators.Add(admin);
        return admin;
    }

    public Edition CreateEdition(string name, DatePeriod period)
        => new(EditionId.New(), Id, name, period);
}
