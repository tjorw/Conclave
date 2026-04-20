using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.ConfirmTicketPaymentWebhook;

public sealed class ConfirmTicketPaymentWebhookHandler(
    IVisitorRegistrationRepository visitorRegistrationRepository,
    ITicketRepository ticketRepository)
    : CommandHandler<ConfirmTicketPaymentWebhookCommand>
{
    protected override async Task ExecuteAsync(ConfirmTicketPaymentWebhookCommand command, CancellationToken ct)
    {
        if (!IsSuccessful(command.PaymentStatus))
            return;

        var registrationId = new VisitorRegistrationId(command.VisitorRegistrationId);
        var registration = await visitorRegistrationRepository.GetByIdAsync(registrationId, ct)
            ?? throw new ResourceNotFoundException("Besöksregistrering", command.VisitorRegistrationId.ToString());

        if (registration.Status == VisitorRegistrationStatus.Confirmed)
        {
            if (registration.PaymentReference == command.ExternalReference)
                return;

            throw new DomainRuleViolationException("Betalningen är redan bekräftad med en annan referens.");
        }

        if (registration.Status != VisitorRegistrationStatus.PendingPayment)
            return;

        var ticket = await ticketRepository.GetByIdAsync(registration.TicketId, ct)
            ?? throw new ResourceNotFoundException("Biljett", registration.TicketId.Value.ToString());

        registration.ConfirmPayment(command.ExternalReference);
        ticket.ConfirmPayment();

        await visitorRegistrationRepository.SaveAsync(ct);
    }

    private static bool IsSuccessful(string status)
        => status.Equals("Paid", StringComparison.OrdinalIgnoreCase)
           || status.Equals("Succeeded", StringComparison.OrdinalIgnoreCase)
           || status.Equals("Success", StringComparison.OrdinalIgnoreCase);
}
