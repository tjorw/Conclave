
namespace ConventionSystem.Application.Tenancy.Commands.RestoreTenant;

public record RestoreTenantCommand(Guid TenantId) : ICommand;