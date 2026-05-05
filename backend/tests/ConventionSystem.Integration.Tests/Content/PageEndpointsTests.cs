using System.Net;
using ConventionSystem.Domain.Content.Aggregates;
using ConventionSystem.Domain.Content.Ids;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Infrastructure.Persistence;
using ConventionSystem.Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConventionSystem.Integration.Tests.Content;

public sealed class PageEndpointsTests(ConventionSystemFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task PublicPage_ReturnsPublishedConventionScopedPage()
    {
        var token = await LoginAsync(AdminEmail, AdminPassword);
        var client = CreateClient(token);
        var create = await client.PostAsJsonAsync("/api/pages", new
        {
            slug = "info",
            title = "Info",
            content = "**Hej**",
            editionId = (Guid?)null
        });
        create.EnsureSuccessStatusCode();
        var pageId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await client.PostAsync($"/api/pages/{pageId}/publish", null)).EnsureSuccessStatusCode();

        var publicClient = CreateClient();
        var response = await publicClient.GetAsync("/api/pages/info");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Info", body.GetProperty("title").GetString());
        Assert.Equal("**Hej**", body.GetProperty("content").GetString());
    }

    [Fact]
    public async Task PublicPage_DoesNotReturnUnpublishedPage()
    {
        var token = await LoginAsync(AdminEmail, AdminPassword);
        var client = CreateClient(token);
        var create = await client.PostAsJsonAsync("/api/pages", new
        {
            slug = "draft",
            title = "Draft",
            content = "Ej publicerad",
            editionId = (Guid?)null
        });
        create.EnsureSuccessStatusCode();

        var response = await CreateClient().GetAsync("/api/pages/draft");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PublicPage_PrioritizesActiveEditionScopedPage()
    {
        var editionId = await CreateActiveEditionAsync();
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
        var conventionId = new ConventionId(Factory.SeededConventionId);
        const string slug = "rules-public-priority";

        var conventionPage = new Page(PageId.New(), conventionId, null, slug, "Konventionsregler", "Konvention");
        conventionPage.Publish();
        var editionPage = new Page(PageId.New(), conventionId, new EditionId(editionId), slug, "Upplageregler", "Upplaga");
        editionPage.Publish();
        await db.Pages.AddRangeAsync(conventionPage, editionPage);
        await db.SaveChangesAsync();

        var response = await CreateClient().GetAsync($"/api/pages/{slug}");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Upplageregler", body.GetProperty("title").GetString());
        Assert.Equal(editionId, body.GetProperty("editionId").GetGuid());
    }

    [Fact]
    public async Task CreatePage_WithDuplicateSlugInSameScope_ReturnsSlugError()
    {
        var token = await LoginAsync(AdminEmail, AdminPassword);
        var client = CreateClient(token);
        var payload = new { slug = "same", title = "Sida", content = "Text", editionId = (Guid?)null };
        (await client.PostAsJsonAsync("/api/pages", payload)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/pages", payload);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("page_slug_already_exists", body.GetProperty("errorCode").GetString());
        Assert.Equal("Sluggen finns redan i valt scope.", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task ListPages_FiltersByExactScope()
    {
        var editionId = await CreateActiveEditionAsync();
        var token = await LoginAsync(AdminEmail, AdminPassword);
        var client = CreateClient(token);

        (await client.PostAsJsonAsync("/api/pages", new
        {
            slug = "scope-convention",
            title = "Konventionssida",
            content = "Konvention",
            editionId = (Guid?)null
        })).EnsureSuccessStatusCode();

        (await client.PostAsJsonAsync("/api/pages", new
        {
            slug = "scope-edition",
            title = "Upplagesida",
            content = "Upplaga",
            editionId
        })).EnsureSuccessStatusCode();

        var conventionResponse = await client.GetAsync("/api/pages");
        var editionResponse = await client.GetAsync($"/api/pages?editionId={editionId}");

        conventionResponse.EnsureSuccessStatusCode();
        editionResponse.EnsureSuccessStatusCode();

        var conventionPages = await conventionResponse.Content.ReadFromJsonAsync<JsonElement>();
        var editionPages = await editionResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Contains(conventionPages.EnumerateArray(), p => p.GetProperty("slug").GetString() == "scope-convention");
        Assert.DoesNotContain(conventionPages.EnumerateArray(), p => p.GetProperty("slug").GetString() == "scope-edition");
        Assert.Contains(editionPages.EnumerateArray(), p => p.GetProperty("slug").GetString() == "scope-edition");
        Assert.DoesNotContain(editionPages.EnumerateArray(), p => p.GetProperty("slug").GetString() == "scope-convention");
    }

    [Fact]
    public async Task UpdatePageMenuOrder_WithNegativeValue_ReturnsValidationError()
    {
        var token = await LoginAsync(AdminEmail, AdminPassword);
        var client = CreateClient(token);
        var create = await client.PostAsJsonAsync("/api/pages", new
        {
            slug = "menu-order-invalid",
            title = "Menyordning",
            content = "Text",
            editionId = (Guid?)null,
            showInPublicMenu = true
        });
        create.EnsureSuccessStatusCode();
        var pageId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var response = await client.PatchAsJsonAsync($"/api/pages/{pageId}/menu-order", new { menuSortOrder = -1 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("page_menu_sort_order_must_be_non_negative", body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task PublicMenuPages_SortsByMenuOrderThenTitle_AndIgnoresHiddenPages()
    {
        var token = await LoginAsync(AdminEmail, AdminPassword);
        var client = CreateClient(token);

        var firstId = await CreatePageAsync(client, "menu-first", "Zoo", true, null);
        var secondId = await CreatePageAsync(client, "menu-second", "Alpha", true, null);
        var thirdId = await CreatePageAsync(client, "menu-third", "Beta", true, null);
        var hiddenId = await CreatePageAsync(client, "menu-hidden", "Hidden", false, null);

        (await client.PatchAsJsonAsync($"/api/pages/{firstId}/menu-order", new { menuSortOrder = 1 })).EnsureSuccessStatusCode();
        (await client.PatchAsJsonAsync($"/api/pages/{secondId}/menu-order", new { menuSortOrder = 2 })).EnsureSuccessStatusCode();
        (await client.PatchAsJsonAsync($"/api/pages/{thirdId}/menu-order", new { menuSortOrder = 2 })).EnsureSuccessStatusCode();
        (await client.PatchAsJsonAsync($"/api/pages/{hiddenId}/menu-order", new { menuSortOrder = 0 })).EnsureSuccessStatusCode();

        (await client.PostAsync($"/api/pages/{firstId}/publish", null)).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/pages/{secondId}/publish", null)).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/pages/{thirdId}/publish", null)).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/pages/{hiddenId}/publish", null)).EnsureSuccessStatusCode();

        var response = await CreateClient().GetAsync("/api/pages/menu");

        response.EnsureSuccessStatusCode();
        var items = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();

        Assert.Equal(["menu-first", "menu-second", "menu-third"], items.Select(item => item.GetProperty("slug").GetString()!).ToArray());
        Assert.DoesNotContain(items, item => item.GetProperty("slug").GetString() == "menu-hidden");
    }

    [Fact]
    public async Task PublicMenuPages_PrioritizesActiveEditionScopedPage_WhenSlugCollides()
    {
        var editionId = await CreateActiveEditionAsync();
        var token = await LoginAsync(AdminEmail, AdminPassword);
        var client = CreateClient(token);
        const string slug = "rules-menu-priority";

        var conventionRulesId = await CreatePageAsync(client, slug, "Konventionsregler", true, null);
        var editionRulesId = await CreatePageAsync(client, slug, "Upplageregler", true, editionId);
        const string aboutSlug = "about-menu-priority";
        var aboutId = await CreatePageAsync(client, aboutSlug, "Om konventet", true, null);

        (await client.PatchAsJsonAsync($"/api/pages/{conventionRulesId}/menu-order", new { menuSortOrder = 1 })).EnsureSuccessStatusCode();
        (await client.PatchAsJsonAsync($"/api/pages/{editionRulesId}/menu-order", new { menuSortOrder = 1 })).EnsureSuccessStatusCode();
        (await client.PatchAsJsonAsync($"/api/pages/{aboutId}/menu-order", new { menuSortOrder = 0 })).EnsureSuccessStatusCode();

        (await client.PostAsync($"/api/pages/{conventionRulesId}/publish", null)).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/pages/{editionRulesId}/publish", null)).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/pages/{aboutId}/publish", null)).EnsureSuccessStatusCode();

        var response = await CreateClient().GetAsync("/api/pages/menu");

        response.EnsureSuccessStatusCode();
        var items = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();

        var rulesItems = items.Where(item => item.GetProperty("slug").GetString() == slug).ToList();
        Assert.Single(rulesItems);

        var rulesItem = rulesItems.Single();
        Assert.Equal(editionId, rulesItem.GetProperty("editionId").GetGuid());
        Assert.Equal("Upplageregler", rulesItem.GetProperty("title").GetString());

        var aboutIndex = items.FindIndex(item => item.GetProperty("slug").GetString() == aboutSlug);
        var rulesIndex = items.FindIndex(item => item.GetProperty("slug").GetString() == slug);
        Assert.True(aboutIndex >= 0);
        Assert.True(rulesIndex >= 0);
        Assert.True(aboutIndex < rulesIndex);
    }

    [Fact]
    public async Task PublicMenuPages_UsesSelectedScopeMenuSortOrder_WhenSlugCollides()
    {
        var editionId = await CreateActiveEditionAsync();
        var token = await LoginAsync(AdminEmail, AdminPassword);
        var client = CreateClient(token);
        const string slug = "rules-menu-order-scope";

        var conventionRulesId = await CreatePageAsync(client, slug, "Konventionsregler", true, null);
        var editionRulesId = await CreatePageAsync(client, slug, "Upplageregler", true, editionId);
        const string aboutSlug = "about-menu-order-scope";
        var aboutId = await CreatePageAsync(client, aboutSlug, "Om konventet", true, null);

        // Scope-separerad ordning: convention-rules får lägre ordningstal,
        // men eftersom aktiv upplaga prioriteras ska edition-rules styra slutlig ordning.
        (await client.PatchAsJsonAsync($"/api/pages/{conventionRulesId}/menu-order", new { menuSortOrder = 0 })).EnsureSuccessStatusCode();
        (await client.PatchAsJsonAsync($"/api/pages/{editionRulesId}/menu-order", new { menuSortOrder = 5 })).EnsureSuccessStatusCode();
        (await client.PatchAsJsonAsync($"/api/pages/{aboutId}/menu-order", new { menuSortOrder = 2 })).EnsureSuccessStatusCode();

        (await client.PostAsync($"/api/pages/{conventionRulesId}/publish", null)).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/pages/{editionRulesId}/publish", null)).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/pages/{aboutId}/publish", null)).EnsureSuccessStatusCode();

        var response = await CreateClient().GetAsync("/api/pages/menu");

        response.EnsureSuccessStatusCode();
        var items = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();

        var rulesItem = items.Single(item => item.GetProperty("slug").GetString() == slug);
        Assert.Equal(editionId, rulesItem.GetProperty("editionId").GetGuid());
        Assert.Equal(5, rulesItem.GetProperty("menuSortOrder").GetInt32());

        Assert.Equal([aboutSlug, slug], items.Select(item => item.GetProperty("slug").GetString()!).Where(s => s is aboutSlug or slug).ToArray());
    }

    private static async Task<Guid> CreatePageAsync(HttpClient client, string slug, string title, bool showInPublicMenu, Guid? editionId)
    {
        var response = await client.PostAsJsonAsync("/api/pages", new
        {
            slug,
            title,
            content = "Text",
            editionId,
            showInPublicMenu
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreateActiveEditionAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
        var convention = await db.Conventions
            .Include(c => c.Administrators)
            .FirstAsync(c => c.Id == new ConventionId(Factory.SeededConventionId));
        var admin = await db.Persons.FirstAsync(p => p.Email == AdminEmail);
        var edition = convention.CreateEdition(
            "Test 2027",
            new DatePeriod(new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 3)),
            admin.Id,
            admin.Id);
        convention.SetActiveEdition(edition.Id);
        await db.Editions.AddAsync(edition);
        await db.SaveChangesAsync();
        return edition.Id.Value;
    }
}
