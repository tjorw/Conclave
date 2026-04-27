using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Exceptions;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.CreateTicketType;

public sealed class CreateTicketTypeHandler(
    ITicketTypeRepository ticketTypeRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : ICommandHandler<CreateTicketTypeCommand, Guid>
{
    public async Task<Guid> Handle(CreateTicketTypeCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var performedById = currentUser.PersonId;

        var edition = await editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new ResourceNotFoundException("Upplagan", command.EditionId.ToString());

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new ResourceNotFoundException("Konventionen", edition.ConventionId.Value.ToString());

        if (!convention.IsAdministrator(performedById))
            throw new ForbiddenException("Utföraren har inte behörighet att skapa biljetttyper.");

        if (command.ValidDays != null)
        {
            var period = edition.Period;
            if (command.ValidDays.Any(d => d < period.StartDate || d > period.EndDate))
                throw new TicketValidDaysOutsideEditionPeriodException();
        }

        var ticketType = new TicketType(TicketTypeId.New(), editionId, command.Name, command.Price, command.Category,
            command.ValidDays, command.AllowedCategories, command.Description);
        await ticketTypeRepository.AddAndSaveAsync(ticketType, ct);
        return ticketType.Id.Value;
    }
}
