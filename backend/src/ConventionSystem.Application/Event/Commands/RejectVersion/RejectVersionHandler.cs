using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Commands.RejectVersion;

public sealed class RejectVersionHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<RejectVersionCommand>
{
    protected override async Task ExecuteAsync(RejectVersionCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Comment))
            throw new InvalidOperationException("En kommentar måste anges vid avvisning.");

        var performedById = currentUser.PersonId;

        var ev = await eventRepository.GetByIdAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        var context = await EditionContextLoader.LoadWithCategoriesAsync(
            editionRepository,
            conventionRepository,
            ev.EditionId,
            ct);

        ApplicationAuthorization.EnsureConventionAdmin(context.Convention, performedById, "Endast administratörer kan avvisa en version av evenemanget.");
        ev.Reject(performedById, command.Comment);
        await eventRepository.SaveAsync(ct);
    }
}
