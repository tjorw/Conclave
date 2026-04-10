namespace ConventionSystem.Infrastructure.Identity;

public sealed class ConventionUserLink
{
    private ConventionUserLink() { }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = default!;
    public ApplicationUser User { get; private set; } = default!;
    public Guid ConventionId { get; private set; }
    public Guid PersonId { get; private set; }

    public static ConventionUserLink Create(string userId, Guid conventionId, Guid personId)
        => new()
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            ConventionId = conventionId,
            PersonId = personId
        };
}
