using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Commands.RemoveStation;

public sealed class RemoveStationHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<RemoveStationCommand>
{
    protected override async Task ExecuteAsync(RemoveStationCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var stationId = new StationId(command.StationId);
        var performedById = currentUser.PersonId;

        var context = await EditionContextLoader.LoadWithStructureAsync(
            editionRepository,
            conventionRepository,
            editionId,
            ct);

        ApplicationAuthorization.EnsureStationManager(
            context.Convention,
            context.Edition,
            stationId,
            performedById,
            "Utföraren har inte behörighet att ta bort denna station.");

        var station = context.Edition.RemoveStation(stationId);
        editionRepository.MarkAsRemoved(station);
        await editionRepository.SaveAsync(ct);
    }
}
