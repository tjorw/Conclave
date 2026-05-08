using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.ConfirmTeamRegistration;

public sealed class ConfirmTeamRegistrationHandler(
    ITeamEventRegistrationRepository registrationRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<ConfirmTeamRegistrationCommand>
{
    protected override async Task ExecuteAsync(ConfirmTeamRegistrationCommand command, CancellationToken ct)
    {
        var registrationId = new TeamEventRegistrationId(command.TeamEventRegistrationId);

        var registration = await registrationRepository.GetByIdAsync(registrationId, ct)
            ?? throw new ResourceNotFoundException("Laganmälan", command.TeamEventRegistrationId.ToString());

        var edition = await editionRepository.GetByIdAsync(registration.EditionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", registration.EditionId.Value.ToString());

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new ResourceNotFoundException("Konvention", edition.ConventionId.Value.ToString());

        if (!convention.IsAdministrator(currentUser.PersonId))
            throw new ForbiddenException("Endast administratör kan bekräfta en laganmälan.");

        registration.Confirm();
        await registrationRepository.SaveAsync(ct);
    }
}
