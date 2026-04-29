using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Commands.CreateCoOrganiserInvitation;

public sealed class CreateCoOrganiserInvitationHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<CreateCoOrganiserInvitationCommand>
{
    protected override async Task ExecuteAsync(CreateCoOrganiserInvitationCommand command, CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdWithCoOrganisersAndInvitationsAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        var context = await EditionContextLoader.LoadAsync(editionRepository, conventionRepository, ev.EditionId, ct);
        ApplicationAuthorization.EnsureConventionAdminOrOwner(
            context.Convention,
            ev.LeadOrganiserId,
            currentUser.PersonId,
            "Endast huvudarrangören eller administratörer kan skapa inbjudningar.");

        ev.CreateInvitation(command.Email, currentUser.PersonId);
        await eventRepository.SaveAsync(ct);
    }
}
