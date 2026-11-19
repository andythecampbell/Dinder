using Dinder.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dinder.Api.Data;

public static class DevelopmentSeed
{
    public static readonly Guid HouseholdId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    public static async Task Run(IServiceProvider services, IConfiguration configuration)
    {
        var db = services.GetRequiredService<DinderDbContext>();
        var users = services.GetRequiredService<UserManager<IdentityUser>>();
        // No shared fallback password. Local setup generates values; test hosts supply their own.
        var chefPassword = configuration["Bootstrap:ChefPassword"] ?? throw new InvalidOperationException("Set Bootstrap__ChefPassword for development seeding.");
        var memberPassword = configuration["Bootstrap:MemberPassword"] ?? throw new InvalidOperationException("Set Bootstrap__MemberPassword for development seeding.");
        await using var transaction = await db.Database.BeginTransactionAsync();
        if (!await db.Households.AnyAsync(x => x.Id == HouseholdId))
            db.Households.Add(new Household { Id = HouseholdId, Name = "Our kitchen" });
        await db.SaveChangesAsync();
        await AddMember("chef@dinder.local", chefPassword, HouseholdRole.Chef);
        await AddMember("member@dinder.local", memberPassword, HouseholdRole.Member);
        if (!await db.Recipes.AnyAsync(x => x.HouseholdId == HouseholdId))
        {
            db.Recipes.AddRange(
                Seed("Lemon chickpea bowls", "Bright lemon, warm chickpeas, and a little crunch.", 20,
                    ["Chickpeas", "Cooked rice", "Cucumber", "Lemon", "Olive oil"],
                    ["Warm the chickpeas with a little olive oil.", "Spoon over rice and add chopped cucumber.", "Finish with lemon juice and season to taste."]),
                Seed("Tomato & basil pasta", "A simple, cozy dinner for the middle of the week.", 25,
                    ["Pasta", "Chopped tomatoes", "Garlic", "Basil", "Olive oil"],
                    ["Cook pasta according to its package.", "Soften garlic in oil, add tomatoes, and simmer.", "Toss with pasta and fresh basil."]),
                Seed("Sheet-pan vegetable wraps", "Roasted vegetables, tucked into a warm tortilla.", 35,
                    ["Bell peppers", "Courgette", "Red onion", "Tortillas", "Plain yogurt"],
                    ["Slice vegetables, toss with oil, and season.", "Roast at 220°C until tender, about 25 minutes.", "Serve in warm tortillas with yogurt."]));
        }
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        async Task AddMember(string email, string password, HouseholdRole role)
        {
            var user = await users.FindByEmailAsync(email);
            if (user is null)
            {
                user = new IdentityUser { UserName = email, Email = email };
                var result = await users.CreateAsync(user, password);
                if (!result.Succeeded) throw new InvalidOperationException("Bootstrap account creation failed: " + string.Join(", ", result.Errors.Select(x => x.Code)));
            }
            if (!await db.HouseholdMembers.AnyAsync(x => x.HouseholdId == HouseholdId && x.UserId == user.Id))
                db.HouseholdMembers.Add(new HouseholdMember { HouseholdId = HouseholdId, UserId = user.Id, Role = role });
            await db.SaveChangesAsync();
        }
        static Recipe Seed(string title, string description, int minutes, string[] ingredients, string[] instructions) => new()
        { HouseholdId = HouseholdId, Title = title, Description = description, Minutes = minutes, Ingredients = ingredients, Instructions = instructions, IsDevelopmentSeed = true };
    }
}
