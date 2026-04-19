using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Commands.DeactivatePromotionCode;

public sealed class DeactivatePromotionCodeHandler(
    IPromotionCodeRepository promotionCodeRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : IRequestHandler<DeactivatePromotionCodeCommand>
{
    public async Task Handle(DeactivatePromotionCodeCommand command, CancellationToken ct)
    {
        var promotionCodeId = new PromotionCodeId(command.PromotionCodeId);
        var performedById = currentUser.PersonId;

        var promotionCode = await promotionCodeRepository.GetByIdAsync(promotionCodeId, ct)
            ?? throw new ResourceNotFoundException("Kampanjkod", command.PromotionCodeId.ToString());

        var edition = await editionRepository.GetByIdAsync(promotionCode.EditionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", promotionCode.EditionId.Value.ToString());

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new ResourceNotFoundException("Konventionen", edition.ConventionId.Value.ToString());

        if (!convention.IsAdministrator(performedById))
            throw new ForbiddenException("Utföraren har inte behörighet att deaktivera kampanjkoder.");

        promotionCode.Deactivate(performedById);
        await promotionCodeRepository.SaveAsync(ct);
    }
}
