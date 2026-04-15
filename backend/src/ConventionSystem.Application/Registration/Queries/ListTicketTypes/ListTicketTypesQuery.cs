using MediatR;

namespace ConventionSystem.Application.Registration.Queries.ListTicketTypes;

public sealed record ListTicketTypesQuery(Guid EditionId) : IRequest<IReadOnlyList<TicketTypeAdminDto>>;
