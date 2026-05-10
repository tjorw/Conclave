using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Content.Queries.GetPageTranslation;

public sealed record GetPageTranslationQuery(Guid PageId, string Locale) : IQuery<PageTranslationDto?>;

public sealed record PageTranslationDto(Guid PageId, string Locale, string Title, string Content);
