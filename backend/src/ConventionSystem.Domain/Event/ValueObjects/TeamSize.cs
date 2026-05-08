using ConventionSystem.Domain.Common;

namespace ConventionSystem.Domain.Event.ValueObjects;

public sealed class TeamSize : ValueObject
{
    public int Min { get; }
    public int Max { get; }

    public TeamSize(int min, int max)
    {
        if (min < 1)
            throw new ArgumentException("Minsta lagstorlek måste vara minst 1.", nameof(min));
        if (max < min)
            throw new ArgumentException("Högsta lagstorlek måste vara minst lika stor som minsta lagstorlek.", nameof(max));
        Min = min;
        Max = max;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Min;
        yield return Max;
    }
}
