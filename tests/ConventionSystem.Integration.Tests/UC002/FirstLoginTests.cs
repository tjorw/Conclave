using ConventionSystem.Integration.Tests.Infrastructure;

namespace ConventionSystem.Integration.Tests.UC002;

public sealed class FirstLoginTests(ConventionSystemFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task FirstLogin_CreatesConventionUserLink_PersonIdInToken()
    {
        var (conventionId, email, password, _) = await ProvisionAsync();

        // Provisioning skapar konventionen och admin-kontot. Vi verifierar att
        // inloggning ger tillbaka ett JWT med person_id-claim (UC002-flödet).
        var token = await LoginAsync(conventionId, email, password);
        var claims = ParseClaims(token).ToList();

        var personIdClaim = claims.FirstOrDefault(c => c.Type == "person_id");
        Assert.NotNull(personIdClaim);
        Assert.True(Guid.TryParse(personIdClaim.Value, out var personId));
        Assert.NotEqual(Guid.Empty, personId);
    }

    [Fact]
    public async Task SecondLogin_ReturnsSamePersonId()
    {
        var (conventionId, email, password, _) = await ProvisionAsync();

        var token1 = await LoginAsync(conventionId, email, password);
        var token2 = await LoginAsync(conventionId, email, password);

        var personId1 = ParseClaims(token1).First(c => c.Type == "person_id").Value;
        var personId2 = ParseClaims(token2).First(c => c.Type == "person_id").Value;

        Assert.Equal(personId1, personId2);
    }
}
