using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Enums;
using ConventionSystem.Domain.Convention.Ids;
using MediatR;

namespace ConventionSystem.Application.Convention.Commands.CloseRegistration;

public sealed class CloseRegistrationHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : IRequestHandler<CloseRegistrationCommand>
{
    public async Task Handle(CloseRegistrationCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var performedById = currentUser.PersonId;

        var edition = await editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new InvalidOperationException($"Upplaga '{command.EditionId}' hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(performedById))
            throw new InvalidOperationException("Utföraren är inte administratör för denna konvention.");

        switch (command.RegistrationType)
        {
            case RegistrationType.Organiser:
                edition.CloseOrganiserRegistration(performedById);
                break;
            case RegistrationType.Staff:
                edition.CloseStaffRegistration(performedById);
                break;
            default:
                throw new InvalidOperationException($"Stängning stöds inte för registreringstypen: {command.RegistrationType}.");
        }

        await editionRepository.SaveAsync(ct);
    }
}
