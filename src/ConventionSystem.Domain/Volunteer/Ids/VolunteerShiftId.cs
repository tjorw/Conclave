namespace ConventionSystem.Domain.Volunteer.Ids;

public readonly record struct VolunteerShiftId(Guid Value)
{
    public static VolunteerShiftId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
