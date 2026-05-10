using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Shared;

namespace ConventionSystem.Domain.Convention.Entities;

public sealed class Category : Entity<CategoryId>
{
    private readonly List<CategoryTranslation> _translations = [];

    public PersonId ResponsibleId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? OrganizerInstructions { get; private set; }
    public string? PublicDescription { get; private set; }
    public IReadOnlyList<CategoryTranslation> Translations => _translations.AsReadOnly();

    private Category() { }

    internal Category(CategoryId id, PersonId responsibleId, string name,
        string? organizerInstructions, string? publicDescription)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Namn får inte vara tomt.", nameof(name));
        ResponsibleId = responsibleId;
        Name = name;
        OrganizerInstructions = organizerInstructions;
        PublicDescription = publicDescription;
    }

    internal void UpsertTranslation(string locale, string name)
    {
        if (!LocaleConstants.IsSupported(locale))
            throw new ArgumentException($"Språket '{locale}' stöds inte.", nameof(locale));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Namn får inte vara tomt.", nameof(name));

        var existing = _translations.FirstOrDefault(t =>
            t.Locale.Equals(locale, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
            existing.Update(name.Trim());
        else
            _translations.Add(new CategoryTranslation(Id, locale.ToLowerInvariant(), name.Trim()));
    }

    internal void ChangeResponsible(PersonId personId) => ResponsibleId = personId;

    internal void Update(string name, string? organizerInstructions, string? publicDescription, PersonId responsibleId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Namn får inte vara tomt.", nameof(name));
        Name = name;
        OrganizerInstructions = organizerInstructions;
        PublicDescription = publicDescription;
        ResponsibleId = responsibleId;
    }
}
