using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Commands.ApproveCoOrganiserApplication;

public sealed class ApproveCoOrganiserApplicationHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    IPersonRepository personRepository,
    ICurrentUser currentUser)
    : CommandHandler<ApproveCoOrganiserApplicationCommand>
{
    protected override async Task ExecuteAsync(ApproveCoOrganiserApplicationCommand command, CancellationToken ct)
    {
        var performedById = currentUser.PersonId;

        var ev = await eventRepository.GetByIdWithCoOrganisersAndApplicationsAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        var context = await EditionContextLoader.LoadWithCategoriesAsync(
            editionRepository,
            conventionRepository,
            ev.EditionId,
            ct);
        ApplicationAuthorization.EnsureConventionAdmin(context.Convention, performedById, "Endast administratörer kan godkänna medarrangörsansökningar.");
        var applicationId = new CoOrganiserApplicationId(command.ApplicationId);
        var application = ev.CoOrganiserApplications.FirstOrDefault(a => a.Id == applicationId)
            ?? throw new ResourceNotFoundException("Medarrangörsansökan", command.ApplicationId.ToString());

        var person = await personRepository.FindByEmailInConventionAsync(context.Convention.Id, application.Email, ct);
        if (person is null)
        {
            var name = string.IsNullOrWhiteSpace(application.Name)
                ? application.Email
                : application.Name;
            person = context.Convention.CreatePerson(name, application.Email);
            await personRepository.AddAndSaveAsync(person, ct);
        }

        ev.ApproveCoOrganiserApplication(applicationId, person.Id, performedById);
        await eventRepository.SaveAsync(ct);
    }
}
