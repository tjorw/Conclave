using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Commands.AcceptStaffApplication;

public sealed class AcceptStaffApplicationHandler(
    IStaffApplicationRepository staffApplicationRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : IRequestHandler<AcceptStaffApplicationCommand>
{
    public async Task Handle(AcceptStaffApplicationCommand command, CancellationToken ct)
    {
        var applicationId = new StaffApplicationId(command.StaffApplicationId);
        var performedById = currentUser.PersonId;

        var application = await staffApplicationRepository.GetByIdAsync(applicationId, ct)
            ?? throw new ResourceNotFoundException("Staffansökan", command.StaffApplicationId.ToString());

        var edition = await editionRepository.GetByIdAsync(application.EditionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", application.EditionId.Value.ToString());

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new ResourceNotFoundException("Konvention", edition.ConventionId.Value.ToString());

        if (!convention.IsAdministrator(performedById) && !edition.IsStaffCoordinator(performedById))
            throw new ForbiddenException("Utföraren har inte behörighet att acceptera staffansökningar.");

        application.Accept(performedById);
        await staffApplicationRepository.SaveAsync(ct);
    }
}
