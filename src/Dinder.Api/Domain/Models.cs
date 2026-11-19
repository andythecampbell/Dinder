namespace Dinder.Api.Domain;

public enum HouseholdRole { Member, Chef }
public enum VoteChoice { Yes, No, Skip }

public sealed class Household
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
}

public sealed class HouseholdMember
{
    public Guid HouseholdId { get; set; }
    public string UserId { get; set; } = "";
    public HouseholdRole Role { get; set; }
}

public sealed class Recipe
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HouseholdId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int Minutes { get; set; }
    public string[] Ingredients { get; set; } = [];
    public string[] Instructions { get; set; } = [];
    public bool IsDevelopmentSeed { get; set; }
}

public sealed class PlanningRound
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HouseholdId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public int TargetYesCount { get; set; } = 15;
}

public sealed class Vote
{
    public Guid HouseholdId { get; set; }
    public Guid RoundId { get; set; }
    public Guid RecipeId { get; set; }
    public string UserId { get; set; } = "";
    public VoteChoice Choice { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
