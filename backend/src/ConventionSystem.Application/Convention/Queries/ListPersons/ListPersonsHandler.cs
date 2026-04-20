using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Queries.ListPersons;

public sealed class ListPersonsHandler(IPersonRepository personRepository)
    : IQueryHandler<ListPersonsQuery, IReadOnlyList<PersonDto>>
{
    public Task<IReadOnlyList<PersonDto>> Handle(ListPersonsQuery query, CancellationToken ct)
        => personRepository.ListByConventionIdAsync(new ConventionId(query.ConventionId), ct);
}
