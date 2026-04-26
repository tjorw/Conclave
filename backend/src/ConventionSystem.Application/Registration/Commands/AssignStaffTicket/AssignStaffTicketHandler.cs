using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.AssignStaffTicket;

public sealed class AssignStaffTicketHandler(
    ITicketRepository ticketRepository,
    ITicketTypeRepository ticketTypeRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    IPersonRepository personRepository,
    ICurrentUser currentUser)
    : CommandHandler<AssignStaffTicketCommand>
{
    protected override async Task ExecuteAsync(AssignStaffTicketCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var personId = new PersonId(command.PersonId);
        var performedById = currentUser.PersonId;

        var edition = await editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new ResourceNotFoundException("Upplagan", command.EditionId.ToString());

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new ResourceNotFoundException("Konventionen", edition.ConventionId.Value.ToString());

        ApplicationAuthorization.EnsureStaffApplicationManager(
            convention, edition, performedById,
            "Utföraren har inte behörighet att hantera funktionärsbiljetter.");

        var person = await personRepository.GetByIdAsync(personId, ct)
            ?? throw new ResourceNotFoundException("Person", command.PersonId.ToString());

        if (person.ConventionId != edition.ConventionId)
            throw new ForbiddenException("Personen tillhör inte denna konvention.");

        TicketTypeId? ticketTypeId = null;
        TicketType? ticketType = null;
        if (command.TicketTypeId is not null)
        {
            ticketTypeId = new TicketTypeId(command.TicketTypeId.Value);
            ticketType = await ticketTypeRepository.GetByIdAsync(ticketTypeId.Value, ct)
                ?? throw new ResourceNotFoundException("Biljetttypen", command.TicketTypeId.Value.ToString());

            if (ticketType.EditionId != editionId)
                throw new DomainRuleViolationException("Funktionärsbiljetten tillhör inte denna upplaga.");

            if (ticketType.Type != TicketTypeCategory.Staff)
                throw new DomainRuleViolationException("Biljetttypen är inte en funktionärsbiljett.");
        }

        var currentTickets = await ticketRepository.ListActiveStaffTicketsAsync(editionId, [personId], ct);

        if (ticketTypeId is not null && currentTickets.Count == 1 && currentTickets[0].TicketTypeId == ticketTypeId.Value)
            return;

        foreach (var ticket in currentTickets)
            ticket.Revoke(performedById);

        if (ticketTypeId is not null)
        {
            var newTicket = Ticket.CreateOrganizerTicket(ticketTypeId.Value, personId, editionId, performedById);
            if (ticketType!.Price == 0)
                newTicket.ConfirmPayment();
            ticketRepository.Add(newTicket);
        }

        await ticketRepository.SaveAsync(ct);
    }
}
