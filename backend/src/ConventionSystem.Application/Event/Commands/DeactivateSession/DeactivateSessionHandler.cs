using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
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

        var edition = await editionRepository.GetByIdWithCategoriesAsync(ev.EditionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", ev.EditionId.Value.ToString());

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new ResourceNotFoundException("Konvention", edition.ConventionId.Value.ToString());

        if (!convention.IsAdministrator(performedById)
            && !edition.IsCategoryResponsible(ev.CategoryId, performedById))
            throw new ForbiddenException("Utföraren har inte behörighet att inaktivera sessioner för detta evenemang.");

        ev.DeactivateSession(sessionId, performedById);
        await eventRepository.SaveAsync(ct);
    }
}
