using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Commands.CancelVisitorRegistration;

public sealed class CancelVisitorRegistrationHandler(
    IVisitorRegistrationRepository visitorRegistrationRepository,
    ITicketRepository ticketRepository,
    ICurrentUser currentUser)
    : IRequestHandler<CancelVisitorRegistrationCommand>
{
    public async Task Handle(CancelVisitorRegistrationCommand command, CancellationToken ct)
    {
        var registrationId = new VisitorRegistrationId(command.VisitorRegistrationId);
        var performedById = currentUser.PersonId;

        var registration = await visitorRegistrationRepository.GetByIdAsync(registrationId, ct)
            ?? throw new InvalidOperationException($"Besöksregistreringen '{command.VisitorRegistrationId}' hittades inte.");

        if (currentUser.PersonId != registration.PersonId && !currentUser.IsAdmin)
            throw new UnauthorizedAccessException("Du har inte behörighet att avboka denna besöksregistrering.");

        var ticket = await ticketRepository.GetByIdAsync(registration.TicketId, ct)
            ?? throw new InvalidOperationException("Biljetten för registreringen hittades inte.");

        registration.Cancel();
        ticket.Revoke(performedById);

        await visitorRegistrationRepository.SaveAsync(ct);
    }
}
