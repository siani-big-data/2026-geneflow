using GeneFlow.ApiNet2.Domain.Activity;
using GeneFlow.ApiNet2.Domain.Activity.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace GeneFlow.ApiNet2.Infrastructure.Activity.Persistence.Context;

/// <summary>
/// DbContext for the Activity bounded context.
/// </summary>
public sealed class ActivityContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityContext"/>.
    /// </summary>
    public ActivityContext(DbContextOptions<ActivityContext> options) : base(options)
    {
    }

    /// <summary>
    /// Activity events projected from the event bus.
    /// </summary>
    public DbSet<ActivityEvent> ActivityEvents => Set<ActivityEvent>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("activity");

        // Smart enumerations are stored as strings, not as separate tables.
        modelBuilder.Ignore<ActivityVerb>();
        modelBuilder.Ignore<ActivityObjectType>();
        modelBuilder.Ignore<ActivityVisibility>();

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ActivityContext).Assembly,
            type => type.Namespace?.Contains("Activity") == true);
    }
}
