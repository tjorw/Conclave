using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Events;
using ConventionSystem.Domain.Registration.Ids;
using ConventionSystem.Domain.Registration.ValueObjects;

namespace ConventionSystem.Domain.Registration.Aggregates;

public sealed class VolunteerApplication : AggregateRoot
{
    private readonly List<Availability> _availabilities = [];
    private readonly List<StationPreference> _stationPreferences = [];

    public VolunteerApplicationId Id { get; private set; }
    public PersonId PersonId { get; private set; }
    public EditionId EditionId { get; private set; }
    public string InterestDescription { get; private set; } = string.Empty;
    public VolunteerApplicationStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<Availability> Availabilities => _availabilities.AsReadOnly();
    public IReadOnlyList<StationPreference> StationPreferences => _stationPreferences.AsReadOnly();

    private VolunteerApplication() { }

    public VolunteerApplication(VolunteerApplicationId id, PersonId personId, EditionId editionId, string interestDescription)
    {
        Id = id;
        PersonId = personId;
        EditionId = editionId;
        InterestDescription = interestDescription;
        Status = VolunteerApplicationStatus.Received;
        CreatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new VolunteerApplicationReceived(Id, PersonId, EditionId, DateTimeOffset.UtcNow));
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
            ?? throw new InvalidOperationException("Tillgängligheten hittades inte.");
        _availabilities.Remove(availability);
    }

    public StationPreference AddStationPreference(StationId stationId)
    {
        if (_stationPreferences.Any(s => s.StationId == stationId))
            throw new InvalidOperationException("Stationsönskemål för denna station finns redan.");

        var preference = new StationPreference(stationId);
        _stationPreferences.Add(preference);
        return preference;
    }

    public void RemoveStationPreference(StationId stationId)
    {
        var preference = _stationPreferences.FirstOrDefault(s => s.StationId == stationId)
            ?? throw new InvalidOperationException("Stationsönskemålet hittades inte.");
        _stationPreferences.Remove(preference);
    }
}
