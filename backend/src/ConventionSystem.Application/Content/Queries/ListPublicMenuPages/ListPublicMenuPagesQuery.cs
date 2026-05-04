using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Content.Queries.ListPublicMenuPages;

public sealed record ListPublicMenuPagesQuery : IQuery<IReadOnlyList<PublicPageMenuItemDto>>;
