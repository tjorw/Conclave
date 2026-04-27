using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Queries;

namespace ConventionSystem.Application.Registration.Queries.ListVisitorTicketTypesForWalkup;

public sealed record ListVisitorTicketTypesForWalkupQuery(Guid EditionId)
    : IQuery<IReadOnlyList<VisitorTicketTypeDto>>;
