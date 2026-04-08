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

        var registrant = convention.RegisterPerson(command.RegistrantName, command.RegistrantEmail);
        convention.AddAdministrator(registrant.Id, registrant.Id);

        await repository.CreateWithAdminAsync(convention, registrant, ct);
        return convention.Id.Value;
    }
}
