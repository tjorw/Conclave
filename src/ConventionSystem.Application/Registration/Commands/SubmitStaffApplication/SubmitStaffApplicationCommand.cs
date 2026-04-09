using MediatR;

namespace ConventionSystem.Application.Registration.Commands.SubmitStaffApplication;

public sealed record SubmitStaffApplicationCommand(
    Guid EditionId,
    Guid PersonId,
    string InterestDescription) : IRequest<Guid>;
