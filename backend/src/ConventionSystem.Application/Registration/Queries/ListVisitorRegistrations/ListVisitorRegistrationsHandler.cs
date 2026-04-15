using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Queries.ListVisitorRegistrations;

public sealed class ListVisitorRegistrationsHandler(IVisitorRegistrationRepository visitorRegistrationRepository)
    : IRequestHandler<ListVisitorRegistrationsQuery, IReadOnlyList<VisitorRegistrationAdminDto>>
{
    public Task<IReadOnlyList<VisitorRegistrationAdminDto>> Handle(ListVisitorRegistrationsQuery query, CancellationToken ct)
        => visitorRegistrationRepository.ListByEditionAsync(new EditionId(query.EditionId), ct);
}
