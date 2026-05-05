using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Commands.SetFeatured;

public sealed class SetFeaturedHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<SetFeaturedCommand>
{
    private const int MaxFeaturedPerEdition = 6;

    protected override async Task ExecuteAsync(SetFeaturedCommand command, CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        var edition = await editionRepository.GetByIdAsync(ev.EditionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", ev.EditionId.Value.ToString());

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new ResourceNotFoundException("Konvention", edition.ConventionId.Value.ToString());

        if (!convention.IsAdministrator(currentUser.PersonId))
            throw new ForbiddenException("Utföraren är inte administratör för denna konvention.");

        if (command.IsFeatured && !ev.IsFeatured)
        {
            var featuredCount = await eventRepository.CountFeaturedByEditionIdAsync(ev.EditionId, ct);
            if (featuredCount >= MaxFeaturedPerEdition)
                throw new EventFeaturedLimitExceededException();
        }

        ev.SetFeatured(command.IsFeatured, command.FeaturedSortOrder);
        await eventRepository.SaveAsync(ct);
    }
}
