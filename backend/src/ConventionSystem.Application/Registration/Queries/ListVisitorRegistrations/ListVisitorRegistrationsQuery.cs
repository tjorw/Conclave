using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Registration.Queries.ListVisitorRegistrations;

public sealed record ListVisitorRegistrationsQuery(Guid EditionId) : IQuery<IReadOnlyList<VisitorRegistrationAdminDto>>;
