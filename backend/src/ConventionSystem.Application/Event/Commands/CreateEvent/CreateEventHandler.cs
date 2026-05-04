using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Convention.Enums;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Commands.CreateEvent;

public sealed class CreateEventHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IPersonRepository personRepository,
    ICurrentUser currentUser)
    : ICommandHandler<CreateEventCommand, Guid>
{
    public async Task<Guid> Handle(CreateEventCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var categoryId = new CategoryId(command.CategoryId);
        var leadOrganiserId = new PersonId(command.LeadOrganiserId);

        if (!currentUser.IsAdmin && leadOrganiserId != currentUser.PersonId)
            throw new UnauthorizedAccessException("Utföraren kan bara skapa evenemang åt sig själv.");

        var edition = await editionRepository.GetByIdWithCategoriesAsync(editionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", command.EditionId.ToString());

        if (edition.Status != EditionStatus.Published)
            throw new InvalidOperationException("Upplagan måste vara publicerad innan evenemang kan skapas.");

        if (!edition.Categories.Any(c => c.Id == categoryId))
            throw new InvalidOperationException("Kategorin hittades inte på denna upplaga.");

        var person = await personRepository.GetByIdAsync(leadOrganiserId, ct)
            ?? throw new ResourceNotFoundException("Person", leadOrganiserId.Value.ToString());
        if (person.ConventionId != edition.ConventionId)
            throw new InvalidOperationException("Personen tillhör inte denna konvention.");

        EnsureProgramTagsAreDefinedOnEdition(command.ProgramTags, edition.ProgramTagDefinitions);

        var ev = new Domain.Event.Aggregates.Event(EventId.New(), editionId, categoryId, leadOrganiserId);
        ev.SetProgramTags(command.ProgramTags);
        await eventRepository.AddAndSaveAsync(ev, ct);
        return ev.Id.Value;
    }

    private static void EnsureProgramTagsAreDefinedOnEdition(
        IReadOnlyList<string> programTags,
        IReadOnlyList<ProgramTagDefinition> editionProgramTags)
    {
        var allowedTagNames = editionProgramTags
            .Select(t => t.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unknownTags = programTags
            .Where(t => !allowedTagNames.Contains(t.Trim()))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (unknownTags.Count > 0)
            throw new InvalidOperationException($"Följande taggar finns inte definierade på upplagan: {string.Join(", ", unknownTags)}.");
    }
}
