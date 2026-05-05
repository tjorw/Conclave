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

        var conventionPage = new Page(PageId.New(), conventionId, null, "rules", "Konventionsregler", "Konvention");
        conventionPage.Publish();
        var editionPage = new Page(PageId.New(), conventionId, new EditionId(editionId), "rules", "Upplageregler", "Upplaga");
        editionPage.Publish();
        await db.Pages.AddRangeAsync(conventionPage, editionPage);
        await db.SaveChangesAsync();

        var response = await CreateClient().GetAsync("/api/pages/rules");

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

        var firstId = await CreatePageAsync(client, "menu-first", "Zoo", true);
        var secondId = await CreatePageAsync(client, "menu-second", "Alpha", true);
        var thirdId = await CreatePageAsync(client, "menu-third", "Beta", true);
        var hiddenId = await CreatePageAsync(client, "menu-hidden", "Hidden", false);

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

        Assert.Equal(["menu-first", "menu-second", "menu-third"], items.Select(item => item.GetProperty("slug").GetString()).ToArray());
        Assert.DoesNotContain(items, item => item.GetProperty("slug").GetString() == "menu-hidden");
    }

    private static async Task<Guid> CreatePageAsync(HttpClient client, string slug, string title, bool showInPublicMenu)
    {
        var response = await client.PostAsJsonAsync("/api/pages", new
        {
            slug,
            title,
            content = "Text",
            editionId = (Guid?)null,
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
