using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Commands.CancelVisitorRegistration;

public sealed class CancelVisitorRegistrationHandler(
    IVisitorRegistrationRepository visitorRegistrationRepository,
    ITicketRepository ticketRepository,
    ITicketTypeRepository ticketTypeRepository,
    ICurrentUser currentUser)
    : IRequestHandler<CancelVisitorRegistrationCommand>
{
    public async Task Handle(CancelVisitorRegistrationCommand command, CancellationToken ct)
    {
        var registrationId = new VisitorRegistrationId(command.VisitorRegistrationId);
        var performedById = currentUser.PersonId;

        var registration = await visitorRegistrationRepository.GetByIdAsync(registrationId, ct)
            ?? throw new ResourceNotFoundException("Besöksregistrering", command.VisitorRegistrationId.ToString());

        if (currentUser.PersonId != registration.PersonId && !currentUser.IsAdmin)
            throw new ForbiddenException("Du har inte behörighet att avboka denna besöksregistrering.");

        var ticket = await ticketRepository.GetByIdAsync(registration.TicketId, ct)
            ?? throw new ResourceNotFoundException("Biljett", registration.TicketId.Value.ToString());

        var ticketType = await ticketTypeRepository.GetByIdAsync(ticket.TicketTypeId, ct)
            ?? throw new ResourceNotFoundException("Biljetttyp", ticket.TicketTypeId.Value.ToString());

        var isFreeTicket = ticketType.Price == 0;

        if (registration.Status == VisitorRegistrationStatus.Confirmed && !currentUser.IsAdmin && !isFreeTicket)
            throw new ForbiddenException("Betalda biljetter med pris över 0 kr kan inte avbokas av besökaren. Kontakta arrangören.");

        registration.Cancel();
        ticket.Revoke(performedById);

        await visitorRegistrationRepository.SaveAsync(ct);
    }
}
