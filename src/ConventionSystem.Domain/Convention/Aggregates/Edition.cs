using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Enums;
using ConventionSystem.Domain.Convention.Events;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;

namespace ConventionSystem.Domain.Convention.Aggregates;

public sealed class Edition : AggregateRoot
{
    private readonly List<Venue> _venues = [];
    private readonly List<Station> _stations = [];
    private readonly List<Category> _categories = [];

    public EditionId Id { get; private set; }
    public ConventionId ConventionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DatePeriod Period { get; private set; } = null!;
    public EditionStatus Status { get; private set; }
    public bool OrganiserRegistrationOpen { get; private set; }
    public bool StaffRegistrationOpen { get; private set; }
    public bool VisitorRegistrationOpen { get; private set; }
    public PersonId? StaffCoordinatorId { get; private set; }
    public PersonId? EventCoordinatorId { get; private set; }

    public IReadOnlyList<Venue> Venues => _venues.AsReadOnly();
    public IReadOnlyList<Station> Stations => _stations.AsReadOnly();
    public IReadOnlyList<Category> Categories => _categories.AsReadOnly();

    private Edition() { }

    internal Edition(EditionId id, ConventionId conventionId, string name, DatePeriod period)
    {
        Id = id;
        ConventionId = conventionId;
        Name = name;
        Period = period;
        Status = EditionStatus.Draft;
    }

    public void Publish(PersonId performedById)
    {
        if (Status == EditionStatus.Published)
            throw new InvalidOperationException("Upplagan är redan publicerad.");

        Status = EditionStatus.Published;
        RaiseDomainEvent(new EditionPublished(Id, performedById, DateTimeOffset.UtcNow));
    }

    public void OpenOrganiserRegistration(PersonId performedById)
    {
        EnsurePublished();
        OrganiserRegistrationOpen = true;
        RaiseDomainEvent(new RegistrationOpened(Id, RegistrationType.Organiser, performedById, DateTimeOffset.UtcNow));
    }

    public void OpenStaffRegistration(PersonId performedById)
    {
        EnsurePublished();
        StaffRegistrationOpen = true;
        RaiseDomainEvent(new RegistrationOpened(Id, RegistrationType.Staff, performedById, DateTimeOffset.UtcNow));
    }

    public void OpenVisitorRegistration(PersonId performedById)
    {
        EnsurePublished();
        VisitorRegistrationOpen = true;
        RaiseDomainEvent(new RegistrationOpened(Id, RegistrationType.Visitor, performedById, DateTimeOffset.UtcNow));
    }

    public Venue CreateVenue(string name, string building, string? description = null)
    {
        var venue = new Venue(VenueId.New(), name, building, description);
        _venues.Add(venue);
        return venue;
    }

    public Station CreateStation(string name, PersonId responsibleId, string? description = null)
    {
        var station = new Station(StationId.New(), responsibleId, name, description);
        _stations.Add(station);
        return station;
    }

    public Category CreateCategory(string name, PersonId responsibleId, string? description = null)
    {
        var category = new Category(CategoryId.New(), responsibleId, name, description);
        _categories.Add(category);
        return category;
    }

    /// <summary>
    /// Kopierar lokaler och stationer från en källupplaga.
    /// Anropas av applikationslagret som ansvarar för att hämta källupplagets data.
    /// </summary>
    public void CopyStructure(EditionId sourceEditionId, IReadOnlyList<Venue> sourceVenues,
        IReadOnlyList<Station> sourceStations, PersonId performedById)
    {
        foreach (var v in sourceVenues)
            _venues.Add(new Venue(VenueId.New(), v.Name, v.Building, v.Description));

        foreach (var s in sourceStations)
            _stations.Add(new Station(StationId.New(), s.ResponsibleId, s.Name, s.Description));

        RaiseDomainEvent(new StructureCopiedFromEdition(
            Id, sourceEditionId, sourceVenues.Count, sourceStations.Count, performedById, DateTimeOffset.UtcNow));
    }

    private void EnsurePublished()
    {
        if (Status != EditionStatus.Published)
            throw new InvalidOperationException("Upplagan måste vara publicerad innan registrering kan öppnas.");
    }
}
