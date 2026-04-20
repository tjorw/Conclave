using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Commands.CreateConvention;

public sealed class CreateConventionHandler(IConventionRepository repository)
    : ICommandHandler<CreateConventionCommand, Guid>
{
    public async Task<Guid> Handle(CreateConventionCommand command, CancellationToken ct)
    {
        if (await repository.SlugExistsAsync(command.Slug, ct))
            throw new InvalidOperationException($"Slug '{command.Slug}' är redan använd.");

        var conventionId = command.ConventionId.HasValue
            ? new ConventionId(command.ConventionId.Value)
            : ConventionId.New();

        var convention = new Domain.Convention.Aggregates.Convention(
            conventionId,
            command.Name,
            command.Slug);

        var registrant = convention.RegisterPerson(command.RegistrantName, command.RegistrantEmail);
        convention.AddAdministrator(registrant.Id, registrant.Id);

        await repository.CreateWithAdminAsync(convention, registrant, ct);
        return convention.Id.Value;
    }
}
