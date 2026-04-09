using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Commands.CancelSessionRegistration;

public sealed class CancelSessionRegistrationHandler(
    ISessionRegistrationRepository sessionRegistrationRepository)
    : IRequestHandler<CancelSessionRegistrationCommand>
{
    public async Task Handle(CancelSessionRegistrationCommand command, CancellationToken ct)
    {
        var registrationId = new SessionRegistrationId(command.SessionRegistrationId);

        var registration = await sessionRegistrationRepository.GetByIdAsync(registrationId, ct)
            ?? throw new InvalidOperationException($"Sessionsregistreringen '{command.SessionRegistrationId}' hittades inte.");

        registration.Cancel();
        await sessionRegistrationRepository.SaveAsync(ct);
    }
}
