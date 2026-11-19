using System.Security.Claims;
using Dinder.Api.Data;
using Dinder.Api.Domain;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dinder.Api.Features;

public static class ApiEndpoints
{
    public static void MapDinderApi(this WebApplication app)
    {
        app.MapGet("/api/auth/csrf", (HttpContext context, IAntiforgery antiforgery) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return Results.Ok(new { token = antiforgery.GetAndStoreTokens(context).RequestToken });
        });
        app.MapPost("/api/auth/login", async (LoginRequest input, SignInManager<IdentityUser> signIn) =>
        {
            if (string.IsNullOrWhiteSpace(input.Email) || input.Email.Length > 254 || string.IsNullOrEmpty(input.Password) || input.Password.Length > 256)
                return Results.Problem(statusCode: 400, title: "Enter an email address and password.");
            var result = await signIn.PasswordSignInAsync(input.Email.Trim(), input.Password, false, lockoutOnFailure: true);
            return result.Succeeded ? Results.NoContent() : Results.Problem(statusCode: 401, title: "Sign-in failed. Check your details or try again later.");
        }).RequireRateLimiting("login");
        app.MapPost("/api/auth/logout", async (SignInManager<IdentityUser> signIn) =>
        { await signIn.SignOutAsync(); return Results.NoContent(); }).RequireAuthorization();
        app.MapGet("/api/auth/me", async (ClaimsPrincipal user, DinderDbContext db, CancellationToken ct) =>
        {
            var id = UserId(user);
            var memberships = await (from member in db.HouseholdMembers.AsNoTracking()
                                     join household in db.Households.AsNoTracking() on member.HouseholdId equals household.Id
                                     where member.UserId == id
                                     select new MembershipDto(household.Id, household.Name, member.Role)).ToArrayAsync(ct);
            return new SessionDto(user.Identity!.Name!, memberships);
        }).RequireAuthorization();

        var kitchen = app.MapGroup("/api/households/{householdId:guid}").RequireAuthorization();
        kitchen.MapGet("/kitchen", async (Guid householdId, ClaimsPrincipal user, PlanningService service, DinderDbContext db, CancellationToken ct) =>
        {
            var userId = UserId(user);
            var membership = await service.Membership(householdId, userId, ct);
            var household = await db.Households.AsNoTracking().SingleAsync(x => x.Id == householdId, ct);
            var round = await db.PlanningRounds.AsNoTracking().SingleOrDefaultAsync(x => x.HouseholdId == householdId && x.IsActive, ct);
            var recipes = await db.Recipes.AsNoTracking().Where(x => x.HouseholdId == householdId).OrderBy(x => x.Title)
                .Select(x => new RecipeDto(x.Id, x.Title, x.Description, x.Minutes, x.Ingredients, x.Instructions, x.IsDevelopmentSeed)).ToArrayAsync(ct);
            var votes = round is null ? [] : await db.Votes.AsNoTracking().Where(x => x.HouseholdId == householdId && x.RoundId == round.Id && x.UserId == userId)
                .Select(x => new VoteDto(x.RecipeId, x.Choice)).ToArrayAsync(ct);
            return new KitchenDto(household.Name, membership.Role, round is null ? null : new RoundDto(round.Id, round.StartedAt, round.TargetYesCount), recipes, votes);
        });
        kitchen.MapPost("/rounds", async (Guid householdId, ClaimsPrincipal user, PlanningService service, CancellationToken ct) =>
        {
            var round = await service.Start(householdId, UserId(user), ct);
            return Results.Ok(new RoundDto(round.Id, round.StartedAt, round.TargetYesCount));
        });
        kitchen.MapPost("/rounds/{roundId:guid}/close", async (Guid householdId, Guid roundId, ClaimsPrincipal user, PlanningService service, CancellationToken ct) =>
        {
            await service.Close(householdId, roundId, UserId(user), ct);
            return Results.NoContent();
        });
        kitchen.MapPut("/rounds/{roundId:guid}/votes/{recipeId:guid}", async (Guid householdId, Guid roundId, Guid recipeId, VoteRequest input, ClaimsPrincipal user, PlanningService service, CancellationToken ct) =>
        {
            if (input.Choice is null) throw new RuleViolation(400, "Choose Yes, No, or Skip.");
            var vote = await service.Cast(householdId, roundId, recipeId, UserId(user), input.Choice.Value, ct);
            return new VoteDto(vote.RecipeId, vote.Choice);
        });
    }

    private static string UserId(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
