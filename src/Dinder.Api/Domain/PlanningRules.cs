namespace Dinder.Api.Domain;

public sealed class RuleViolation(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public static class PlanningRules
{
    public static HouseholdMember RequireMember(HouseholdMember? membership, Guid householdId, string userId)
    {
        if (membership is null || membership.HouseholdId != householdId || membership.UserId != userId)
            throw new RuleViolation(404, "Household not found.");
        return membership;
    }

    public static void RequireChef(HouseholdMember membership)
    {
        if (membership.Role != HouseholdRole.Chef)
            throw new RuleViolation(403, "Only the Chef can start a planning round.");
    }

    public static PlanningRound StartRound(HouseholdMember membership, bool alreadyActive, DateTimeOffset now)
    {
        RequireChef(membership);
        if (alreadyActive) throw new RuleViolation(409, "A planning round is already active.");
        return new PlanningRound { HouseholdId = membership.HouseholdId, StartedAt = now };
    }

    public static void ValidateVote(HouseholdMember membership, PlanningRound? round, Recipe? recipe, VoteChoice choice)
    {
        if (!Enum.IsDefined(choice)) throw new RuleViolation(400, "Choose Yes, No, or Skip.");
        if (round is null || recipe is null || round.HouseholdId != membership.HouseholdId || recipe.HouseholdId != membership.HouseholdId)
            throw new RuleViolation(404, "Round or recipe not found.");
        if (!round.IsActive) throw new RuleViolation(409, "This planning round is closed.");
    }
}
