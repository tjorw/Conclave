using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Convention.Queries.GetEdition;

public sealed record GetEditionQuery(Guid EditionId) : IQuery<EditionDto?>;
