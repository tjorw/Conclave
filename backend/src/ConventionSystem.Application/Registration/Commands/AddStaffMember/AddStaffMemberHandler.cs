using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Commands.AddStaffMember;

public sealed class AddStaffMemberHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    IPersonRepository personRepository,
    IStaffApplicationRepository staffApplicationRepository,
    ICurrentUser currentUser)
    : IRequestHandler<AddStaffMemberCommand, Guid>
{
    public async Task<Guid> Handle(AddStaffMemberCommand command, CancellationToken ct)
    {
        var editionId      = new EditionId(command.EditionId);
        var performedById  = currentUser.PersonId;

        var edition = await editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new InvalidOperationException($"Upplagan '{command.EditionId}' hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(performedById) && !edition.IsStaffCoordinator(performedById))
            throw new InvalidOperationException("Utföraren har inte behörighet att lägga till funktionärer.");

        // Find or create person
        var person = await personRepository.FindByEmailInConventionAsync(edition.ConventionId, command.Email, ct);
        if (person is null)
        {
            if (string.IsNullOrWhiteSpace(command.Name))
                throw new ArgumentException("Namn krävs när personen inte finns i registret.", nameof(command.Name));

            person = convention.CreatePerson(command.Name, command.Email, command.Phone);
            await personRepository.AddAndSaveAsync(person, ct);
        }

        if (await staffApplicationRepository.HasActiveApplicationAsync(person.Id, editionId, ct))
            throw new InvalidOperationException("Personen har redan en aktiv staffansökan för denna upplaga.");

        var description = string.IsNullOrWhiteSpace(command.Note) ? "Tillagd av administratör" : command.Note;
        var application  = new StaffApplication(StaffApplicationId.New(), person.Id, editionId, description);
        application.Accept(performedById);

        await staffApplicationRepository.AddAndSaveAsync(application, ct);
        return application.Id.Value;
    }
}
