using GeneFlow.ApiNet2.Domain.Notifications;
using GeneFlow.ApiNet2.Domain.Notifications.Entities;
using GeneFlow.ApiNet2.Domain.Notifications.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace GeneFlow.ApiNet2.Infrastructure.Notifications.Persistence.Context;

/// <summary>
/// DbContext for the Notifications bounded context. Owns the
/// <c>notifications</c> PostgreSQL schema (notifications + watches).
/// </summary>
public sealed class NotificationContext : DbContext
{
    public NotificationContext(DbContextOptions<NotificationContext> options) : base(options)
    {
    }

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Watch> Watches => Set<Watch>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("notifications");

        modelBuilder.Ignore<NotificationType>();
        modelBuilder.Ignore<WatchLevel>();

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(NotificationContext).Assembly,
            type => type.Namespace?.Contains("Notifications") == true);
    }
}
