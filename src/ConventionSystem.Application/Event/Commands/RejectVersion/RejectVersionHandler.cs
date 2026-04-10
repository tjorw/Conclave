using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using MediatR;

namespace ConventionSystem.Application.Event.Commands.RejectVersion;

public sealed class RejectVersionHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : IRequestHandler<RejectVersionCommand>
{
    public async Task Handle(RejectVersionCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Comment))
            throw new InvalidOperationException("En kommentar måste anges vid avvisning.");

        var performedById = currentUser.PersonId;

        var ev = await eventRepository.GetByIdWithDraftVersionAsync(new EventId(command.EventId), ct)
            ?? throw new InvalidOperationException($"Evenemanget '{command.EventId}' hittades inte.");

        var edition = await editionRepository.GetByIdWithCategoriesAsync(ev.EditionId, ct)
            ?? throw new InvalidOperationException("Upplagan hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(performedById)
            && !edition.IsCategoryResponsible(ev.CategoryId, performedById))
            throw new InvalidOperationException("Utföraren har inte behörighet att avvisa evenemang i denna kategori.");

        ev.RejectVersion(performedById, command.Comment);
        await eventRepository.SaveAsync(ct);
    }
}
