using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Content.Queries.ListPublicMenuPages;

public sealed record ListPublicMenuPagesQuery(string? Locale = null) : IQuery<IReadOnlyList<PublicPageMenuItemDto>>;
