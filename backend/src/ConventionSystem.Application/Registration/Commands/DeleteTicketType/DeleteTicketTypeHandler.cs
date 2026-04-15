using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Exceptions;
using ConventionSystem.Domain.Registration.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Commands.DeleteTicketType;

public sealed class DeleteTicketTypeHandler(
    ITicketTypeRepository ticketTypeRepository,
    ITicketRepository ticketRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : IRequestHandler<DeleteTicketTypeCommand>
{
    public async Task Handle(DeleteTicketTypeCommand command, CancellationToken ct)
    {
        var performedById = currentUser.PersonId;

        var ticketType = await ticketTypeRepository.GetByIdAsync(new TicketTypeId(command.TicketTypeId), ct)
            ?? throw new TicketTypeNotFoundException();

        var edition = await editionRepository.GetByIdAsync(ticketType.EditionId, ct)
            ?? throw new InvalidOperationException("Upplagan hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(performedById))
            throw new UnauthorizedAccessException("Utföraren har inte behörighet att ta bort biljetttyper.");

        if (await ticketRepository.ExistsByTypeAsync(ticketType.Id, ct))
            throw new TicketTypeHasIssuedTicketsException();

        await ticketTypeRepository.DeleteAndSaveAsync(ticketType, ct);
    }
}
