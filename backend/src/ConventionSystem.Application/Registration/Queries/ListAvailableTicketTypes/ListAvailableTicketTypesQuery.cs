using MediatR;

namespace ConventionSystem.Application.Registration.Queries.ListAvailableTicketTypes;

public sealed record ListAvailableTicketTypesQuery(Guid EditionId) : IRequest<IReadOnlyList<VisitorTicketTypeDto>>;
