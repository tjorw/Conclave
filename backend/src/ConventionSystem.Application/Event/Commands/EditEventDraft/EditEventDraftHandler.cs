using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Commands.EditEventDraft;

public sealed class EditEventDraftHandler(IEventRepository eventRepository, IEditionRepository editionRepository)
    : CommandHandler<EditEventDraftCommand>
{
    protected override async Task ExecuteAsync(EditEventDraftCommand command, CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        var edition = await editionRepository.GetByIdWithProgramTagDefinitionsAsync(ev.EditionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", ev.EditionId.Value.ToString());

        EnsureProgramTagsAreDefinedOnEdition(command.ProgramTags, edition.ProgramTagDefinitions);

        ev.EditTitle(command.Title);
        ev.EditDescription(command.Description);
        ev.SetProgramTags(command.ProgramTags);
        ev.SetRegistrationType(command.RegistrationType, command.DropInRules);
        ev.UpdateScheduleRequestText(command.ScheduleRequestText);
        ev.SetCoOrganiserCount(command.CoOrganiserCount);

        await eventRepository.SaveAsync(ct);
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
