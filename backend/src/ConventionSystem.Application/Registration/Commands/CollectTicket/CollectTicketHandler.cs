using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Commands.CollectTicket;

public sealed class CollectTicketHandler(
    ITicketRepository ticketRepository,
    ICurrentUser currentUser)
    : IRequestHandler<CollectTicketCommand>
{
    public async Task Handle(CollectTicketCommand command, CancellationToken ct)
    {
        var ticketId = new TicketId(command.TicketId);
        var performedById = currentUser.PersonId;

        var ticket = await ticketRepository.GetByIdAsync(ticketId, ct)
            ?? throw new InvalidOperationException($"Biljetten '{command.TicketId}' hittades inte.");

        ticket.Collect(performedById);
        await ticketRepository.SaveAsync(ct);
    }
}
