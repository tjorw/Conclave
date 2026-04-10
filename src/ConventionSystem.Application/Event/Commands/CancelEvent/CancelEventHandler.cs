using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using MediatR;

namespace ConventionSystem.Application.Event.Commands.CancelEvent;

public sealed class CancelEventHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository)
    : IRequestHandler<CancelEventCommand>
{
    public async Task Handle(CancelEventCommand command, CancellationToken ct)
    {
        var performedById = new PersonId(command.PerformedById);

        var ev = await eventRepository.GetByIdAsync(new EventId(command.EventId), ct)
            ?? throw new InvalidOperationException($"Evenemanget '{command.EventId}' hittades inte.");

        var edition = await editionRepository.GetByIdWithCategoriesAsync(ev.EditionId, ct)
            ?? throw new InvalidOperationException("Upplagan hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(performedById)
            && !edition.IsCategoryResponsible(ev.CategoryId, performedById))
            throw new InvalidOperationException("Utföraren har inte behörighet att ställa in detta evenemang.");

        ev.CancelEvent(performedById);
        await eventRepository.SaveAsync(ct);
    }
}
