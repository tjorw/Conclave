using ConventionSystem.Application.Registration.Queries;

namespace ConventionSystem.Application.Registration.Queries.ListTeamRegistrations;

public sealed record ListTeamRegistrationsQuery(Guid EventId) : IQuery<IReadOnlyList<TeamRegistrationSummaryDto>>;
