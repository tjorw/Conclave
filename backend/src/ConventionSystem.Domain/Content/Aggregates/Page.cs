using System.Text.RegularExpressions;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Content.Events;
using ConventionSystem.Domain.Content.Ids;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Domain.Content.Aggregates;

public sealed class Page : AggregateRoot
{
    private static readonly Regex SlugRegex = new("^[a-z0-9-]+$", RegexOptions.Compiled);

    public PageId Id { get; private set; }
    public ConventionId ConventionId { get; private set; }
    public EditionId? EditionId { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public bool IsPublished { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Page() { }

    public Page(PageId id, ConventionId conventionId, EditionId? editionId, string slug, string title, string content)
    {
        Id = id;
        ConventionId = conventionId;
        EditionId = editionId;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
        Update(slug, title, content, editionId);
    }

    public void Update(string slug, string title, string content, EditionId? editionId)
    {
        Slug = NormalizeSlug(slug);
        Title = NormalizeTitle(title);
        Content = NormalizeContent(content);
        EditionId = editionId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Publish()
    {
        if (IsPublished) return;
        IsPublished = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new PagePublished(Id, Slug, UpdatedAt));
    }

    public void Unpublish()
    {
        if (!IsPublished) return;
        IsPublished = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new PageUnpublished(Id, Slug, UpdatedAt));
    }

    private static string NormalizeSlug(string slug)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        if (normalized.Length is < 1 or > 200 || !SlugRegex.IsMatch(normalized))
            throw new ArgumentException("Slug får bara innehålla gemener, siffror och bindestreck och vara max 200 tecken.", nameof(slug));
        return normalized;
    }

    private static string NormalizeTitle(string title)
    {
        var normalized = title.Trim();
        if (normalized.Length is < 1 or > 300)
            throw new ArgumentException("Titel måste anges och vara max 300 tecken.", nameof(title));
        return normalized;
    }

    private static string NormalizeContent(string content)
    {
        var normalized = content.Trim();
        if (normalized.Length > 20_000)
            throw new ArgumentException("Innehåll får inte vara längre än 20 000 tecken.", nameof(content));
        return normalized;
    }
}
