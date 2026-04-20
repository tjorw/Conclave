using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Commands.RemoveStaffArea;

public sealed class RemoveStaffAreaHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<RemoveStaffAreaCommand>
{
    protected override async Task ExecuteAsync(RemoveStaffAreaCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var performedById = currentUser.PersonId;

        var context = await EditionContextLoader.LoadWithStructureAsync(
            editionRepository,
            conventionRepository,
            editionId,
            ct);

        ApplicationAuthorization.EnsureConventionAdmin(
            context.Convention,
            performedById,
            "Utföraren är inte administratör för denna konvention.");

        var (area, stations) = context.Edition.RemoveStaffArea(new StaffAreaId(command.StaffAreaId));
        foreach (var station in stations) editionRepository.MarkAsRemoved(station);
        editionRepository.MarkAsRemoved(area);
        await editionRepository.SaveAsync(ct);
    }
}
