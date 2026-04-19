using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.DeactivatePromotionCode;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class DeactivatePromotionCodeHandlerTests
{
    private readonly IPromotionCodeRepository _promotionRepo = Substitute.For<IPromotionCodeRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly DeactivatePromotionCodeHandler _handler;

    public DeactivatePromotionCodeHandlerTests()
    {
        _handler = new DeactivatePromotionCodeHandler(_promotionRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    [Fact]
    public async Task Handle_ValidCommand_DeactivatesCode()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test", "test");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var edition = convention.CreateEdition("K", new DatePeriod(new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 2)), staff.Id, evt.Id);

        var promo = new PromotionCode(PromotionCodeId.New(), edition.Id, "SAVE", "rabatt", PromotionDiscountType.Fixed, 1000, null, null, null, null, admin.Id);

        _promotionRepo.GetByIdAsync(promo.Id, Arg.Any<CancellationToken>()).Returns(promo);
        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new DeactivatePromotionCodeCommand(promo.Id.Value), default);

        Assert.False(promo.IsActive);
        await _promotionRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }
}
