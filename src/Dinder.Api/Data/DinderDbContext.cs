using Dinder.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Dinder.Api.Data;

public sealed class DinderDbContext(DbContextOptions<DinderDbContext> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Household> Households => Set<Household>();
    public DbSet<HouseholdMember> HouseholdMembers => Set<HouseholdMember>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<PlanningRound> PlanningRounds => Set<PlanningRound>();
    public DbSet<Vote> Votes => Set<Vote>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        model.Entity<Household>(b => { b.ToTable("households"); b.Property(x => x.Name).HasMaxLength(100); });
        model.Entity<HouseholdMember>(b =>
        {
            b.ToTable("household_members");
            b.HasKey(x => new { x.HouseholdId, x.UserId });
            b.Property(x => x.Role).HasConversion<string>().HasMaxLength(16);
            b.HasOne<Household>().WithMany().HasForeignKey(x => x.HouseholdId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<Recipe>(b =>
        {
            b.ToTable("recipes");
            b.HasAlternateKey(x => new { x.HouseholdId, x.Id });
            b.Property(x => x.Title).HasMaxLength(200);
            b.Property(x => x.Description).HasMaxLength(1000);
            b.HasOne<Household>().WithMany().HasForeignKey(x => x.HouseholdId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<PlanningRound>(b =>
        {
            b.ToTable("planning_rounds");
            b.HasAlternateKey(x => new { x.HouseholdId, x.Id });
            b.HasIndex(x => x.HouseholdId).IsUnique().HasFilter("is_active = true");
            b.HasOne<Household>().WithMany().HasForeignKey(x => x.HouseholdId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<Vote>(b =>
        {
            b.ToTable("votes");
            b.HasKey(x => new { x.HouseholdId, x.RoundId, x.RecipeId, x.UserId });
            b.Property(x => x.Choice).HasConversion<string>().HasMaxLength(8);
            b.HasOne<HouseholdMember>().WithMany().HasForeignKey(x => new { x.HouseholdId, x.UserId }).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<PlanningRound>().WithMany().HasForeignKey(x => new { x.HouseholdId, x.RoundId }).HasPrincipalKey(x => new { x.HouseholdId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Recipe>().WithMany().HasForeignKey(x => new { x.HouseholdId, x.RecipeId }).HasPrincipalKey(x => new { x.HouseholdId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
        // Keep framework Identity tables conventional; domain columns use the documented household_id convention.
        foreach (var type in model.Model.GetEntityTypes().Where(t => t.ClrType.Namespace == typeof(Household).Namespace))
            foreach (var property in type.GetProperties())
                property.SetColumnName(string.Concat(property.Name.Select((c, i) => char.IsUpper(c) && i > 0 ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString())));
    }
}
