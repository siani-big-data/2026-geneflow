using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Domain.Plans.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace GeneFlow.ApiNet2.Infrastructure.Plans.Persistence.Context;

/// <summary>
/// DbContext for Plans bounded context.
/// </summary>
public sealed class PlanContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the PlanContext.
    /// </summary>
    public PlanContext(DbContextOptions<PlanContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Plans DbSet.
    /// </summary>
    public DbSet<Plan> Plans => Set<Plan>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("plans");

        // Ignore enumerations - they are stored as values
        modelBuilder.Ignore<PlanFeature>();

        // Only apply configurations from the Plans namespace
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(PlanContext).Assembly,
            type => type.Namespace?.Contains("Plans") == true);
    }
}
