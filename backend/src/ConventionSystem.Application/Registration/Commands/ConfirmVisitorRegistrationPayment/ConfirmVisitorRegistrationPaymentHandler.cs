using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Commands.ConfirmVisitorRegistrationPayment;

public sealed class ConfirmVisitorRegistrationPaymentHandler(
    IVisitorRegistrationRepository visitorRegistrationRepository,
    ITicketRepository ticketRepository)
    : IRequestHandler<ConfirmVisitorRegistrationPaymentCommand>
{
    public async Task Handle(ConfirmVisitorRegistrationPaymentCommand command, CancellationToken ct)
    {
        var registrationId = new VisitorRegistrationId(command.VisitorRegistrationId);

        var registration = await visitorRegistrationRepository.GetByIdAsync(registrationId, ct)
            ?? throw new ResourceNotFoundException("Besöksregistrering", command.VisitorRegistrationId.ToString());

        var ticket = await ticketRepository.GetByIdAsync(registration.TicketId, ct)
            ?? throw new ResourceNotFoundException("Biljett", registration.TicketId.Value.ToString());

        registration.ConfirmPayment(command.ExternalReference);
        ticket.ConfirmPayment();

        await visitorRegistrationRepository.SaveAsync(ct);
    }
}
