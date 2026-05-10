using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Domain.Convention.Entities;

public sealed class ProgramTagTranslation
{
    public EditionId EditionId { get; private set; }
    public string TagName { get; private set; } = string.Empty;
    public string Locale { get; private set; } = string.Empty;
    public string TranslatedName { get; private set; } = string.Empty;

    private ProgramTagTranslation() { }

    internal ProgramTagTranslation(EditionId editionId, string tagName, string locale, string translatedName)
    {
        EditionId = editionId;
        TagName = tagName;
        Locale = locale;
        TranslatedName = translatedName;
    }

    internal void Update(string translatedName)
    {
        TranslatedName = translatedName;
    }
}
