using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using MediatR;

namespace ConventionSystem.Application.Convention.Commands.CreateConvention;

public sealed class CreateConventionHandler(IConventionRepository repository)
    : IRequestHandler<CreateConventionCommand, Guid>
{
    public async Task<Guid> Handle(CreateConventionCommand command, CancellationToken ct)
    {
        if (await repository.SlugExistsAsync(command.Slug, ct))
            throw new InvalidOperationException($"Slug '{command.Slug}' är redan använd.");

        var convention = new Domain.Convention.Aggregates.Convention(
            ConventionId.New(),
            command.Name,
            command.Slug);

        await repository.AddAsync(convention, ct);
        return convention.Id.Value;
    }
}
