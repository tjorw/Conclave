namespace ConventionSystem.Integration.Tests.Infrastructure;

// En container delas av alla integrationstestklasser i samlingen.
// Varje test provisionerar sin egna konvention och databas för isolering.
[CollectionDefinition(Name)]
public sealed class ConventionTestCollection : ICollectionFixture<ConventionSystemFactory>
{
    public const string Name = "Integration";
}
