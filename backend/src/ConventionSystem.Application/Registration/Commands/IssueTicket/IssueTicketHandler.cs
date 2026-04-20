using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.IssueTicket;

public sealed class IssueTicketHandler(
    ITicketRepository ticketRepository,
    ITicketTypeRepository ticketTypeRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    IPersonRepository personRepository,
    ICurrentUser currentUser)
    : ICommandHandler<IssueTicketCommand, Guid>
{
    public async Task<Guid> Handle(IssueTicketCommand command, CancellationToken ct)
    {
        var personId = new PersonId(command.PersonId);
        var editionId = new EditionId(command.EditionId);
        var ticketTypeId = new TicketTypeId(command.TicketTypeId);
        var performedById = currentUser.PersonId;

        var edition = await editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new ResourceNotFoundException("Upplagan", command.EditionId.ToString());

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new ResourceNotFoundException("Konventionen", edition.ConventionId.Value.ToString());

        if (!convention.IsAdministrator(performedById))
            throw new ForbiddenException("Utföraren har inte behörighet att utfärda biljetter.");

        var person = await personRepository.GetByIdAsync(personId, ct)
            ?? throw new ResourceNotFoundException("Person", command.PersonId.ToString());
        if (person.ConventionId != edition.ConventionId)
            throw new ForbiddenException("Personen tillhör inte denna konvention.");

        var ticketType = await ticketTypeRepository.GetByIdAsync(ticketTypeId, ct)
            ?? throw new ResourceNotFoundException("Biljetttypen", command.TicketTypeId.ToString());
        if (ticketType.EditionId != editionId)
            throw new ForbiddenException("Biljetttypen tillhör inte denna upplaga.");

        var ticket = new Ticket(TicketId.New(), ticketTypeId, personId, editionId, performedById);
        await ticketRepository.AddAndSaveAsync(ticket, ct);
        return ticket.Id.Value;
    }
}
