using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Convention.Queries.ListPersons;

public sealed record ListPersonsQuery(Guid ConventionId) : IQuery<IReadOnlyList<PersonDto>>;
