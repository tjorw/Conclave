using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Domain.Convention.Entities;

public sealed class CategoryTranslation
{
    public CategoryId CategoryId { get; private set; }
    public string Locale { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    private CategoryTranslation() { }

    internal CategoryTranslation(CategoryId categoryId, string locale, string name)
    {
        CategoryId = categoryId;
        Locale = locale;
        Name = name;
    }

    internal void Update(string name)
    {
        Name = name;
    }
}
