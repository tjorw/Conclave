using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Exceptions;
using ConventionSystem.Domain.Registration.Events;
using ConventionSystem.Domain.Registration.Ids;
using ConventionSystem.Domain.Registration.ValueObjects;

namespace ConventionSystem.Domain.Registration.Aggregates;

public sealed class StaffApplication : AggregateRoot
{
    private readonly List<Availability> _availabilities = [];
    private readonly List<StationPreference> _stationPreferences = [];

    public StaffApplicationId Id { get; private set; }
    public PersonId PersonId { get; private set; }
    public EditionId EditionId { get; private set; }
    public string InterestDescription { get; private set; } = string.Empty;
    public StaffApplicationStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<Availability> Availabilities => _availabilities.AsReadOnly();
    public IReadOnlyList<StationPreference> StationPreferences => _stationPreferences.AsReadOnly();

    private StaffApplication() { }

    public StaffApplication(StaffApplicationId id, PersonId personId, EditionId editionId, string interestDescription)
    {
        if (string.IsNullOrWhiteSpace(interestDescription))
            throw new ArgumentException("Intressebeskrivning får inte vara tom.", nameof(interestDescription));

        Id = id;
        PersonId = personId;
        EditionId = editionId;
        InterestDescription = interestDescription;
        Status = StaffApplicationStatus.Received;
        CreatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new StaffApplicationReceived(Id, PersonId, EditionId, DateTimeOffset.UtcNow));
    }

    public Availability AddAvailability(DateTime from, DateTime to)
    {
        var availability = new Availability(AvailabilityId.New(), new TimeSlot(from, to));
        _availabilities.Add(availability);
        return availability;
    }

    public void RemoveAvailability(AvailabilityId availabilityId)
    {
        var availability = _availabilities.FirstOrDefault(a => a.Id == availabilityId)
            ?? throw new AvailabilityNotFoundException();
        _availabilities.Remove(availability);
    }

    public StationPreference AddStationPreference(StationId stationId)
    {
        if (_stationPreferences.Any(s => s.StationId == stationId))
            throw new DuplicateStationPreferenceException();

        var preference = new StationPreference(stationId);
        _stationPreferences.Add(preference);
        return preference;
    }

    public void RemoveStationPreference(StationId stationId)
    {
        var preference = _stationPreferences.FirstOrDefault(s => s.StationId == stationId)
            ?? throw new StationPreferenceNotFoundException();
        _stationPreferences.Remove(preference);
    }

    public void Accept(PersonId performedById)
    {
        if (Status != StaffApplicationStatus.Received && Status != StaffApplicationStatus.UnderReview)
            throw new StaffApplicationCannotBeAcceptedInCurrentStateException();

        Status = StaffApplicationStatus.Confirmed;
        RaiseDomainEvent(new StaffApplicationAccepted(Id, PersonId, EditionId, performedById, DateTimeOffset.UtcNow));
    }

    public void Reject(PersonId performedById)
    {
        if (Status != StaffApplicationStatus.Received && Status != StaffApplicationStatus.UnderReview)
            throw new StaffApplicationCannotBeRejectedInCurrentStateException();

        Status = StaffApplicationStatus.Rejected;
        RaiseDomainEvent(new StaffApplicationRejected(Id, PersonId, EditionId, performedById, DateTimeOffset.UtcNow));
    }
}
