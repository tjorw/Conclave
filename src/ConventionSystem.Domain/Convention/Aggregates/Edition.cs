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
    private readonly List<StaffArea> _staffAreas = [];
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
    public IReadOnlyList<StaffArea> StaffAreas => _staffAreas.AsReadOnly();
    public IReadOnlyList<Station> Stations => _stations.AsReadOnly();
    public IReadOnlyList<Category> Categories => _categories.AsReadOnly();

    private Edition() { }

    internal Edition(EditionId id, ConventionId conventionId, string name, DatePeriod period,
        PersonId staffCoordinatorId, PersonId eventCoordinatorId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Namn får inte vara tomt.", nameof(name));

        Id = id;
        ConventionId = conventionId;
        Name = name;
        Period = period;
        StaffCoordinatorId = staffCoordinatorId;
        EventCoordinatorId = eventCoordinatorId;
        Status = EditionStatus.Draft;
    }

    public void Publish(PersonId performedById)
    {
        if (Status == EditionStatus.Published)
            throw new InvalidOperationException("Upplagan är redan publicerad.");
        if (StaffCoordinatorId is null)
            throw new InvalidOperationException("Upplagan måste ha en bemanningskoordinator innan den kan publiceras.");
        if (EventCoordinatorId is null)
            throw new InvalidOperationException("Upplagan måste ha en evenemangskoordinator innan den kan publiceras.");

        Status = EditionStatus.Published;
        RaiseDomainEvent(new EditionPublished(Id, performedById, DateTimeOffset.UtcNow));
    }

    public void OpenOrganiserRegistration(PersonId performedById)
    {
        EnsurePublished();
        if (OrganiserRegistrationOpen)
            throw new InvalidOperationException("Arrangörsregistrering är redan öppen.");
        OrganiserRegistrationOpen = true;
        RaiseDomainEvent(new RegistrationOpened(Id, RegistrationType.Organiser, performedById, DateTimeOffset.UtcNow));
    }

    public void OpenStaffRegistration(PersonId performedById)
    {
        EnsurePublished();
        if (StaffRegistrationOpen)
            throw new InvalidOperationException("Personalregistrering är redan öppen.");
        StaffRegistrationOpen = true;
        RaiseDomainEvent(new RegistrationOpened(Id, RegistrationType.Staff, performedById, DateTimeOffset.UtcNow));
    }

    public void OpenVisitorRegistration(PersonId performedById)
    {
        EnsurePublished();
        if (VisitorRegistrationOpen)
            throw new InvalidOperationException("Besökarregistrering är redan öppen.");
        VisitorRegistrationOpen = true;
        RaiseDomainEvent(new RegistrationOpened(Id, RegistrationType.Visitor, performedById, DateTimeOffset.UtcNow));
    }

    public Venue CreateVenue(string name, string building, string? description = null)
    {
        var venue = new Venue(VenueId.New(), name, building, description);
        _venues.Add(venue);
        return venue;
    }

    public StaffArea CreateStaffArea(string name, PersonId responsibleId, string? description = null)
    {
        var staffArea = new StaffArea(StaffAreaId.New(), name, description, responsibleId);
        _staffAreas.Add(staffArea);
        return staffArea;
    }

    public Station CreateStation(string name, StaffAreaId staffAreaId, string? description = null)
    {
        if (!_staffAreas.Any(sa => sa.Id == staffAreaId))
            throw new InvalidOperationException("Funktionsområdet hittades inte på denna upplaga.");
        var station = new Station(StationId.New(), staffAreaId, name, description);
        _stations.Add(station);
        return station;
    }

    public Category CreateCategory(string name, PersonId responsibleId, string? description = null)
    {
        var category = new Category(CategoryId.New(), responsibleId, name, description);
        _categories.Add(category);
        return category;
    }

    public void ChangeCategoryResponsible(CategoryId categoryId, PersonId newResponsibleId)
    {
        var category = _categories.FirstOrDefault(c => c.Id == categoryId)
            ?? throw new InvalidOperationException($"Kategorin hittades inte på denna upplaga.");
        category.ChangeResponsible(newResponsibleId);
    }

    public bool IsStaffAreaResponsible(StaffAreaId staffAreaId, PersonId personId)
        => _staffAreas.Any(sa => sa.Id == staffAreaId && sa.ResponsibleId == personId);

    public bool IsStaffAreaResponsibleForStation(StationId stationId, PersonId personId)
    {
        var station = _stations.FirstOrDefault(s => s.Id == stationId);
        return station is not null && IsStaffAreaResponsible(station.StaffAreaId, personId);
    }

    public bool IsStaffCoordinator(PersonId personId)
        => StaffCoordinatorId == personId;

    public bool IsCategoryResponsible(CategoryId categoryId, PersonId personId)
        => _categories.Any(c => c.Id == categoryId && c.ResponsibleId == personId);

    /// <summary>
    /// Kopierar lokaler, funktionsområden och stationer från en källupplaga.
    /// Anropas av applikationslagret som ansvarar för att hämta källupplagets data.
    /// </summary>
    public void CopyStructure(EditionId sourceEditionId, IReadOnlyList<Venue> sourceVenues,
        IReadOnlyList<StaffArea> sourceStaffAreas, IReadOnlyList<Station> sourceStations, PersonId performedById)
    {
        if (Status != EditionStatus.Draft)
            throw new InvalidOperationException("Kan bara kopiera struktur till en upplaga med status Utkast.");

        _venues.Clear();
        _staffAreas.Clear();
        _stations.Clear();

        foreach (var v in sourceVenues)
            _venues.Add(new Venue(VenueId.New(), v.Name, v.Building, v.Description));

        var staffAreaIdMap = new Dictionary<StaffAreaId, StaffAreaId>();
        foreach (var sa in sourceStaffAreas)
        {
            var newId = StaffAreaId.New();
            staffAreaIdMap[sa.Id] = newId;
            _staffAreas.Add(new StaffArea(newId, sa.Name, sa.Description, sa.ResponsibleId));
        }

        foreach (var s in sourceStations)
        {
            var mappedAreaId = staffAreaIdMap.GetValueOrDefault(s.StaffAreaId, s.StaffAreaId);
            _stations.Add(new Station(StationId.New(), mappedAreaId, s.Name, s.Description));
        }

        RaiseDomainEvent(new StructureCopiedFromEdition(
            Id, sourceEditionId, sourceVenues.Count, sourceStaffAreas.Count, sourceStations.Count, performedById, DateTimeOffset.UtcNow));
    }

    private void EnsurePublished()
    {
        if (Status != EditionStatus.Published)
            throw new InvalidOperationException("Upplagan måste vara publicerad innan registrering kan öppnas.");
    }
}
