using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Event.Commands.CreateCoOrganiserInvitation;

public sealed record CreateCoOrganiserInvitationCommand(Guid EventId, string Email) : ICommand;
