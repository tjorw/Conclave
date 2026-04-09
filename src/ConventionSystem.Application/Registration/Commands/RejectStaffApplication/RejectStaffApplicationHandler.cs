using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Commands.RejectStaffApplication;

public sealed class RejectStaffApplicationHandler(
    IStaffApplicationRepository staffApplicationRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository)
    : IRequestHandler<RejectStaffApplicationCommand>
{
    public async Task Handle(RejectStaffApplicationCommand command, CancellationToken ct)
    {
        var applicationId = new StaffApplicationId(command.StaffApplicationId);
        var performedById = new PersonId(command.PerformedById);

        var application = await staffApplicationRepository.GetByIdAsync(applicationId, ct)
            ?? throw new InvalidOperationException($"Staffansökan '{command.StaffApplicationId}' hittades inte.");

        var edition = await editionRepository.GetByIdAsync(application.EditionId, ct)
            ?? throw new InvalidOperationException("Upplagan hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(performedById) && !edition.IsStaffCoordinator(performedById))
            throw new InvalidOperationException("Utföraren har inte behörighet att avslå staffansökningar.");

        application.Reject(performedById);
        await staffApplicationRepository.SaveAsync(ct);
    }
}
