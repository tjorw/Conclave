using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.CancelTeamRegistration;

public sealed class CancelTeamRegistrationHandler(
    ITeamEventRegistrationRepository registrationRepository,
    ITeamRepository teamRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<CancelTeamRegistrationCommand>
{
    protected override async Task ExecuteAsync(CancelTeamRegistrationCommand command, CancellationToken ct)
    {
        var registrationId = new TeamEventRegistrationId(command.TeamEventRegistrationId);

        var registration = await registrationRepository.GetByIdAsync(registrationId, ct)
            ?? throw new ResourceNotFoundException("Laganmälan", command.TeamEventRegistrationId.ToString());

        var team = await teamRepository.GetByIdAsync(registration.TeamId, ct)
            ?? throw new ResourceNotFoundException("Lag", registration.TeamId.Value.ToString());

        var isCaption = team.CaptainPersonId == currentUser.PersonId;

        if (!isCaption)
        {
            var edition = await editionRepository.GetByIdAsync(registration.EditionId, ct)
                ?? throw new ResourceNotFoundException("Upplaga", registration.EditionId.Value.ToString());

            var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
                ?? throw new ResourceNotFoundException("Konvention", edition.ConventionId.Value.ToString());

            if (!convention.IsAdministrator(currentUser.PersonId))
                throw new ForbiddenException("Bara lagkaptenen eller en administratör kan avboka laganmälan.");
        }

        registration.Cancel(currentUser.PersonId);
        await registrationRepository.SaveAsync(ct);
    }
}
