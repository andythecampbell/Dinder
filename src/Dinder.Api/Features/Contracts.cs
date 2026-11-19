using Dinder.Api.Domain;

namespace Dinder.Api.Features;

public sealed record LoginRequest(string Email, string Password);
public sealed record VoteRequest(VoteChoice? Choice);
public sealed record MembershipDto(Guid Id, string Name, HouseholdRole Role);
public sealed record SessionDto(string Email, MembershipDto[] Households);
public sealed record RecipeDto(Guid Id, string Title, string Description, int Minutes, string[] Ingredients, string[] Instructions, bool IsDevelopmentSeed);
public sealed record RoundDto(Guid Id, DateTimeOffset StartedAt, int TargetYesCount);
public sealed record VoteDto(Guid RecipeId, VoteChoice Choice);
public sealed record KitchenDto(string Name, HouseholdRole Role, RoundDto? ActiveRound, RecipeDto[] Recipes, VoteDto[] MyVotes);
