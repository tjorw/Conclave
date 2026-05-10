using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Content.Queries.GetPublicPage;

public sealed record GetPublicPageQuery(string Slug, string? Locale = null) : IQuery<PublicPageDto?>;
