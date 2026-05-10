using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Convention.Queries.GetEditionLocales;

public sealed record GetEditionLocalesQuery(Guid EditionId) : IQuery<IReadOnlyList<EditionLocaleDto>>;

public sealed record EditionLocaleDto(string Locale, bool IsPrimary);
