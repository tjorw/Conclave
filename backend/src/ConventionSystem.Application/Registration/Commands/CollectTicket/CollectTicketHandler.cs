using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.CollectTicket;

public sealed class CollectTicketHandler(
    ITicketRepository ticketRepository,
    ITicketTypeRepository ticketTypeRepository,
    ICurrentUser currentUser)
    : ICommandHandler<CollectTicketCommand, CollectTicketResult>
{
    public async Task<CollectTicketResult> Handle(CollectTicketCommand command, CancellationToken ct)
    {
        var ticketId = new TicketId(command.TicketId);
        var performedById = currentUser.PersonId;

        var ticket = await ticketRepository.GetByIdAsync(ticketId, ct)
            ?? throw new ResourceNotFoundException("Biljett", command.TicketId.ToString());

        var ticketType = await ticketTypeRepository.GetByIdAsync(ticket.TicketTypeId, ct)
            ?? throw new ResourceNotFoundException("Biljetttyp", ticket.TicketTypeId.Value.ToString());

        ticket.Collect(performedById);
        await ticketRepository.SaveAsync(ct);

        var perks = ticketType.Perks
            .Select(perk => perk.Description)
            .ToList();

        return new CollectTicketResult(ticket.Id.Value, perks);
    }
}
