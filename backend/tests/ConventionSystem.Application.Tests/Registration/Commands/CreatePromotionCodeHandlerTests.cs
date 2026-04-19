using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.CreatePromotionCode;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Exceptions;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class CreatePromotionCodeHandlerTests
{
    private readonly IPromotionCodeRepository _promotionRepo = Substitute.For<IPromotionCodeRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly CreatePromotionCodeHandler _handler;

    public CreatePromotionCodeHandlerTests()
    {
        _handler = new CreatePromotionCodeHandler(_promotionRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Aggregates.Edition edition) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staff.Id, evt.Id);

        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (convention, admin, edition);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsId()
    {
        var (_, admin, edition) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        var id = await _handler.Handle(new CreatePromotionCodeCommand(
            edition.Id.Value,
            "SAVE10",
            "10% rabatt",
            PromotionDiscountType.Percentage,
            10,
            null,
            null,
            null,
            null), default);

        Assert.NotEqual(Guid.Empty, id);
        await _promotionRepo.Received(1).AddAndSaveAsync(Arg.Any<Domain.Registration.Aggregates.PromotionCode>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateCode_Throws()
    {
        var (_, admin, edition) = Setup();
        _currentUser.PersonId.Returns(admin.Id);
        _promotionRepo.ExistsByEditionAndCodeAsync(edition.Id, "SAVE10", Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<PromotionCodeAlreadyExistsException>(() => _handler.Handle(new CreatePromotionCodeCommand(
            edition.Id.Value,
            "save10",
            "10% rabatt",
            PromotionDiscountType.Percentage,
            10,
            null,
            null,
            null,
            null), default));
    }
}
