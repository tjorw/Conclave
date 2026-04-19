using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Tenancy.Abstractions;
using ConventionSystem.Application.Tenancy.Commands.SuspendTenant;
using ConventionSystem.Domain.Tenancy.Aggregates;
using ConventionSystem.Domain.Tenancy.Enums;
using ConventionSystem.Domain.Tenancy.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Tenancy.Commands;

public class SuspendTenantHandlerTests
{
    private readonly ITenantRepository _repository = Substitute.For<ITenantRepository>();
    private readonly SuspendTenantHandler _handler;

    public SuspendTenantHandlerTests()
    {
        _handler = new SuspendTenantHandler(_repository);
    }

    [Fact]
    public async Task Handle_ExistingTenant_SuspendsAndSaves()
    {
        var tenant = new Tenant(TenantId.New(), "mycon", "My Convention");
        _repository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        await _handler.Handle(new SuspendTenantCommand(tenant.Id.Value), default);

        Assert.Equal(TenantStatus.Suspended, tenant.Status);
        await _repository.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownTenant_ThrowsResourceNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns((Tenant?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(new SuspendTenantCommand(Guid.NewGuid()), default));
    }
}