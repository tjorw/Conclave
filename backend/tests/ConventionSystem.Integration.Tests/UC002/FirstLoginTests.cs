using ConventionSystem.Integration.Tests.Infrastructure;

namespace ConventionSystem.Integration.Tests.UC002;

public sealed class FirstLoginTests(ConventionSystemFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task FirstLogin_PersonIdInToken()
    {
        var (email, password) = await Factory.CreateTestUserAsync();

        var token = await LoginAsync(email, password);
        var claims = ParseClaims(token).ToList();

        var personIdClaim = claims.FirstOrDefault(c => c.Type == "person_id");
        Assert.NotNull(personIdClaim);
        Assert.True(Guid.TryParse(personIdClaim.Value, out var personId));
        Assert.NotEqual(Guid.Empty, personId);
    }

    [Fact]
    public async Task SecondLogin_ReturnsSamePersonId()
    {
        var (email, password) = await Factory.CreateTestUserAsync();

        var token1 = await LoginAsync(email, password);
        var token2 = await LoginAsync(email, password);

        var personId1 = ParseClaims(token1).First(c => c.Type == "person_id").Value;
        var personId2 = ParseClaims(token2).First(c => c.Type == "person_id").Value;

        Assert.Equal(personId1, personId2);
    }
}
