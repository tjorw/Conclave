using ConventionSystem.Domain.Common;

namespace ConventionSystem.Domain.Event.ValueObjects;

public sealed class EventProgramTag : ValueObject
{
    public string Name { get; }

    public EventProgramTag(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Taggnamn får inte vara tomt.", nameof(name));

        var normalizedName = name.Trim();
        if (normalizedName.Length > 64)
            throw new ArgumentException("Taggnamn får vara högst 64 tecken.", nameof(name));

        Name = normalizedName;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name.ToUpperInvariant();
    }
}
