using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.CancelSessionRegistration;

public sealed class CancelSessionRegistrationHandler(
    ISessionRegistrationRepository sessionRegistrationRepository,
    ICurrentUser currentUser)
    : CommandHandler<CancelSessionRegistrationCommand>
{
    protected override async Task ExecuteAsync(CancelSessionRegistrationCommand command, CancellationToken ct)
    {
        var registrationId = new SessionRegistrationId(command.SessionRegistrationId);

        var registration = await sessionRegistrationRepository.GetByIdAsync(registrationId, ct)
            ?? throw new ResourceNotFoundException("Sessionsregistrering", command.SessionRegistrationId.ToString());

        if (currentUser.PersonId != registration.PersonId && !currentUser.IsAdmin)
            throw new ForbiddenException("Du har inte behörighet att avboka denna sessionsregistrering.");

        registration.Cancel();
        await sessionRegistrationRepository.SaveAsync(ct);
    }
}
