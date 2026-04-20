using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.CancelOwnTicket;

public sealed class CancelOwnTicketHandler(
    ITicketRepository ticketRepository,
    ITicketTypeRepository ticketTypeRepository,
    ICurrentUser currentUser)
    : CommandHandler<CancelOwnTicketCommand>
{
    protected override async Task ExecuteAsync(CancelOwnTicketCommand command, CancellationToken ct)
    {
        var ticketId = new TicketId(command.TicketId);
        var ticket = await ticketRepository.GetByIdAsync(ticketId, ct)
            ?? throw new ResourceNotFoundException("Biljett", command.TicketId.ToString());

        if (ticket.PersonId != currentUser.PersonId)
            throw new ForbiddenException("Du kan bara avboka din egen biljett.");

        if (ticket.Status == TicketStatus.Paid)
        {
            var ticketType = await ticketTypeRepository.GetByIdAsync(ticket.TicketTypeId, ct)
                ?? throw new ResourceNotFoundException("Biljetttyp", ticket.TicketTypeId.Value.ToString());

            if (ticketType.Price == 0)
                ticket.Revoke(currentUser.PersonId);
            else
                ticket.CancelOwn();
        }
        else
        {
            ticket.CancelOwn();
        }

        await ticketRepository.SaveAsync(ct);
    }
}
