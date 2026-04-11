using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Convention.Enums;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using MediatR;

namespace ConventionSystem.Application.Event.Commands.CreateEvent;

public sealed class CreateEventHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IPersonRepository personRepository)
    : IRequestHandler<CreateEventCommand, Guid>
{
    public async Task<Guid> Handle(CreateEventCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var categoryId = new CategoryId(command.CategoryId);
        var leadOrganiserId = new PersonId(command.LeadOrganiserId);
        var conventionId = new ConventionId(command.ConventionId);

        var edition = await editionRepository.GetByIdWithCategoriesAsync(editionId, ct)
            ?? throw new InvalidOperationException($"Upplagan '{command.EditionId}' hittades inte.");

        if (edition.Status != EditionStatus.Published)
            throw new InvalidOperationException("Upplagan måste vara publicerad innan evenemang kan skapas.");

        if (!edition.Categories.Any(c => c.Id == categoryId))
            throw new InvalidOperationException("Kategorin hittades inte på denna upplaga.");

        var person = await personRepository.GetByIdAsync(leadOrganiserId, ct)
            ?? throw new InvalidOperationException($"Person '{command.LeadOrganiserId}' hittades inte.");
        if (person.ConventionId != conventionId)
            throw new InvalidOperationException("Personen tillhör inte denna konvention.");

        var ev = new Domain.Event.Aggregates.Event(EventId.New(), editionId, categoryId, leadOrganiserId);
        await eventRepository.AddAndSaveAsync(ev, ct);
        return ev.Id.Value;
    }
}
