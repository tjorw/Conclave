using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Registration.Queries.ListAvailableTicketTypes;

public sealed record ListAvailableTicketTypesQuery(Guid EditionId) : IQuery<IReadOnlyList<VisitorTicketTypeDto>>;
