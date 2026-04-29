using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Commands.AdjustCoOrganiserLimit;

public sealed class AdjustCoOrganiserLimitHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<AdjustCoOrganiserLimitCommand>
{
    protected override async Task ExecuteAsync(AdjustCoOrganiserLimitCommand command, CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        var context = await EditionContextLoader.LoadAsync(editionRepository, conventionRepository, ev.EditionId, ct);
        ApplicationAuthorization.EnsureConventionAdmin(context.Convention, currentUser.PersonId, "Endast administratörer kan justera godkänt antal medarrangörer.");

        ev.AdjustCoOrganiserLimit(command.Limit);
        await eventRepository.SaveAsync(ct);
    }
}
