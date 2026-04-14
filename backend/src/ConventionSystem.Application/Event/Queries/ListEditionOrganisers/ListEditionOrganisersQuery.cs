using ConventionSystem.Application.Common;
using ConventionSystem.Application.Event.Queries;

namespace ConventionSystem.Application.Event.Queries.ListEditionOrganisers;

public sealed record ListEditionOrganisersQuery(Guid EditionId)
    : IQuery<IReadOnlyList<EditionOrganiserDto>>;
