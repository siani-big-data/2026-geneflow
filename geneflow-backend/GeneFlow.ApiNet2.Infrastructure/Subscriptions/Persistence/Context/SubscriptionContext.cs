using GeneFlow.ApiNet2.Domain.Subscriptions;
using GeneFlow.ApiNet2.Domain.Subscriptions.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace GeneFlow.ApiNet2.Infrastructure.Subscriptions.Persistence.Context;

/// <summary>
/// DbContext for Subscriptions bounded context.
/// </summary>
public sealed class SubscriptionContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the SubscriptionContext.
    /// </summary>
    public SubscriptionContext(DbContextOptions<SubscriptionContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Subscriptions DbSet.
    /// </summary>
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("subscriptions");

        // Ignore enumerations - they are stored as values
        modelBuilder.Ignore<SubscriptionStatus>();
        modelBuilder.Ignore<BillingCycle>();

        // Only apply configurations from the Subscriptions namespace
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SubscriptionContext).Assembly,
            type => type.Namespace?.Contains("Subscriptions") == true);
    }
}
