using MediatR;

namespace ConventionSystem.Application.Event.Commands.SubmitForReview;

public sealed record SubmitForReviewCommand(Guid EventId) : IRequest;
