using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Queries;

namespace ConventionSystem.Application.Convention.Queries.ListEditionResponsibles;

public sealed record ListEditionResponsiblesQuery(Guid EditionId)
    : IQuery<IReadOnlyList<EditionResponsibleDto>>;
