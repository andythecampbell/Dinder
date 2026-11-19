using Dinder.Api.Domain;
using Xunit;

namespace Dinder.Api.Tests;

public sealed class PlanningRulesTests
{
    private static readonly Guid Household = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static HouseholdMember Member(HouseholdRole role = HouseholdRole.Member) => new() { HouseholdId = Household, UserId = "member", Role = role };

    [Fact]
    public void Membership_must_match_both_household_and_authenticated_user()
    {
        Assert.Throws<RuleViolation>(() => PlanningRules.RequireMember(null, Household, "member"));
        Assert.Throws<RuleViolation>(() => PlanningRules.RequireMember(Member(), Guid.NewGuid(), "member"));
        Assert.Throws<RuleViolation>(() => PlanningRules.RequireMember(Member(), Household, "someone-else"));
        Assert.Equal("member", PlanningRules.RequireMember(Member(), Household, "member").UserId);
    }

    [Fact]
    public void Only_chef_can_start_round_and_only_one_can_be_active()
    {
        Assert.Equal(403, Assert.Throws<RuleViolation>(() => PlanningRules.StartRound(Member(), false, DateTimeOffset.UnixEpoch)).StatusCode);
        Assert.Equal(409, Assert.Throws<RuleViolation>(() => PlanningRules.StartRound(Member(HouseholdRole.Chef), true, DateTimeOffset.UnixEpoch)).StatusCode);
        var round = PlanningRules.StartRound(Member(HouseholdRole.Chef), false, DateTimeOffset.UnixEpoch);
        Assert.Equal(Household, round.HouseholdId);
        Assert.Equal(DateTimeOffset.UnixEpoch, round.StartedAt);
        Assert.True(round.IsActive);
        Assert.Equal(15, round.TargetYesCount);
    }

    [Theory]
    [InlineData(VoteChoice.Yes)]
    [InlineData(VoteChoice.No)]
    [InlineData(VoteChoice.Skip)]
    public void Member_can_cast_each_choice_in_active_household_round(VoteChoice choice) =>
        PlanningRules.ValidateVote(Member(), new PlanningRound { HouseholdId = Household }, new Recipe { HouseholdId = Household }, choice);

    [Fact]
    public void Voting_rejects_closed_missing_and_cross_household_resources()
    {
        var round = new PlanningRound { HouseholdId = Household };
        var recipe = new Recipe { HouseholdId = Household };
        Assert.Equal(400, Assert.Throws<RuleViolation>(() => PlanningRules.ValidateVote(Member(), round, recipe, (VoteChoice)99)).StatusCode);
        Assert.Equal(404, Assert.Throws<RuleViolation>(() => PlanningRules.ValidateVote(Member(), null, recipe, VoteChoice.Yes)).StatusCode);
        Assert.Equal(404, Assert.Throws<RuleViolation>(() => PlanningRules.ValidateVote(Member(), round, null, VoteChoice.Yes)).StatusCode);
        Assert.Equal(404, Assert.Throws<RuleViolation>(() => PlanningRules.ValidateVote(Member(), new PlanningRound(), recipe, VoteChoice.Yes)).StatusCode);
        Assert.Equal(404, Assert.Throws<RuleViolation>(() => PlanningRules.ValidateVote(Member(), round, new Recipe(), VoteChoice.Yes)).StatusCode);
        round.IsActive = false;
        Assert.Equal(409, Assert.Throws<RuleViolation>(() => PlanningRules.ValidateVote(Member(), round, recipe, VoteChoice.Yes)).StatusCode);
    }
}
