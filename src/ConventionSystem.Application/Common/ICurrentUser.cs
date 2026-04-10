using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Common;

public interface ICurrentUser
{
    PersonId PersonId { get; }
}
