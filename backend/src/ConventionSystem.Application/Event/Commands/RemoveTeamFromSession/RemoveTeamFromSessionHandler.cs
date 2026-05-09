using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Commands.RemoveTeamFromSession;

public sealed class RemoveTeamFromSessionHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<RemoveTeamFromSessionCommand>
{
    protected override async Task ExecuteAsync(RemoveTeamFromSessionCommand command, CancellationToken ct)
    {
        var eventId = new EventId(command.EventId);
        var sessionId = new SessionId(command.SessionId);

        var ev = await eventRepository.GetByIdWithSessionsAndTeamAssignmentsAsync(eventId, ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        var context = await EditionContextLoader.LoadAsync(editionRepository, conventionRepository, ev.EditionId, ct);
        ApplicationAuthorization.EnsureConventionAdmin(
            context.Convention, currentUser.PersonId,
            "Endast administratörer kan ta bort lagtilldelningar.");

        ev.RemoveTeamFromSession(sessionId, command.TeamEventRegistrationId, currentUser.PersonId);
        await eventRepository.SaveAsync(ct);
    }
}
