using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Event.Commands.CancelCoOrganiserInvitation;

public sealed record CancelCoOrganiserInvitationCommand(Guid EventId, Guid InvitationId) : ICommand;
