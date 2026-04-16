using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Registration.Queries.GetMySchedule;

public sealed record GetMyScheduleQuery(Guid EditionId) : IQuery<IReadOnlyList<MyScheduleItemDto>>;
