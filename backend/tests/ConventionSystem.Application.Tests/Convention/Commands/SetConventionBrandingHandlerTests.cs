using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.SetConventionBranding;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Commands;

public sealed class SetConventionBrandingHandlerTests
{
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly IConventionBrandingRepository _brandingRepo = Substitute.For<IConventionBrandingRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly SetConventionBrandingHandler _handler;

    public SetConventionBrandingHandlerTests()
    {
        _handler = new SetConventionBrandingHandler(_conventionRepo, _brandingRepo, _currentUser);
    }

    [Fact]
    public async Task Handle_NoExistingBranding_CreatesBranding()
    {
        var (convention, admin) = SetupConvention();
        _currentUser.PersonId.Returns(admin.Id);
        _brandingRepo.GetByConventionIdAsync(convention.Id, Arg.Any<CancellationToken>())
            .Returns((ConventionBranding?)null);

        await _handler.Handle(ValidCommand(convention.Id.Value), default);

        await _brandingRepo.Received(1).AddAsync(
            Arg.Is<ConventionBranding>(b =>
                b.ConventionId == convention.Id &&
                b.PrimaryColor == "#112233" &&
                b.AccentColor == "#aabbcc" &&
                b.FontFamily == "Inter"),
            Arg.Any<CancellationToken>());
        await _brandingRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingBranding_UpdatesBranding()
    {
        var (convention, admin) = SetupConvention();
        var branding = new ConventionBranding(convention.Id, "#000000", "#ffffff", null, null, "Roboto", null);
        _currentUser.PersonId.Returns(admin.Id);
        _brandingRepo.GetByConventionIdAsync(convention.Id, Arg.Any<CancellationToken>())
            .Returns(branding);

        await _handler.Handle(ValidCommand(convention.Id.Value), default);

        Assert.Equal("#112233", branding.PrimaryColor);
        Assert.Equal("#aabbcc", branding.AccentColor);
        Assert.Equal("/uploads/logo.svg", branding.LogoUrl);
        Assert.Equal("Inter", branding.FontFamily);
        await _brandingRepo.DidNotReceive().AddAsync(Arg.Any<ConventionBranding>(), Arg.Any<CancellationToken>());
        await _brandingRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidHex_Throws()
    {
        var (convention, admin) = SetupConvention();
        _currentUser.PersonId.Returns(admin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(ValidCommand(convention.Id.Value) with { PrimaryColor = "112233" }, default));
    }

    [Fact]
    public async Task Handle_DisallowedFont_Throws()
    {
        var (convention, admin) = SetupConvention();
        _currentUser.PersonId.Returns(admin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(ValidCommand(convention.Id.Value) with { FontFamily = "Papyrus" }, default));
    }

    [Fact]
    public async Task Handle_CustomCssTooLong_Throws()
    {
        var (convention, admin) = SetupConvention();
        _currentUser.PersonId.Returns(admin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(
                ValidCommand(convention.Id.Value) with { CustomCss = new string('x', ConventionBranding.CustomCssMaxLength + 1) },
                default));
    }

    [Fact]
    public async Task Handle_NonAdmin_ThrowsForbiddenException()
    {
        var (convention, _) = SetupConvention();
        var guest = convention.CreatePerson("Guest", "guest@example.com");
        _currentUser.PersonId.Returns(guest.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(ValidCommand(convention.Id.Value), default));
    }

    private (Domain.Convention.Aggregates.Convention convention, Domain.Convention.Entities.Person admin) SetupConvention()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        return (convention, admin);
    }

    private static SetConventionBrandingCommand ValidCommand(Guid conventionId)
        => new(
            conventionId,
            "#112233",
            "#aabbcc",
            "/uploads/logo.svg",
            "/uploads/favicon.png",
            "Inter",
            "--brand-primary: #112233;");
}
