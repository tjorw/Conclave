using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Convention.Commands.SetConventionBranding;

public sealed record SetConventionBrandingCommand(
    Guid ConventionId,
    string PrimaryColor,
    string AccentColor,
    string? LogoUrl,
    string? FaviconUrl,
    string FontFamily,
    string? CustomCss) : ICommand;
