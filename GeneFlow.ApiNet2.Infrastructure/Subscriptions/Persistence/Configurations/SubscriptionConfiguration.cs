using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Domain.Subscriptions;
using GeneFlow.ApiNet2.Domain.Subscriptions.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneFlow.ApiNet2.Infrastructure.Subscriptions.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Subscription aggregate.
/// </summary>
public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");

        builder.HasKey(s => s.Id);

        // Configure SubscriptionId (stored as string: S00000001)
        builder.Property(s => s.Id)
            .HasColumnName("id")
            .HasMaxLength(9)
            .HasConversion(
                id => id.ToString(),
                value => SubscriptionId.Parse(value));

        // Configure UserId (stored as string: U00000001)
        builder.Property(s => s.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(9)
            .HasConversion(
                id => id.ToString(),
                value => UserId.Parse(value))
            .IsRequired();

        // Configure PlanId (stored as string: L00000001)
        builder.Property(s => s.PlanId)
            .HasColumnName("plan_id")
            .HasMaxLength(9)
            .HasConversion(
                id => id.ToString(),
                value => PlanId.Parse(value))
            .IsRequired();

        builder.Property(s => s.PlanName)
            .HasColumnName("plan_name")
            .HasMaxLength(50)
            .IsRequired();

        // Configure Status (stored as int)
        builder.Property(s => s.Status)
            .HasColumnName("status")
            .HasConversion(
                status => status.Id,
                id => SubscriptionStatus.FromId(id)!)
            .IsRequired();

        // Configure BillingCycle (stored as int)
        builder.Property(s => s.BillingCycle)
            .HasColumnName("billing_cycle")
            .HasConversion(
                cycle => cycle.Id,
                id => BillingCycle.FromId(id)!)
            .IsRequired();

        // SubscriptionPeriod (owned value object)
        builder.OwnsOne(s => s.CurrentPeriod, period =>
        {
            period.Property(p => p.StartDate)
                .HasColumnName("period_start")
                .IsRequired();

            period.Property(p => p.EndDate)
                .HasColumnName("period_end")
                .IsRequired();

            // Ignore computed properties
            period.Ignore(p => p.IsActive);
            period.Ignore(p => p.HasExpired);
            period.Ignore(p => p.DaysRemaining);
            period.Ignore(p => p.Duration);
        });

        builder.Property(s => s.AutoRenew)
            .HasColumnName("auto_renew")
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(s => s.ModifiedAt)
            .HasColumnName("modified_at");

        builder.Property(s => s.CancelledAt)
            .HasColumnName("cancelled_at");

        builder.Property(s => s.CancellationReason)
            .HasColumnName("cancellation_reason")
            .HasMaxLength(500);

        builder.Property(s => s.TrialEndDate)
            .HasColumnName("trial_end_date");

        // Ignore computed properties
        builder.Ignore(s => s.GrantsAccess);
        builder.Ignore(s => s.IsFree);
        builder.Ignore(s => s.IsInTrial);

        // Indexes
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => new { s.UserId, s.Status });
        builder.HasIndex(s => s.PlanId);
        builder.HasIndex(s => s.Status);
    }
}
