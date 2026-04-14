using MediatR;

namespace ConventionSystem.Application.Registration.Commands.AddStaffMember;

public sealed record AddStaffMemberCommand(
    Guid EditionId,
    string Name,
    string Email,
    string? Phone,
    string? Note) : IRequest<Guid>;
