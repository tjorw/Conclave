using ConventionSystem.Application.Convention.Queries;

namespace ConventionSystem.Application.Convention.Queries.SearchPersonsForReception;

public sealed record SearchPersonsForReceptionQuery(Guid EditionId, string SearchTerm)
    : IQuery<IReadOnlyList<PersonSearchResultDto>>;
