using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Commands.UpdateStation;

public sealed class UpdateStationHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<UpdateStationCommand>
{
    protected override async Task ExecuteAsync(UpdateStationCommand command, CancellationToken ct)
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
            "Utföraren har inte behörighet att uppdatera denna station.");

        context.Edition.UpdateStation(stationId, command.Name, command.Description);
        await editionRepository.SaveAsync(ct);
    }
}
