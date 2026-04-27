using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Commands.DeactivateSession;

public sealed class DeactivateSessionHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<DeactivateSessionCommand>
{
    protected override async Task ExecuteAsync(DeactivateSessionCommand command, CancellationToken ct)
    {
        var sessionId = new SessionId(command.SessionId);
        var performedById = currentUser.PersonId;

        var ev = await eventRepository.GetByIdWithSessionsAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        var context = await EditionContextLoader.LoadWithCategoriesAsync(
            editionRepository,
            conventionRepository,
            ev.EditionId,
            ct);
        ApplicationAuthorization.EnsureConventionAdmin(context.Convention, performedById, "Endast administratörer kan avaktivera ett pass.");
        ev.DeactivateSession(sessionId, performedById);
        await eventRepository.SaveAsync(ct);
    }
}
