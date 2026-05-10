namespace ConventionSystem.Domain.Shared;

public static class LocaleConstants
{
    public static readonly IReadOnlyList<string> SupportedLocales = ["sv", "en"];

    public static bool IsSupported(string locale) =>
        SupportedLocales.Contains(locale, StringComparer.OrdinalIgnoreCase);
}
