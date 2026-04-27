using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Event.ValueObjects;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Event.Commands.ApproveVersion;

public sealed class ApproveVersionHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<ApproveVersionCommand>
{
    protected override async Task ExecuteAsync(ApproveVersionCommand command, CancellationToken ct)
    {
        var performedById = currentUser.PersonId;

        var ev = await eventRepository.GetByIdWithCoOrganisersAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        var context = await EditionContextLoader.LoadWithCategoriesAsync(
            editionRepository,
            conventionRepository,
            ev.EditionId,
            ct);

        ApplicationAuthorization.EnsureConventionAdmin(context.Convention, performedById, "Endast administratörer kan godkänna en version av evenemanget.");
        var organizerTicketAssignments = (command.OrganizerTicketAssignments ?? [])
            .Select(a => new OrganizerTicketAssignment(
                new PersonId(a.PersonId),
                a.TicketTypeId is null ? null : new TicketTypeId(a.TicketTypeId.Value)))
            .ToList();

        ev.Approve(performedById, organizerTicketAssignments);
        await eventRepository.SaveAsync(ct);
    }
}
