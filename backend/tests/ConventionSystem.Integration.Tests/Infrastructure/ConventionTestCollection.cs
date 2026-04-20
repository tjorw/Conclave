namespace ConventionSystem.Integration.Tests.Infrastructure;

// En container delas av alla integrationstestklasser i samlingen.
// Alla tester körs mot samma seedade konvention; isolering sker per testkonto.
[CollectionDefinition(Name)]
public sealed class ConventionTestCollection : ICollectionFixture<ConventionSystemFactory>
{
    public const string Name = "Integration";
}
