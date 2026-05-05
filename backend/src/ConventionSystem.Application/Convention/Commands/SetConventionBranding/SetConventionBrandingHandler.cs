using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Commands.SetConventionBranding;

public sealed class SetConventionBrandingHandler(
    IConventionRepository conventionRepository,
    IConventionBrandingRepository brandingRepository,
    ICurrentUser currentUser)
    : CommandHandler<SetConventionBrandingCommand>
{
    protected override async Task ExecuteAsync(SetConventionBrandingCommand command, CancellationToken ct)
    {
        var conventionId = new ConventionId(command.ConventionId);
        var convention = await conventionRepository.GetByIdAsync(conventionId, ct)
            ?? throw new InvalidOperationException($"Konvention '{command.ConventionId}' hittades inte.");

        if (!convention.IsAdministrator(currentUser.PersonId))
            throw new ForbiddenException("Utföraren är inte administratör för denna konvention.");

        var branding = await brandingRepository.GetByConventionIdAsync(conventionId, ct);

        if (branding is null)
        {
            branding = new ConventionBranding(
                conventionId,
                command.PrimaryColor,
                command.AccentColor,
                command.LogoUrl,
                command.FaviconUrl,
                command.FontFamily,
                command.CustomCss);

            await brandingRepository.AddAsync(branding, ct);
        }
        else
        {
            branding.Update(
                command.PrimaryColor,
                command.AccentColor,
                command.LogoUrl,
                command.FaviconUrl,
                command.FontFamily,
                command.CustomCss);
        }

        await brandingRepository.SaveAsync(ct);
    }
}
