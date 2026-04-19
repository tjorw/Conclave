using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Registration.Queries.ListTicketTypes;

public sealed record ListTicketTypesQuery(Guid EditionId) : IQuery<IReadOnlyList<TicketTypeAdminDto>>;
