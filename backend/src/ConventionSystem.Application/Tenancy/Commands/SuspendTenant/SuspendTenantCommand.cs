using MediatR;

namespace ConventionSystem.Application.Tenancy.Commands.SuspendTenant;

public record SuspendTenantCommand(Guid TenantId) : IRequest;