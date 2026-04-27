using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Commands.RejectCoOrganiserApplication;

public sealed class RejectCoOrganiserApplicationHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<RejectCoOrganiserApplicationCommand>
{
    protected override async Task ExecuteAsync(RejectCoOrganiserApplicationCommand command, CancellationToken ct)
    {
        var performedById = currentUser.PersonId;

        var ev = await eventRepository.GetByIdWithCoOrganisersAndApplicationsAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        var context = await EditionContextLoader.LoadWithCategoriesAsync(
            editionRepository,
            conventionRepository,
            ev.EditionId,
            ct);

        ApplicationAuthorization.EnsureConventionAdmin(context.Convention, performedById, "Endast administratörer kan avslå medarrangörsansökningar.");
        ev.RejectCoOrganiserApplication(
            new CoOrganiserApplicationId(command.ApplicationId),
            performedById,
            command.Comment);
        await eventRepository.SaveAsync(ct);
    }
}
