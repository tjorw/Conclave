using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Commands.RemoveEdition;

public sealed class RemoveEditionHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<RemoveEditionCommand>
{
    protected override async Task ExecuteAsync(RemoveEditionCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var edition = await editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", command.EditionId.ToString());

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new ResourceNotFoundException("Konvention", edition.ConventionId.Value.ToString());

        ApplicationAuthorization.EnsureConventionAdmin(
            convention,
            currentUser.PersonId,
            "Utföraren är inte administratör för denna konvention.");

        if (convention.ActiveEditionId == editionId)
            throw new InvalidOperationException("Aktiv upplaga kan inte tas bort. Välj en annan aktiv upplaga först.");

        await editionRepository.DeleteGraphAndSaveAsync(editionId, ct);
    }
}
