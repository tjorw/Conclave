using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Tenancy.Abstractions;
using ConventionSystem.Application.Tenancy.Commands.RestoreTenant;
using ConventionSystem.Domain.Tenancy.Aggregates;
using ConventionSystem.Domain.Tenancy.Enums;
using ConventionSystem.Domain.Tenancy.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Tenancy.Commands;

public class RestoreTenantHandlerTests
{
    private readonly ITenantRepository _repository = Substitute.For<ITenantRepository>();
    private readonly RestoreTenantHandler _handler;

    public RestoreTenantHandlerTests()
    {
        _handler = new RestoreTenantHandler(_repository);
    }

    [Fact]
    public async Task Handle_ExistingSuspendedTenant_RestoresAndSaves()
    {
        var tenant = new Tenant(TenantId.New(), "mycon", "My Convention");
        tenant.Suspend();
        _repository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        await _handler.Handle(new RestoreTenantCommand(tenant.Id.Value), default);

        Assert.Equal(TenantStatus.Active, tenant.Status);
        await _repository.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownTenant_ThrowsResourceNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns((Tenant?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(new RestoreTenantCommand(Guid.NewGuid()), default));
    }
}