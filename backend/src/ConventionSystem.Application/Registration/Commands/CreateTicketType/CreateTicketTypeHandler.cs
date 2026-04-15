using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Commands.CreateTicketType;

public sealed class CreateTicketTypeHandler(
    ITicketTypeRepository ticketTypeRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : IRequestHandler<CreateTicketTypeCommand, Guid>
{
    public async Task<Guid> Handle(CreateTicketTypeCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var performedById = currentUser.PersonId;

        var edition = await editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new InvalidOperationException($"Upplagan '{command.EditionId}' hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(performedById))
            throw new InvalidOperationException("Utföraren har inte behörighet att skapa biljetttyper.");

        var ticketType = new TicketType(TicketTypeId.New(), editionId, command.Name, command.Price, command.Category,
            command.IsSellable, command.IsPubliclyVisible);
        await ticketTypeRepository.AddAndSaveAsync(ticketType, ct);
        return ticketType.Id.Value;
    }
}
