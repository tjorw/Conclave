using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Queries.GetTeamRegistration;

public sealed class GetTeamRegistrationHandler(
    ITeamEventRegistrationRepository registrationRepository)
    : IRequestHandler<GetTeamRegistrationQuery, TeamRegistrationDetailDto?>
{
    public Task<TeamRegistrationDetailDto?> Handle(GetTeamRegistrationQuery query, CancellationToken ct)
        => registrationRepository.GetDetailByIdAsync(
            new TeamEventRegistrationId(query.TeamEventRegistrationId), ct);
}
