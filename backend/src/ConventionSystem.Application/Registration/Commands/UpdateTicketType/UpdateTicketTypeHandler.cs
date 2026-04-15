using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Exceptions;
using ConventionSystem.Domain.Registration.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Commands.UpdateTicketType;

public sealed class UpdateTicketTypeHandler(
    ITicketTypeRepository ticketTypeRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : IRequestHandler<UpdateTicketTypeCommand>
{
    public async Task Handle(UpdateTicketTypeCommand command, CancellationToken ct)
    {
        var performedById = currentUser.PersonId;

        var ticketType = await ticketTypeRepository.GetByIdAsync(new TicketTypeId(command.TicketTypeId), ct)
            ?? throw new TicketTypeNotFoundException();

        var edition = await editionRepository.GetByIdAsync(ticketType.EditionId, ct)
            ?? throw new InvalidOperationException("Upplagan hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(performedById))
            throw new UnauthorizedAccessException("Utföraren har inte behörighet att redigera biljetttyper.");

        ticketType.Update(command.Name, command.Price, command.IsSellable, command.IsPubliclyVisible);
        await ticketTypeRepository.SaveAsync(ct);
    }
}
