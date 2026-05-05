using System.Text.RegularExpressions;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Events;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Domain.Convention.Entities;

public sealed partial class ConventionBranding : AggregateRoot
{
    public const int CustomCssMaxLength = 5000;

    public static readonly IReadOnlySet<string> AllowedFontFamilies = new HashSet<string>(StringComparer.Ordinal)
    {
        "Inter",
        "Roboto",
        "Open Sans",
        "Lato",
        "Merriweather",
    };

    public ConventionId ConventionId { get; private set; }
    public string PrimaryColor { get; private set; } = string.Empty;
    public string AccentColor { get; private set; } = string.Empty;
    public string? LogoUrl { get; private set; }
    public string? FaviconUrl { get; private set; }
    public string FontFamily { get; private set; } = string.Empty;
    public string? CustomCss { get; private set; }

    private ConventionBranding() { }

    public ConventionBranding(
        ConventionId conventionId,
        string primaryColor,
        string accentColor,
        string? logoUrl,
        string? faviconUrl,
        string fontFamily,
        string? customCss)
    {
        ConventionId = conventionId;
        Update(primaryColor, accentColor, logoUrl, faviconUrl, fontFamily, customCss);
    }

    public void Update(
        string primaryColor,
        string accentColor,
        string? logoUrl,
        string? faviconUrl,
        string fontFamily,
        string? customCss)
    {
        ValidateHexColor(primaryColor, nameof(primaryColor));
        ValidateHexColor(accentColor, nameof(accentColor));
        ValidateFontFamily(fontFamily);
        ValidateCustomCss(customCss);

        PrimaryColor = primaryColor;
        AccentColor = accentColor;
        LogoUrl = NormalizeOptionalUrl(logoUrl);
        FaviconUrl = NormalizeOptionalUrl(faviconUrl);
        FontFamily = fontFamily;
        CustomCss = string.IsNullOrWhiteSpace(customCss) ? null : customCss;

        RaiseDomainEvent(new ConventionBrandingUpdated(ConventionId, DateTimeOffset.UtcNow));
    }

    public static void ValidateHexColor(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || !HexColorRegex().IsMatch(value))
            throw new InvalidOperationException($"{parameterName} måste anges som hex-färg i formatet #rrggbb.");
    }

    public static void ValidateFontFamily(string value)
    {
        if (!AllowedFontFamilies.Contains(value))
            throw new InvalidOperationException($"Typsnittet '{value}' är inte tillåtet.");
    }

    public static void ValidateCustomCss(string? value)
    {
        if (value?.Length > CustomCssMaxLength)
            throw new InvalidOperationException($"CustomCss får vara högst {CustomCssMaxLength} tecken.");
    }

    private static string? NormalizeOptionalUrl(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColorRegex();
}
