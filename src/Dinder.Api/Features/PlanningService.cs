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

    public async Task Close(Guid householdId, Guid roundId, string userId, CancellationToken ct)
    {
        var membership = await Membership(householdId, userId, ct);
        var round = await db.PlanningRounds.AsNoTracking().SingleOrDefaultAsync(x => x.HouseholdId == householdId && x.Id == roundId, ct);
        PlanningRules.CloseRound(membership, round);
        // The flag flip is a single conditional UPDATE, so a second close racing this one
        // matches zero rows and reports a conflict instead of returning 204 twice.
        var closed = await db.PlanningRounds
            .Where(x => x.HouseholdId == householdId && x.Id == roundId && x.IsActive)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsActive, false), ct);
        if (closed == 0) throw new RuleViolation(409, "This planning round is already closed.");
    }

    public async Task<Vote> Cast(Guid householdId, Guid roundId, Guid recipeId, string userId, VoteChoice choice, CancellationToken ct)
    {
        var membership = await Membership(householdId, userId, ct);
        var recipe = await db.Recipes.AsNoTracking().SingleOrDefaultAsync(x => x.HouseholdId == householdId && x.Id == recipeId, ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Lock the round row for the duration of the vote write. The no-op UPDATE is the
        // lock: PostgreSQL holds the row lock until commit and SQLite takes the write lock,
        // so a close cannot slip between the activity check and the vote insert. Zero rows
        // means the round was already closed, which the rule check below reports as 409.
        var locked = await db.PlanningRounds
            .Where(x => x.HouseholdId == householdId && x.Id == roundId && x.IsActive)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsActive, x => x.IsActive), ct);
        var round = await db.PlanningRounds.AsNoTracking().SingleOrDefaultAsync(x => x.HouseholdId == householdId && x.Id == roundId, ct);
        PlanningRules.ValidateVote(membership, round, recipe, choice);
        if (locked == 0) throw new RuleViolation(409, "This planning round is closed.");
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
        await transaction.CommitAsync(ct);
        return vote;
    }
}
