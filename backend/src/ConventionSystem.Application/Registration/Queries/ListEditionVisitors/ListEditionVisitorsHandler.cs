using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Registration.Queries.ListEditionVisitors;

public sealed class ListEditionVisitorsHandler(IVisitorRegistrationRepository visitorRegistrationRepository)
    : IQueryHandler<ListEditionVisitorsQuery, IReadOnlyList<EditionVisitorDto>>
{
    public Task<IReadOnlyList<EditionVisitorDto>> Handle(ListEditionVisitorsQuery query, CancellationToken ct)
        => visitorRegistrationRepository.ListConfirmedByEditionIdAsync(new EditionId(query.EditionId), ct);
}
