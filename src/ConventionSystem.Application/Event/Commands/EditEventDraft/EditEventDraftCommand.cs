using ConventionSystem.Domain.Event.Enums;
using MediatR;

namespace ConventionSystem.Application.Event.Commands.EditEventDraft;

public sealed record EditEventDraftCommand(
    Guid EventId,
    string Title,
    string Description,
    RegistrationType RegistrationType,
    string? DropInRules) : IRequest;
