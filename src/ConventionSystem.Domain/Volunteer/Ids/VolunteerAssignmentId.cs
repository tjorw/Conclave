namespace ConventionSystem.Domain.Volunteer.Ids;

public readonly record struct VolunteerAssignmentId(Guid Value)
{
    public static VolunteerAssignmentId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
