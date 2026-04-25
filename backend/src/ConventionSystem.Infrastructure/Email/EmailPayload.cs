namespace ConventionSystem.Infrastructure.Email;

internal sealed record EmailPayload(string To, string ToName, string Subject, string Body);
