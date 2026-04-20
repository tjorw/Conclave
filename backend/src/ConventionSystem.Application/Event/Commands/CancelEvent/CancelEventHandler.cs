using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Commands.CancelEvent;

public sealed class CancelEventHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<CancelEventCommand>
{
    protected override async Task ExecuteAsync(CancelEventCommand command, CancellationToken ct)
    {
        var performedById = currentUser.PersonId;

        var ev = await eventRepository.GetByIdAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        var edition = await editionRepository.GetByIdWithCategoriesAsync(ev.EditionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", ev.EditionId.Value.ToString());

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new ResourceNotFoundException("Konvention", edition.ConventionId.Value.ToString());

        if (!convention.IsAdministrator(performedById)
            && !edition.IsCategoryResponsible(ev.CategoryId, performedById))
            throw new ForbiddenException("Utföraren har inte behörighet att ställa in detta evenemang.");

        ev.CancelEvent(performedById);
        await eventRepository.SaveAsync(ct);
    }
}
