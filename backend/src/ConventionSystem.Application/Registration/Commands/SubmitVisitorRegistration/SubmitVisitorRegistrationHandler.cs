using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Commands.SubmitVisitorRegistration;

public sealed class SubmitVisitorRegistrationHandler(
    IVisitorRegistrationRepository visitorRegistrationRepository,
    ITicketRepository ticketRepository,
    ITicketTypeRepository ticketTypeRepository,
    IEditionRepository editionRepository,
    IPersonRepository personRepository)
    : IRequestHandler<SubmitVisitorRegistrationCommand, Guid>
{
    public async Task<Guid> Handle(SubmitVisitorRegistrationCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var personId = new PersonId(command.PersonId);
        var ticketTypeId = new TicketTypeId(command.TicketTypeId);

        var edition = await editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new InvalidOperationException($"Upplagan '{command.EditionId}' hittades inte.");

        if (!edition.VisitorRegistrationOpen)
            throw new InvalidOperationException("Besöksregistrering är inte öppen för denna upplaga.");

        var person = await personRepository.GetByIdAsync(personId, ct)
            ?? throw new InvalidOperationException($"Person '{command.PersonId}' hittades inte.");
        if (person.ConventionId != edition.ConventionId)
            throw new InvalidOperationException("Personen tillhör inte denna konvention.");
        if (!person.IsActive)
            throw new InvalidOperationException("Inaktiverade personer kan inte initiera nya registreringar.");

        var ticketType = await ticketTypeRepository.GetByIdAsync(ticketTypeId, ct)
            ?? throw new InvalidOperationException($"Biljetttypen '{command.TicketTypeId}' hittades inte.");
        if (ticketType.EditionId != editionId)
            throw new InvalidOperationException("Biljetttypen tillhör inte denna upplaga.");
        if (ticketType.Type != TicketTypeCategory.Visitor)
            throw new InvalidOperationException("Biljetttypen är inte avsedd för besökare.");

        if (await visitorRegistrationRepository.HasActiveRegistrationAsync(personId, editionId, ct))
            throw new InvalidOperationException("Personen har redan en aktiv besöksregistrering för denna upplaga.");

        var ticketId = TicketId.New();
        var ticket = new Ticket(ticketId, ticketTypeId, personId, editionId);
        await ticketRepository.AddAsync(ticket, ct);

        var registration = new VisitorRegistration(VisitorRegistrationId.New(), personId, editionId, ticketId);
        await visitorRegistrationRepository.AddAndSaveAsync(registration, ct);

        return registration.Id.Value;
    }
}
