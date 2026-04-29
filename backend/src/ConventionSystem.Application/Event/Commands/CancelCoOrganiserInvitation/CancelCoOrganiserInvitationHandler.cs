using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Commands.CancelCoOrganiserInvitation;

public sealed class CancelCoOrganiserInvitationHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<CancelCoOrganiserInvitationCommand>
{
    protected override async Task ExecuteAsync(CancelCoOrganiserInvitationCommand command, CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdWithCoOrganisersAndInvitationsAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        var context = await EditionContextLoader.LoadAsync(editionRepository, conventionRepository, ev.EditionId, ct);
        ApplicationAuthorization.EnsureConventionAdminOrOwner(
            context.Convention,
            ev.LeadOrganiserId,
            currentUser.PersonId,
            "Endast huvudarrangören eller administratörer kan avbryta inbjudningar.");

        ev.CancelInvitation(new CoOrganiserInvitationId(command.InvitationId), currentUser.PersonId);
        await eventRepository.SaveAsync(ct);
    }
}
