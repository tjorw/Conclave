using ConventionSystem.Domain.Common;

namespace ConventionSystem.Domain.Registration.ValueObjects;

public sealed class TimeSlot : ValueObject
{
    public DateTime Start { get; }
    public DateTime End { get; }

    public TimeSlot(DateTime start, DateTime end)
    {
        if (end <= start)
            throw new ArgumentException("Sluttiden måste vara efter starttiden.", nameof(end));
        Start = start;
        End = end;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}
