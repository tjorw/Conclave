using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Commands.ConfigureTeamRegistration;

public sealed class ConfigureTeamRegistrationHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<ConfigureTeamRegistrationCommand>
{
    protected override async Task ExecuteAsync(ConfigureTeamRegistrationCommand command, CancellationToken ct)
    {
        if (!Enum.TryParse<RegistrationMode>(command.RegistrationMode, out var mode))
            throw new ArgumentException($"Ogiltigt anmälningsläge: {command.RegistrationMode}.");

        var ev = await eventRepository.GetByIdAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        var edition = await editionRepository.GetByIdAsync(ev.EditionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", ev.EditionId.Value.ToString());

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new ResourceNotFoundException("Konvention", edition.ConventionId.Value.ToString());

        if (!convention.IsAdministrator(currentUser.PersonId))
            throw new ForbiddenException("Utföraren är inte administratör för denna konvention.");

        ev.ConfigureTeamRegistration(mode, command.MinTeamSize, command.MaxTeamSize);
        await eventRepository.SaveAsync(ct);
    }
}
