using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Registration.Ids;
using ConventionSystem.Domain.Registration.ValueObjects;

namespace ConventionSystem.Domain.Registration.Entities;

public sealed class Availability : Entity<AvailabilityId>
{
    public TimeSlot TimeSlot { get; private set; } = null!;

    private Availability() { }

    internal Availability(AvailabilityId id, TimeSlot timeSlot)
        : base(id)
    {
        TimeSlot = timeSlot;
    }
}
