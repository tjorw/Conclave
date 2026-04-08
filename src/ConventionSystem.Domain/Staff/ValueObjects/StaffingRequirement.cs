using ConventionSystem.Domain.Common;

namespace ConventionSystem.Domain.Staff.ValueObjects;

public sealed class StaffingRequirement : ValueObject
{
    public int MinPersons { get; }
    public int MaxPersons { get; }

    public StaffingRequirement(int minPersons, int maxPersons)
    {
        if (minPersons < 0)
            throw new ArgumentException("Minsta antal måste vara 0 eller fler.", nameof(minPersons));
        if (maxPersons < minPersons)
            throw new ArgumentException("Maximalt antal måste vara >= minsta antal.", nameof(maxPersons));
        MinPersons = minPersons;
        MaxPersons = maxPersons;
    }

    public bool IsUnderstaffed(int assignedCount) => assignedCount < MinPersons;
    public bool IsFullyStaffed(int assignedCount) => assignedCount >= MaxPersons;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MinPersons;
        yield return MaxPersons;
    }
}
