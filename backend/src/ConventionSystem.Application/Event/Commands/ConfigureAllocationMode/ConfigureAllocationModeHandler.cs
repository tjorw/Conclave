using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Commands.ConfigureAllocationMode;

public sealed class ConfigureAllocationModeHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<ConfigureAllocationModeCommand>
{
    protected override async Task ExecuteAsync(ConfigureAllocationModeCommand command, CancellationToken ct)
    {
        if (!Enum.TryParse<AllocationMode>(command.AllocationMode, out var mode))
            throw new ArgumentException($"Ogiltigt allokeringsläge: {command.AllocationMode}.");

        var ev = await eventRepository.GetByIdAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        var edition = await editionRepository.GetByIdAsync(ev.EditionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", ev.EditionId.Value.ToString());

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new ResourceNotFoundException("Konvention", edition.ConventionId.Value.ToString());

        if (!convention.IsAdministrator(currentUser.PersonId))
            throw new ForbiddenException("Utföraren är inte administratör för denna konvention.");

        ev.ConfigureAllocationMode(mode);
        await eventRepository.SaveAsync(ct);
    }
}
