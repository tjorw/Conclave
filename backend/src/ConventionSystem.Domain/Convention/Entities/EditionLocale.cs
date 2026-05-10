using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Domain.Convention.Entities;

public sealed class EditionLocale
{
    public EditionId EditionId { get; private set; }
    public string Locale { get; private set; } = string.Empty;
    public bool IsPrimary { get; private set; }

    private EditionLocale() { }

    internal EditionLocale(EditionId editionId, string locale, bool isPrimary)
    {
        EditionId = editionId;
        Locale = locale;
        IsPrimary = isPrimary;
    }
}
