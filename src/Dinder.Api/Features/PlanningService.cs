using Dinder.Api.Data;
using Dinder.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Dinder.Api.Features;

public sealed class PlanningService(DinderDbContext db, TimeProvider clock)
{
    public async Task<HouseholdMember> Membership(Guid householdId, string userId, CancellationToken ct) =>
        PlanningRules.RequireMember(await db.HouseholdMembers.AsNoTracking().SingleOrDefaultAsync(x => x.HouseholdId == householdId && x.UserId == userId, ct), householdId, userId);

    public async Task<PlanningRound> Start(Guid householdId, string userId, CancellationToken ct)
    {
        var membership = await Membership(householdId, userId, ct);
        var round = PlanningRules.StartRound(membership, await db.PlanningRounds.AnyAsync(x => x.HouseholdId == householdId && x.IsActive, ct), clock.GetUtcNow());
        db.PlanningRounds.Add(round);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { throw new RuleViolation(409, "A planning round is already active."); }
        return round;
    }

    public async Task<Vote> Cast(Guid householdId, Guid roundId, Guid recipeId, string userId, VoteChoice choice, CancellationToken ct)
    {
        var membership = await Membership(householdId, userId, ct);
        var round = await db.PlanningRounds.AsNoTracking().SingleOrDefaultAsync(x => x.HouseholdId == householdId && x.Id == roundId, ct);
        var recipe = await db.Recipes.AsNoTracking().SingleOrDefaultAsync(x => x.HouseholdId == householdId && x.Id == recipeId, ct);
        PlanningRules.ValidateVote(membership, round, recipe, choice);
        var vote = await db.Votes.SingleOrDefaultAsync(x => x.HouseholdId == householdId && x.RoundId == roundId && x.RecipeId == recipeId && x.UserId == userId, ct);
        if (vote is null)
        {
            vote = new Vote { HouseholdId = householdId, RoundId = roundId, RecipeId = recipeId, UserId = userId };
            db.Votes.Add(vote);
        }
        vote.Choice = choice;
        vote.UpdatedAt = clock.GetUtcNow();
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { throw new RuleViolation(409, "Another vote was saved at the same time. Refresh and try again."); }
        return vote;
    }
}
