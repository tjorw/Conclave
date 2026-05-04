using ConventionSystem.Domain.Event.Enums;

namespace ConventionSystem.Application.Event.Commands.EditEventDraft;

public sealed record EditEventDraftCommand(
    Guid EventId,
    string Title,
    string Description,
    IReadOnlyList<string> ProgramTags,
    RegistrationType RegistrationType,
    string? DropInRules,
    string? ScheduleRequestText,
    int CoOrganiserCount) : ICommand;
