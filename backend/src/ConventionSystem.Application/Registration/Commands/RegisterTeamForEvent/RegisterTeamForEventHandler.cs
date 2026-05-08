using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Exceptions;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.RegisterTeamForEvent;

public sealed class RegisterTeamForEventHandler(
    IEventRepository eventRepository,
    ITeamRepository teamRepository,
    ITeamEventRegistrationRepository registrationRepository,
    ICurrentUser currentUser)
    : IRequestHandler<RegisterTeamForEventCommand, Guid>
{
    public async Task<Guid> Handle(RegisterTeamForEventCommand command, CancellationToken ct)
    {
        var eventId = new EventId(command.EventId);
        var editionId = new EditionId(command.EditionId);

        var ev = await eventRepository.GetByIdAsync(eventId, ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        if (ev.RegistrationMode != RegistrationMode.Team)
            throw new EventNotAcceptingTeamRegistrationsException();

        if (ev.Status != Domain.Event.Enums.EventStatus.Published)
            throw new ForbiddenException("Evenemanget är inte publicerat och tar inte emot anmälningar.");

        var hasActive = await registrationRepository.HasActiveRegistrationAsync(currentUser.PersonId, eventId, ct);
        if (hasActive)
            throw new ActiveTeamRegistrationExistsException();

        var team = new Team(TeamId.New(), editionId, currentUser.PersonId, command.TeamName);
        await teamRepository.AddAndSaveAsync(team, ct);

        var registration = new TeamEventRegistration(
            TeamEventRegistrationId.New(),
            team.Id,
            eventId,
            editionId);
        await registrationRepository.AddAndSaveAsync(registration, ct);

        return registration.Id.Value;
    }
}
