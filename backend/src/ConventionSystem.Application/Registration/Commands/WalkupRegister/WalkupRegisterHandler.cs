using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.WalkupRegister;

public sealed class WalkupRegisterHandler(
    IEditionRepository editionRepo,
    IConventionRepository conventionRepo,
    ICurrentUser currentUser,
    IPersonRepository personRepo,
    ITicketTypeRepository ticketTypeRepo,
    ITicketRepository ticketRepo,
    IVisitorRegistrationRepository visitorRegistrationRepo)
    : ICommandHandler<WalkupRegisterCommand, Guid>
{
    public async Task<Guid> Handle(WalkupRegisterCommand command, CancellationToken ct)
    {
        var ctx = await EditionContextLoader.LoadWithReceptionStaffAsync(
            editionRepo, conventionRepo, new EditionId(command.EditionId), ct);

        ApplicationAuthorization.EnsureReceptionAccess(
            ctx.Convention, ctx.Edition, currentUser.PersonId,
            "Åtkomst kräver receptionsroll eller administratör.");

        var personId = new PersonId(command.PersonId);
        var person = await personRepo.GetByIdAsync(personId, ct)
            ?? throw new ResourceNotFoundException("Person", command.PersonId.ToString());
        if (person.ConventionId != ctx.Convention.Id)
            throw new ForbiddenException("Personen tillhör inte denna konvention.");

        var ticketTypeId = new TicketTypeId(command.TicketTypeId);
        var ticketType = await ticketTypeRepo.GetByIdAsync(ticketTypeId, ct)
            ?? throw new ResourceNotFoundException("Biljetttyp", command.TicketTypeId.ToString());
        if (ticketType.EditionId != ctx.Edition.Id)
            throw new DomainRuleViolationException("Biljetttypen tillhör inte denna upplaga.");
        if (ticketType.Type != TicketTypeCategory.Visitor)
            throw new DomainRuleViolationException("Walk-up-registrering kräver en besökarbiljetttyp.");

        if (await visitorRegistrationRepo.HasActiveRegistrationForTicketTypeAsync(personId, ctx.Edition.Id, ticketTypeId, ct))
            throw new DomainRuleViolationException("Personen har redan en aktiv registrering för denna biljetttyp.");

        var ticket = new Ticket(TicketId.New(), ticketTypeId, personId, ctx.Edition.Id);
        var registration = new VisitorRegistration(
            VisitorRegistrationId.New(), personId, ctx.Edition.Id, ticket.Id);

        ticket.ConfirmPayment();
        registration.ConfirmPayment("WALKUP-CASH");

        ticketRepo.Add(ticket);
        await visitorRegistrationRepo.AddAndSaveAsync(registration, ct);

        return ticket.Id.Value;
    }
}
