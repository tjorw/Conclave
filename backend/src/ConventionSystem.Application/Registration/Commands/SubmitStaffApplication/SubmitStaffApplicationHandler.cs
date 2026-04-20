using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.SubmitStaffApplication;

public sealed class SubmitStaffApplicationHandler(
    IStaffApplicationRepository staffApplicationRepository,
    IEditionRepository editionRepository,
    IPersonRepository personRepository,
    ICurrentUser currentUser)
    : ICommandHandler<SubmitStaffApplicationCommand, Guid>
{
    public async Task<Guid> Handle(SubmitStaffApplicationCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var personId = currentUser.PersonId;

        var edition = await editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", command.EditionId.ToString());

        if (!currentUser.IsAdmin && !edition.StaffRegistrationOpen)
            throw new DomainRuleViolationException("Staffregistrering är inte öppen för denna upplaga.");

        var person = await personRepository.GetByIdAsync(personId, ct)
            ?? throw new ResourceNotFoundException("Person", personId.Value.ToString());

        if (person.ConventionId != edition.ConventionId)
            throw new ForbiddenException("Personen tillhör inte denna konvention.");

        if (!person.IsActive)
            throw new DomainRuleViolationException("Inaktiverade personer kan inte initiera nya registreringar.");

        if (await staffApplicationRepository.HasActiveApplicationAsync(personId, editionId, ct))
            throw new DomainRuleViolationException("Personen har redan en aktiv staffansökan för denna upplaga.");

        var application = new StaffApplication(StaffApplicationId.New(), personId, editionId, command.InterestDescription);
        await staffApplicationRepository.AddAndSaveAsync(application, ct);
        return application.Id.Value;
    }
}
