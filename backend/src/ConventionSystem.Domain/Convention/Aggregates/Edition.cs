using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Enums;
using ConventionSystem.Domain.Convention.Events;
using ConventionSystem.Domain.Convention.Exceptions;
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
            throw new EditionAlreadyPublishedException();
        if (StaffCoordinatorId is null)
            throw new EditionStaffCoordinatorRequiredException();
        if (EventCoordinatorId is null)
            throw new EditionEventCoordinatorRequiredException();

        Status = EditionStatus.Published;
        RaiseDomainEvent(new EditionPublished(Id, performedById, DateTimeOffset.UtcNow));
    }

    public void OpenOrganiserRegistration(PersonId performedById)
    {
        EnsurePublished();
        if (OrganiserRegistrationOpen)
            throw new OrganiserRegistrationAlreadyOpenException();
        OrganiserRegistrationOpen = true;
        RaiseDomainEvent(new RegistrationOpened(Id, RegistrationType.Organiser, performedById, DateTimeOffset.UtcNow));
    }

    public void OpenStaffRegistration(PersonId performedById)
    {
        EnsurePublished();
        if (StaffRegistrationOpen)
            throw new StaffRegistrationAlreadyOpenException();
        StaffRegistrationOpen = true;
        RaiseDomainEvent(new RegistrationOpened(Id, RegistrationType.Staff, performedById, DateTimeOffset.UtcNow));
    }

    public void OpenVisitorRegistration(PersonId performedById)
    {
        EnsurePublished();
        if (VisitorRegistrationOpen)
            throw new VisitorRegistrationAlreadyOpenException();
        VisitorRegistrationOpen = true;
        RaiseDomainEvent(new RegistrationOpened(Id, RegistrationType.Visitor, performedById, DateTimeOffset.UtcNow));
    }

    public void CloseOrganiserRegistration(PersonId performedById)
    {
        if (!OrganiserRegistrationOpen)
            throw new OrganiserRegistrationNotOpenException();
        OrganiserRegistrationOpen = false;
        RaiseDomainEvent(new RegistrationClosed(Id, RegistrationType.Organiser, performedById, DateTimeOffset.UtcNow));
    }

    public void CloseStaffRegistration(PersonId performedById)
    {
        if (!StaffRegistrationOpen)
            throw new StaffRegistrationNotOpenException();
        StaffRegistrationOpen = false;
        RaiseDomainEvent(new RegistrationClosed(Id, RegistrationType.Staff, performedById, DateTimeOffset.UtcNow));
    }

    public void CloseVisitorRegistration(PersonId performedById)
    {
        if (!VisitorRegistrationOpen)
            throw new VisitorRegistrationNotOpenException();
        VisitorRegistrationOpen = false;
        RaiseDomainEvent(new RegistrationClosed(Id, RegistrationType.Visitor, performedById, DateTimeOffset.UtcNow));
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
            throw new StaffAreaNotFoundInEditionException();
        var station = new Station(StationId.New(), staffAreaId, name, description);
        _stations.Add(station);
        return station;
    }

    public void UpdateStation(StationId stationId, string name, string? description)
    {
        var station = _stations.FirstOrDefault(s => s.Id == stationId)
            ?? throw new StationNotFoundInEditionException();
        station.Update(name, description);
    }

    public Station RemoveStation(StationId stationId)
    {
        var station = _stations.FirstOrDefault(s => s.Id == stationId)
            ?? throw new StationNotFoundInEditionException();
        _stations.Remove(station);
        return station;
    }

    public Category CreateCategory(string name, PersonId responsibleId, string? description = null)
    {
        var category = new Category(CategoryId.New(), responsibleId, name, description);
        _categories.Add(category);
        return category;
    }

    public void UpdateDetails(string name, DatePeriod period, PersonId staffCoordinatorId, PersonId eventCoordinatorId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Namn får inte vara tomt.", nameof(name));
        Name = name;
        Period = period;
        StaffCoordinatorId = staffCoordinatorId;
        EventCoordinatorId = eventCoordinatorId;
    }

    public void UpdateVenue(VenueId venueId, string name, string building, string? description)
    {
        var venue = _venues.FirstOrDefault(v => v.Id == venueId)
            ?? throw new VenueNotFoundInEditionException();
        venue.Update(name, building, description);
    }

    public Venue RemoveVenue(VenueId venueId)
    {
        var venue = _venues.FirstOrDefault(v => v.Id == venueId)
            ?? throw new VenueNotFoundInEditionException();
        _venues.Remove(venue);
        return venue;
    }

    public void UpdateStaffArea(StaffAreaId staffAreaId, string name, string? description, PersonId responsibleId)
    {
        var area = _staffAreas.FirstOrDefault(sa => sa.Id == staffAreaId)
            ?? throw new StaffAreaNotFoundInEditionException();
        area.Update(name, description, responsibleId);
    }

    public (StaffArea area, IReadOnlyList<Station> stations) RemoveStaffArea(StaffAreaId staffAreaId)
    {
        var area = _staffAreas.FirstOrDefault(sa => sa.Id == staffAreaId)
            ?? throw new StaffAreaNotFoundInEditionException();
        var stations = _stations.Where(s => s.StaffAreaId == staffAreaId).ToList();
        foreach (var s in stations) _stations.Remove(s);
        _staffAreas.Remove(area);
        return (area, stations);
    }

    public void UpdateCategory(CategoryId categoryId, string name, string? description, PersonId responsibleId)
    {
        var category = _categories.FirstOrDefault(c => c.Id == categoryId)
            ?? throw new CategoryNotFoundInEditionException();
        category.Update(name, description, responsibleId);
    }

    public Category RemoveCategory(CategoryId categoryId)
    {
        var category = _categories.FirstOrDefault(c => c.Id == categoryId)
            ?? throw new CategoryNotFoundInEditionException();
        _categories.Remove(category);
        return category;
    }

    public void ChangeCategoryResponsible(CategoryId categoryId, PersonId newResponsibleId)
    {
        var category = _categories.FirstOrDefault(c => c.Id == categoryId)
            ?? throw new CategoryNotFoundInEditionException();
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
            throw new EditionMustBeDraftToCopyStructureException();

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
            throw new EditionMustBePublishedException();
    }
}
