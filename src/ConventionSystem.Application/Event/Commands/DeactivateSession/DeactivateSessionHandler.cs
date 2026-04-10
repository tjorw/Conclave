using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using MediatR;

namespace ConventionSystem.Application.Event.Commands.DeactivateSession;

public sealed class DeactivateSessionHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository)
    : IRequestHandler<DeactivateSessionCommand>
{
    public async Task Handle(DeactivateSessionCommand command, CancellationToken ct)
    {
        var sessionId = new SessionId(command.SessionId);
        var performedById = new PersonId(command.PerformedById);

        var ev = await eventRepository.GetByIdWithSessionsAsync(new EventId(command.EventId), ct)
            ?? throw new InvalidOperationException($"Evenemanget '{command.EventId}' hittades inte.");

        var edition = await editionRepository.GetByIdWithCategoriesAsync(ev.EditionId, ct)
            ?? throw new InvalidOperationException("Upplagan hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(performedById)
            && !edition.IsCategoryResponsible(ev.CategoryId, performedById))
            throw new InvalidOperationException("Utföraren har inte behörighet att inaktivera sessioner för detta evenemang.");

        ev.DeactivateSession(sessionId, performedById);
        await eventRepository.SaveAsync(ct);
    }
}
