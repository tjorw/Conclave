using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Queries;

namespace ConventionSystem.Application.Convention.Queries.GetEditionContent;

public sealed record GetEditionContentQuery(Guid EditionId) : IQuery<IReadOnlyList<EditionContentDto>>;
