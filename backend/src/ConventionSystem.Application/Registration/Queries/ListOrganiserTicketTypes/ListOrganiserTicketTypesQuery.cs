using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Registration.Queries.ListOrganiserTicketTypes;

public sealed record ListOrganiserTicketTypesQuery(Guid EditionId) : IQuery<IReadOnlyList<OrganiserTicketTypeDto>>;
