using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Commands.SetEditionLocales;

public sealed class SetEditionLocalesHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser) : CommandHandler<SetEditionLocalesCommand>
{
    protected override async Task ExecuteAsync(SetEditionLocalesCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var edition = await editionRepository.GetByIdWithLocalesAsync(editionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", command.EditionId.ToString());

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new ResourceNotFoundException("Konvention", edition.ConventionId.Value.ToString());

        if (!convention.IsAdministrator(currentUser.PersonId))
            throw new ForbiddenException("Utföraren är inte administratör för denna konvention.");

        edition.ConfigureLocales(command.Locales, command.PrimaryLocale, currentUser.PersonId);
        await editionRepository.SaveAsync(ct);
    }
}
