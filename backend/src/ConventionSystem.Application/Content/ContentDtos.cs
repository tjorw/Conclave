namespace ConventionSystem.Application.Content;

public sealed record PageSummaryDto(
    Guid Id,
    string Slug,
    string Title,
    Guid? EditionId,
    bool IsPublished,
    bool ShowInPublicMenu,
    int MenuSortOrder,
    DateTimeOffset UpdatedAt);

public sealed record PageDto(
    Guid Id,
    string Slug,
    string Title,
    string Content,
    Guid? EditionId,
    bool IsPublished,
    bool ShowInPublicMenu,
    int MenuSortOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PublicPageDto(
    string Slug,
    string Title,
    string Content,
    Guid? EditionId);

public sealed record PublicPageMenuItemDto(
    string Slug,
    string Title,
    int MenuSortOrder,
    Guid? EditionId);
