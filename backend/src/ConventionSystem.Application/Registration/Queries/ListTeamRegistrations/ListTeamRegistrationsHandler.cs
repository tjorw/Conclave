using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Registration.Queries.ListTeamRegistrations;

public sealed class ListTeamRegistrationsHandler(
    ITeamEventRegistrationRepository registrationRepository)
    : IRequestHandler<ListTeamRegistrationsQuery, IReadOnlyList<TeamRegistrationSummaryDto>>
{
    public Task<IReadOnlyList<TeamRegistrationSummaryDto>> Handle(
        ListTeamRegistrationsQuery query, CancellationToken ct)
        => registrationRepository.ListByEventIdAsync(new EventId(query.EventId), ct);
}
