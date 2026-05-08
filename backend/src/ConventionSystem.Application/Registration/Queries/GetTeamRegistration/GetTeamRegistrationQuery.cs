using ConventionSystem.Application.Registration.Queries;

namespace ConventionSystem.Application.Registration.Queries.GetTeamRegistration;

public sealed record GetTeamRegistrationQuery(Guid TeamEventRegistrationId) : IQuery<TeamRegistrationDetailDto?>;
