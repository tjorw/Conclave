using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Queries;

namespace ConventionSystem.Application.Registration.Queries.ListEditionVisitors;

public sealed record ListEditionVisitorsQuery(Guid EditionId)
    : IQuery<IReadOnlyList<EditionVisitorDto>>;
