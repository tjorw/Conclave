using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Registration.Queries.ListStaffTicketTypes;

public sealed record ListStaffTicketTypesQuery(Guid EditionId) : IQuery<IReadOnlyList<StaffTicketTypeDto>>;
