using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Event.Commands.RedeemCoOrganiserInvitation;

public sealed record RedeemCoOrganiserInvitationCommand(string Code) : ICommand;
