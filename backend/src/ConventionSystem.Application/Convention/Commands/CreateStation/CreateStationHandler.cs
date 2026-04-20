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

        ApplicationAuthorization.EnsureStaffAreaManager(
            context.Convention,
            context.Edition,
            staffAreaId,
            performedById,
            "Utföraren har inte behörighet att skapa stationer för detta funktionsområde.");

        var station = context.Edition.CreateStation(command.Name, staffAreaId, command.Description);
        await editionRepository.SaveAsync(ct);

        return station.Id.Value;
    }
}
