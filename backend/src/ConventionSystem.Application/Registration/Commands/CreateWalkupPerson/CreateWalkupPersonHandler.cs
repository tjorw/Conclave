using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Registration.Commands.CreateWalkupPerson;

public sealed class CreateWalkupPersonHandler(
    IEditionRepository editionRepo,
    IConventionRepository conventionRepo,
    ICurrentUser currentUser,
    IPersonRepository personRepo)
    : ICommandHandler<CreateWalkupPersonCommand, Guid>
{
    public async Task<Guid> Handle(CreateWalkupPersonCommand command, CancellationToken ct)
    {
        var ctx = await EditionContextLoader.LoadWithReceptionStaffAsync(
            editionRepo, conventionRepo, new EditionId(command.EditionId), ct);

        ApplicationAuthorization.EnsureReceptionAccess(
            ctx.Convention, ctx.Edition, currentUser.PersonId,
            "Åtkomst kräver receptionsroll eller administratör.");

        if (await personRepo.EmailExistsInConventionAsync(ctx.Convention.Id, command.Email, ct))
            throw new InvalidOperationException(
                $"E-postadressen '{command.Email}' är redan registrerad i denna konvention.");

        var person = ctx.Convention.CreatePerson(command.Name, command.Email, command.Phone);
        await personRepo.AddAndSaveAsync(person, ct);
        return person.Id.Value;
    }
}
