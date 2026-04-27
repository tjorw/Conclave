using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Reception.Queries.GetPersonScheduleForReception;

public sealed record GetPersonScheduleForReceptionQuery(Guid PersonId, Guid EditionId)
    : IQuery<PersonScheduleDto>;
