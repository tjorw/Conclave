using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Commands.CreateStation;

public sealed class CreateStationHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : ICommandHandler<CreateStationCommand, Guid>
{
    public async Task<Guid> Handle(CreateStationCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var performedById = currentUser.PersonId;
        var staffAreaId = new StaffAreaId(command.StaffAreaId);

        var context = await EditionContextLoader.LoadWithStructureAsync(
            editionRepository,
            conventionRepository,
            editionId,
            ct);
            
        ApplicationAuthorization.EnsureConventionAdmin(context.Convention, performedById, "Endast administratörer kan skapa en station.");

        var station = context.Edition.CreateStation(command.Name, staffAreaId, command.Description);
        await editionRepository.SaveAsync(ct);

        return station.Id.Value;
    }
}
