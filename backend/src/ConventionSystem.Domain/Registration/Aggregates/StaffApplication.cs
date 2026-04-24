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
    private readonly List<StaffAreaPreference> _staffAreaPreferences = [];

    public StaffApplicationId Id { get; private set; }
    public PersonId PersonId { get; private set; }
    public EditionId EditionId { get; private set; }
    public string InterestDescription { get; private set; } = string.Empty;
    public StaffApplicationStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<Availability> Availabilities => _availabilities.AsReadOnly();
    public IReadOnlyList<StaffAreaPreference> StaffAreaPreferences => _staffAreaPreferences.AsReadOnly();

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

    public StaffAreaPreference AddStaffAreaPreference(StaffAreaId staffAreaId)
    {
        if (_staffAreaPreferences.Any(s => s.StaffAreaId == staffAreaId))
            throw new DuplicateStaffAreaPreferenceException();

        var preference = new StaffAreaPreference(staffAreaId);
        _staffAreaPreferences.Add(preference);
        return preference;
    }

    public void RemoveStaffAreaPreference(StaffAreaId staffAreaId)
    {
        var preference = _staffAreaPreferences.FirstOrDefault(s => s.StaffAreaId == staffAreaId)
            ?? throw new StaffAreaPreferenceNotFoundException();
        _staffAreaPreferences.Remove(preference);
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
