namespace ConventionSystem.Application.Convention.Queries;

public sealed record ConventionBrandingDto(
    Guid ConventionId,
    string PrimaryColor,
    string AccentColor,
    string? LogoUrl,
    string? FaviconUrl,
    string FontFamily,
    string? CustomCss);
