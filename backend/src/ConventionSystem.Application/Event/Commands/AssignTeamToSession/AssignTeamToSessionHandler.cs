using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Event.Commands.AssignTeamToSession;

public sealed class AssignTeamToSessionHandler(
    ITeamEventRegistrationRepository registrationRepository,
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<AssignTeamToSessionCommand>
{
    protected override async Task ExecuteAsync(AssignTeamToSessionCommand command, CancellationToken ct)
    {
        var eventId = new EventId(command.EventId);
        var sessionId = new SessionId(command.SessionId);
        var registrationId = new TeamEventRegistrationId(command.TeamEventRegistrationId);

        var registration = await registrationRepository.GetByIdAsync(registrationId, ct)
            ?? throw new ResourceNotFoundException("Laganmälan", command.TeamEventRegistrationId.ToString());

        if (registration.Status != TeamRegistrationStatus.Confirmed)
            throw new DomainRuleViolationException(
                "Laget måste ha en bekräftad anmälan för att kunna tilldelas en session.");

        if (registration.EventId != eventId)
            throw new DomainRuleViolationException(
                "Laganmälan tillhör inte det angivna evenemanget.");

        var ev = await eventRepository.GetByIdWithSessionsAndTeamAssignmentsAsync(eventId, ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        var context = await EditionContextLoader.LoadAsync(editionRepository, conventionRepository, ev.EditionId, ct);
        ApplicationAuthorization.EnsureConventionAdmin(
            context.Convention, currentUser.PersonId,
            "Endast administratörer kan tilldela lag till sessioner.");

        ev.AssignTeamToSession(sessionId, command.TeamEventRegistrationId, currentUser.PersonId);
        await eventRepository.SaveAsync(ct);
    }
}
