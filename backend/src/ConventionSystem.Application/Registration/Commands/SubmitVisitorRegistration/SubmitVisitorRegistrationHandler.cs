using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.SubmitVisitorRegistration;

public sealed class SubmitVisitorRegistrationHandler(
    IVisitorRegistrationRepository visitorRegistrationRepository,
    ITicketRepository ticketRepository,
    ITicketTypeRepository ticketTypeRepository,
    IEditionRepository editionRepository,
    IPersonRepository personRepository,
    ICurrentUser currentUser)
    : ICommandHandler<SubmitVisitorRegistrationCommand, Guid>
{
    public async Task<Guid> Handle(SubmitVisitorRegistrationCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var personId = currentUser.PersonId;
        var ticketTypeId = new TicketTypeId(command.TicketTypeId);

        var edition = await editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", command.EditionId.ToString());

        if (!edition.VisitorRegistrationOpen)
            throw new DomainRuleViolationException("Besöksregistrering är inte öppen för denna upplaga.");

        var person = await personRepository.GetByIdAsync(personId, ct)
            ?? throw new ResourceNotFoundException("Person", personId.Value.ToString());
        if (person.ConventionId != edition.ConventionId)
            throw new ForbiddenException("Personen tillhör inte denna konvention.");
        if (!person.IsActive)
            throw new DomainRuleViolationException("Inaktiverade personer kan inte initiera nya registreringar.");

        var ticketType = await ticketTypeRepository.GetByIdAsync(ticketTypeId, ct)
            ?? throw new ResourceNotFoundException("Biljetttyp", command.TicketTypeId.ToString());
        if (ticketType.EditionId != editionId)
            throw new DomainRuleViolationException("Biljetttypen tillhör inte denna upplaga.");
        if (ticketType.Type != TicketTypeCategory.Visitor)
            throw new DomainRuleViolationException("Biljetttypen är inte avsedd för besökare.");
        if (await visitorRegistrationRepository.HasActiveRegistrationForTicketTypeAsync(personId, editionId, ticketTypeId, ct))
            throw new DomainRuleViolationException("Personen har redan en aktiv registrering för denna biljettyp i upplagan.");

        var ticketId = TicketId.New();
        var ticket = new Ticket(ticketId, ticketTypeId, personId, editionId);
        var registration = new VisitorRegistration(VisitorRegistrationId.New(), personId, editionId, ticketId);

        if (ticketType.Price == 0)
        {
            ticket.ConfirmPayment();
            registration.ConfirmPayment("AUTO-FREE");
        }

        ticketRepository.Add(ticket);
        await visitorRegistrationRepository.AddAndSaveAsync(registration, ct);

        return registration.Id.Value;
    }
}
