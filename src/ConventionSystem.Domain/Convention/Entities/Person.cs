using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Domain.Convention.Entities;

public sealed class Person : Entity<PersonId>
{
    public ConventionId ConventionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Person() { }

    internal Person(PersonId id, ConventionId conventionId, string name, string email, string? phone)
        : base(id)
    {
        ConventionId = conventionId;
        Name = name;
        Email = email;
        Phone = phone;
    }

    internal void Update(string name, string email, string? phone)
    {
        Name = name;
        Email = email;
        Phone = phone;
    }

    internal void Deactivate() => IsActive = false;
}
