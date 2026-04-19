using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Registration.Queries.GetMySessionRegistrations;

public sealed record GetMySessionRegistrationsQuery(Guid EditionId) : IQuery<IReadOnlyList<MySessionRegistrationSummaryDto>>;
