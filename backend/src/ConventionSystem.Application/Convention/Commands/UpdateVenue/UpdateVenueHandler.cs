using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Commands.UpdateVenue;

public sealed class UpdateVenueHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<UpdateVenueCommand>
{
    protected override async Task ExecuteAsync(UpdateVenueCommand command, CancellationToken ct)
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

        context.Edition.UpdateVenue(new VenueId(command.VenueId), command.Name, command.Building, command.Description);
        await editionRepository.SaveAsync(ct);
    }
}
