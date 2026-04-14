using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Commands.SubmitStaffApplication;

public sealed class SubmitStaffApplicationHandler(
    IStaffApplicationRepository staffApplicationRepository,
    IEditionRepository editionRepository,
    IPersonRepository personRepository)
    : IRequestHandler<SubmitStaffApplicationCommand, Guid>
{
    public async Task<Guid> Handle(SubmitStaffApplicationCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var personId = new PersonId(command.PersonId);

        var edition = await editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new InvalidOperationException($"Upplagan '{command.EditionId}' hittades inte.");

        if (!edition.StaffRegistrationOpen)
            throw new InvalidOperationException("Staffregistrering är inte öppen för denna upplaga.");

        var person = await personRepository.GetByIdAsync(personId, ct)
            ?? throw new InvalidOperationException($"Person '{command.PersonId}' hittades inte.");
        if (person.ConventionId != edition.ConventionId)
            throw new InvalidOperationException("Personen tillhör inte denna konvention.");
        if (!person.IsActive)
            throw new InvalidOperationException("Inaktiverade personer kan inte initiera nya registreringar.");

        if (await staffApplicationRepository.HasActiveApplicationAsync(personId, editionId, ct))
            throw new InvalidOperationException("Personen har redan en aktiv staffansökan för denna upplaga.");

        var application = new StaffApplication(StaffApplicationId.New(), personId, editionId, command.InterestDescription);
        await staffApplicationRepository.AddAndSaveAsync(application, ct);
        return application.Id.Value;
    }
}
