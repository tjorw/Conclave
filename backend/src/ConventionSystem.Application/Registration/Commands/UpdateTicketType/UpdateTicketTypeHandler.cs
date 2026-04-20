using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Exceptions;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.UpdateTicketType;

public sealed class UpdateTicketTypeHandler(
    ITicketTypeRepository ticketTypeRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<UpdateTicketTypeCommand>
{
    protected override async Task ExecuteAsync(UpdateTicketTypeCommand command, CancellationToken ct)
    {
        var performedById = currentUser.PersonId;

        var ticketType = await ticketTypeRepository.GetByIdAsync(new TicketTypeId(command.TicketTypeId), ct)
            ?? throw new TicketTypeNotFoundException();

        var edition = await editionRepository.GetByIdAsync(ticketType.EditionId, ct)
            ?? throw new ResourceNotFoundException("Upplagan", ticketType.EditionId.Value.ToString());

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new ResourceNotFoundException("Konventionen", edition.ConventionId.Value.ToString());

        if (!convention.IsAdministrator(performedById))
            throw new ForbiddenException("Utföraren har inte behörighet att redigera biljetttyper.");

        if (command.ValidDays != null)
        {
            var period = edition.Period;
            if (command.ValidDays.Any(d => d < period.StartDate || d > period.EndDate))
                throw new TicketValidDaysOutsideEditionPeriodException();
        }

        ticketType.Update(command.Name, command.Price, command.Category, command.ValidDays, command.AllowedCategories);
        await ticketTypeRepository.SaveAsync(ct);
    }
}
