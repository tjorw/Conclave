using ConventionSystem.Application.Tenancy.Abstractions;
using ConventionSystem.Application.Tenancy.Commands.CreateTenant;
using ConventionSystem.Domain.Tenancy.Aggregates;
using ConventionSystem.Domain.Tenancy.Enums;
using ConventionSystem.Domain.Tenancy.Events;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Tenancy.Commands;

public class CreateTenantHandlerTests
{
    private readonly ITenantRepository _repository = Substitute.For<ITenantRepository>();
    private readonly CreateTenantHandler _handler;

    public CreateTenantHandlerTests()
    {
        _handler = new CreateTenantHandler(_repository);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsTenantId()
    {
        _repository.SubdomainExistsAsync("mycon", Arg.Any<CancellationToken>()).Returns(false);

        var id = await _handler.Handle(new CreateTenantCommand("MyCon", "My Convention"), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsAndSaves()
    {
        _repository.SubdomainExistsAsync("mycon", Arg.Any<CancellationToken>()).Returns(false);

        await _handler.Handle(new CreateTenantCommand("MyCon", "My Convention"), default);

        await _repository.Received(1).AddAsync(
            Arg.Is<Tenant>(t =>
                t.Subdomain == "mycon"
                && t.DisplayName == "My Convention"
                && t.Status == TenantStatus.Suspended
                && t.DomainEvents.OfType<TenantCreated>().Any()),
            Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateSubdomain_Throws()
    {
        _repository.SubdomainExistsAsync("mycon", Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CreateTenantCommand("MyCon", "My Convention"), default));
    }
}