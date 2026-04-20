using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.RevokeTicket;

public sealed class RevokeTicketHandler(
    ITicketRepository ticketRepository,
    ICurrentUser currentUser)
    : CommandHandler<RevokeTicketCommand>
{
    protected override async Task ExecuteAsync(RevokeTicketCommand command, CancellationToken ct)
    {
        var ticketId = new TicketId(command.TicketId);
        var performedById = currentUser.PersonId;

        var ticket = await ticketRepository.GetByIdAsync(ticketId, ct)
            ?? throw new ResourceNotFoundException("Biljett", command.TicketId.ToString());

        ticket.Revoke(performedById);
        await ticketRepository.SaveAsync(ct);
    }
}
