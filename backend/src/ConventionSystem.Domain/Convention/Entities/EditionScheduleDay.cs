using ConventionSystem.Domain.Common;

namespace ConventionSystem.Domain.Convention.Entities;

public sealed class EditionScheduleDay : Entity<Guid>
{
    public DateOnly Date { get; private set; }
    public TimeOnly? StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }

    private EditionScheduleDay() { }

    public EditionScheduleDay(Guid id, DateOnly date, TimeOnly? startTime, TimeOnly? endTime) : base(id)
    {
        var effectiveStart = startTime ?? new TimeOnly(0, 0);
        var effectiveEnd = endTime ?? new TimeOnly(23, 59);
        if (effectiveEnd <= effectiveStart)
            throw new ArgumentException("Sluttid maste vara efter starttid.", nameof(endTime));

        Date = date;
        StartTime = startTime;
        EndTime = endTime;
    }
}
