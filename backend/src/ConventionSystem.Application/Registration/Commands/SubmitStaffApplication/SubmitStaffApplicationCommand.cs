using MediatR;

namespace ConventionSystem.Application.Registration.Commands.SubmitStaffApplication;

public sealed record SubmitStaffApplicationCommand(
    Guid EditionId,
    string InterestDescription) : IRequest<Guid>;
