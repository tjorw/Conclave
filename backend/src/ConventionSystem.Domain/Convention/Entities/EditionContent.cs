namespace ConventionSystem.Domain.Convention.Entities;

public sealed class EditionContent
{
    private EditionContent() { }

    public EditionContent(string key, string value)
    {
        Key = key;
        Value = value.Trim();
    }

    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;

    internal void SetValue(string value) => Value = value.Trim();
}
