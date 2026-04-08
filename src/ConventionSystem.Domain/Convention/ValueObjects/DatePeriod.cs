using ConventionSystem.Domain.Common;

namespace ConventionSystem.Domain.Convention.ValueObjects;

public sealed class DatePeriod : ValueObject
{
    public DateOnly StartDate { get; }
    public DateOnly EndDate { get; }

    public DatePeriod(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
            throw new ArgumentException("Slutdatum måste vara efter startdatum.", nameof(endDate));
        StartDate = startDate;
        EndDate = endDate;
    }

    public int DurationDays() => EndDate.DayNumber - StartDate.DayNumber + 1;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
    }
}
