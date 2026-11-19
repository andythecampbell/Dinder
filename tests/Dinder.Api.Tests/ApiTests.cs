using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dinder.Api.Data;
using Dinder.Api.Domain;
using Dinder.Api.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dinder.Api.Tests;

public sealed class ApiTests : IDisposable
{
    private readonly TestApplication app = new();
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private static string KitchenPath => $"/api/households/{DevelopmentSeed.HouseholdId}/kitchen";
    private static string RoundsPath => $"/api/households/{DevelopmentSeed.HouseholdId}/rounds";
    private HttpClient Client() => app.CreateClient(new() { AllowAutoRedirect = false });

    [Fact]
    public async Task Anonymous_cannot_read_household_or_start_round()
    {
        using var client = Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(KitchenPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(RoundsPath, null)).StatusCode);
    }

    [Fact]
    public async Task Login_and_authenticated_mutations_require_csrf_and_logout_revokes_session()
    {
        using var client = Client();
        var body = new { email = "chef@dinder.local", password = TestApplication.Password };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/login", body)).StatusCode);
        await Login(client, body.email);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(RoundsPath, null)).StatusCode);
        await Csrf(client);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(KitchenPath)).StatusCode);
    }

    [Fact]
    public async Task Member_cannot_start_round_and_chef_cannot_start_second_active_round()
    {
        using var member = Client(); await Login(member, "member@dinder.local");
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PostAsync(RoundsPath, null)).StatusCode);
        using var chef = Client(); await Login(chef, "chef@dinder.local");
        Assert.Equal(HttpStatusCode.OK, (await chef.PostAsync(RoundsPath, null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await chef.PostAsync(RoundsPath, null)).StatusCode);
    }

    [Theory]
    [InlineData(VoteChoice.Yes)]
    [InlineData(VoteChoice.No)]
    [InlineData(VoteChoice.Skip)]
    public async Task Votes_persist_across_requests_are_private_and_update_without_duplicates(VoteChoice choice)
    {
        using var chef = Client(); await Login(chef, "chef@dinder.local");
        await chef.PostAsync(RoundsPath, null);
        using var member = Client(); await Login(member, "member@dinder.local");
        var kitchen = await GetKitchen(member);
        var path = $"{RoundsPath}/{kitchen.ActiveRound!.Id}/votes/{kitchen.Recipes[0].Id}";
        Assert.Equal(HttpStatusCode.OK, (await member.PutAsJsonAsync(path, new { choice = "Yes" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await member.PutAsJsonAsync(path, new { choice = choice.ToString() })).StatusCode);
        using var freshClient = Client(); await Login(freshClient, "member@dinder.local");
        Assert.Equal(choice, Assert.Single((await GetKitchen(freshClient)).MyVotes).Choice);
        Assert.Empty((await GetKitchen(chef)).MyVotes);
        using var scope = app.Services.CreateScope();
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<DinderDbContext>().Votes.CountAsync());
    }

    [Fact]
    public async Task Foreign_household_and_foreign_recipe_cannot_be_read_or_mutated()
    {
        using var chef = Client(); await Login(chef, "chef@dinder.local");
        await chef.PostAsync(RoundsPath, null);
        var kitchen = await GetKitchen(chef);
        var other = new Household { Name = "Another family" };
        var otherRecipe = new Recipe { HouseholdId = other.Id, Title = "Private recipe" };
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DinderDbContext>();
            db.AddRange(other, otherRecipe);
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.NotFound, (await chef.GetAsync($"/api/households/{other.Id}/kitchen")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await chef.PostAsync($"/api/households/{other.Id}/rounds", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await chef.PutAsJsonAsync($"{RoundsPath}/{kitchen.ActiveRound!.Id}/votes/{otherRecipe.Id}", new { choice = "Yes" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await chef.PutAsJsonAsync($"/api/households/{other.Id}/rounds/{kitchen.ActiveRound.Id}/votes/{otherRecipe.Id}", new { choice = "Yes" })).StatusCode);
    }

    [Fact]
    public async Task Missing_invalid_numeric_votes_and_closed_round_are_rejected()
    {
        using var chef = Client(); await Login(chef, "chef@dinder.local");
        await chef.PostAsync(RoundsPath, null);
        var kitchen = await GetKitchen(chef);
        var path = $"{RoundsPath}/{kitchen.ActiveRound!.Id}/votes/{kitchen.Recipes[0].Id}";
        Assert.Equal(HttpStatusCode.BadRequest, (await chef.PutAsJsonAsync(path, new { choice = "Maybe" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await chef.PutAsJsonAsync(path, new { choice = 0 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await chef.PutAsJsonAsync(path, new { })).StatusCode);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DinderDbContext>();
            (await db.PlanningRounds.SingleAsync()).IsActive = false;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Conflict, (await chef.PutAsJsonAsync(path, new { choice = "Yes" })).StatusCode);
    }

    [Fact]
    public async Task Database_rejects_cross_household_vote_even_if_application_checks_are_bypassed()
    {
        using var chef = Client(); await Login(chef, "chef@dinder.local");
        await chef.PostAsync(RoundsPath, null);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DinderDbContext>();
        var other = new Household { Name = "Other" };
        var recipe = new Recipe { HouseholdId = other.Id, Title = "Other recipe" };
        db.AddRange(other, recipe); await db.SaveChangesAsync();
        var member = await db.HouseholdMembers.FirstAsync();
        db.Votes.Add(new Vote { HouseholdId = DevelopmentSeed.HouseholdId, RecipeId = recipe.Id, RoundId = (await db.PlanningRounds.SingleAsync()).Id, UserId = member.UserId, Choice = VoteChoice.Yes });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    private static async Task Csrf(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", response.GetProperty("token").GetString());
    }
    private static async Task Login(HttpClient client, string email)
    {
        await Csrf(client);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/login", new { email, password = TestApplication.Password })).StatusCode);
        await Csrf(client);
    }
    private static async Task<KitchenDto> GetKitchen(HttpClient client) => (await client.GetFromJsonAsync<KitchenDto>(KitchenPath, Json))!;
    public void Dispose() => app.Dispose();
}
