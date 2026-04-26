using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Commands.RemoveReceptionStaff;

public sealed class RemoveReceptionStaffHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<RemoveReceptionStaffCommand>
{
    protected override async Task ExecuteAsync(RemoveReceptionStaffCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var context = await EditionContextLoader.LoadWithReceptionStaffAsync(editionRepository, conventionRepository, editionId, ct);

        if (!context.Convention.IsAdministrator(currentUser.PersonId))
            throw new ForbiddenException("Utföraren är inte administratör för denna konvention.");

        context.Edition.RemoveReceptionStaff(new PersonId(command.PersonId), currentUser.PersonId);
        await editionRepository.SaveAsync(ct);
    }
}
