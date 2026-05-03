using ConventionSystem.Domain.Common;

namespace ConventionSystem.Domain.Convention.ValueObjects;

public sealed class ProgramTagDefinition : ValueObject
{
    public string Name { get; }

    public ProgramTagDefinition(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Taggnamn får inte vara tomt.", nameof(name));

        if (name.Trim().Length > 64)
            throw new ArgumentException("Taggnamn får vara högst 64 tecken.", nameof(name));

        Name = name.Trim();
    }

    public bool EqualsName(string otherName)
        => string.Equals(Name, otherName.Trim(), StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name.ToUpperInvariant();
    }
}