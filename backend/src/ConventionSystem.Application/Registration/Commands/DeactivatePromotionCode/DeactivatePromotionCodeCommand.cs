
namespace ConventionSystem.Application.Registration.Commands.DeactivatePromotionCode;

public sealed record DeactivatePromotionCodeCommand(Guid PromotionCodeId) : ICommand;
