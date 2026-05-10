using ConventionSystem.Domain.Content.Ids;

namespace ConventionSystem.Domain.Content.Entities;

public sealed class PageTranslation
{
    public PageId PageId { get; private set; }
    public string Locale { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;

    private PageTranslation() { }

    internal PageTranslation(PageId pageId, string locale, string title, string content)
    {
        PageId = pageId;
        Locale = locale;
        Title = title;
        Content = content;
    }

    internal void Update(string title, string content)
    {
        Title = title;
        Content = content;
    }
}
