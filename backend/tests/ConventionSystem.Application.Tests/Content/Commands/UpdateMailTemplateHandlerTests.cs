using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Application.Content.Commands.UpdateMailTemplate;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Content.Aggregates;
using ConventionSystem.Domain.Content.Enums;
using ConventionSystem.Domain.Content.Ids;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Content.Commands;

public sealed class UpdateMailTemplateHandlerTests
{
    private readonly IMailTemplateRepository _mailTemplateRepo = Substitute.For<IMailTemplateRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly Common.ICurrentUser _currentUser = Substitute.For<Common.ICurrentUser>();
    private readonly UpdateMailTemplateHandler _handler;

    public UpdateMailTemplateHandlerTests()
    {
        _handler = new UpdateMailTemplateHandler(_mailTemplateRepo, _conventionRepo, _currentUser);
    }

    private Domain.Convention.Aggregates.Convention Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        _currentUser.PersonId.Returns(admin.Id);
        _conventionRepo.GetSingleAsync(Arg.Any<CancellationToken>()).Returns(convention);
        return convention;
    }

    [Fact]
    public async Task Handle_NewTemplate_CreatesAndSavesTemplate()
    {
        var convention = Setup();
        _mailTemplateRepo.GetByTypeAsync(convention.Id, MailTemplateType.VisitorRegistrationConfirmed, Arg.Any<CancellationToken>())
            .Returns((MailTemplate?)null);

        await _handler.Handle(new UpdateMailTemplateCommand(
            convention.Id.Value,
            "VisitorRegistrationConfirmed",
            "Nytt ämne",
            "Ny brödtext"), default);

        await _mailTemplateRepo.Received(1).AddAsync(
            Arg.Is<MailTemplate>(t =>
                t.Subject == "Nytt ämne" &&
                t.BodyMarkdown == "Ny brödtext" &&
                t.IsCustomized),
            Arg.Any<CancellationToken>());
        await _mailTemplateRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingTemplate_UpdatesTemplate()
    {
        var convention = Setup();
        var existing = new MailTemplate(
            MailTemplateId.New(),
            convention.Id,
            MailTemplateType.VisitorRegistrationConfirmed,
            "Gammalt ämne",
            "Gammal body");
        _mailTemplateRepo.GetByTypeAsync(convention.Id, MailTemplateType.VisitorRegistrationConfirmed, Arg.Any<CancellationToken>())
            .Returns(existing);

        await _handler.Handle(new UpdateMailTemplateCommand(
            convention.Id.Value,
            "VisitorRegistrationConfirmed",
            "Uppdaterat ämne",
            "Uppdaterad body"), default);

        Assert.Equal("Uppdaterat ämne", existing.Subject);
        Assert.Equal("Uppdaterad body", existing.BodyMarkdown);
        await _mailTemplateRepo.DidNotReceive().AddAsync(Arg.Any<MailTemplate>(), Arg.Any<CancellationToken>());
        await _mailTemplateRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownTemplateType_Throws()
    {
        Setup();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.Handle(new UpdateMailTemplateCommand(
                Guid.NewGuid(),
                "OkändTyp",
                "Ämne",
                "Body"), default));
    }

    [Fact]
    public async Task Handle_NonAdmin_Throws()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var outsider = convention.RegisterPerson("Utomstående", "other@example.com");
        _currentUser.PersonId.Returns(outsider.Id);
        _conventionRepo.GetSingleAsync(Arg.Any<CancellationToken>()).Returns(convention);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new UpdateMailTemplateCommand(
                convention.Id.Value,
                "VisitorRegistrationConfirmed",
                "Ämne",
                "Body"), default));
    }
}
