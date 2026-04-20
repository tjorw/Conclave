using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.RegisterManualTicketPayment;

public sealed class RegisterManualTicketPaymentHandler(
    ITicketRepository ticketRepository,
    IVisitorRegistrationRepository visitorRegistrationRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<RegisterManualTicketPaymentCommand>
{
    protected override async Task ExecuteAsync(RegisterManualTicketPaymentCommand command, CancellationToken ct)
    {
        var ticketId = new TicketId(command.TicketId);
        var ticket = await ticketRepository.GetByIdAsync(ticketId, ct)
            ?? throw new ResourceNotFoundException("Biljett", command.TicketId.ToString());

        var edition = await editionRepository.GetByIdAsync(ticket.EditionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", ticket.EditionId.Value.ToString());

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new ResourceNotFoundException("Konvent", edition.ConventionId.Value.ToString());

        if (!convention.IsAdministrator(currentUser.PersonId))
            throw new ForbiddenException("Du har inte behörighet att registrera manuell biljettbetalning.");

        var registration = await visitorRegistrationRepository.GetByTicketIdAsync(ticketId, ct);
        ticket.ConfirmPayment();

        if (registration is not null)
        {
            var reference = string.IsNullOrWhiteSpace(command.ExternalReference)
                ? $"MANUAL-{ticket.Id.Value:N}"
                : command.ExternalReference.Trim();

            registration.ConfirmPayment(reference);
        }

        if (registration is not null)
            await visitorRegistrationRepository.SaveAsync(ct);
        else
            await ticketRepository.SaveAsync(ct);
    }
}
