using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Domain.Plans.Enumerations;
using GeneFlow.ApiNet2.Domain.Plans.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneFlow.ApiNet2.Infrastructure.Plans.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Plan aggregate.
/// </summary>
public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans");

        builder.HasKey(p => p.Id);

        // Configure PlanId
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => PlanId.From(value));

        // PlanName (owned value object)
        builder.OwnsOne(p => p.Name, name =>
        {
            name.Property(n => n.Value)
                .HasColumnName("name")
                .HasMaxLength(PlanName.MaxLength)
                .IsRequired();

            name.HasIndex(n => n.Value).IsUnique();
        });

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        // PlanPricing (owned value object)
        builder.OwnsOne(p => p.Pricing, pricing =>
        {
            pricing.Property(pr => pr.MonthlyPrice)
                .HasColumnName("monthly_price")
                .HasPrecision(10, 2)
                .IsRequired();

            pricing.Property(pr => pr.AnnualPrice)
                .HasColumnName("annual_price")
                .HasPrecision(10, 2)
                .IsRequired();

            pricing.Property(pr => pr.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();

            // Ignore computed properties
            pricing.Ignore(pr => pr.IsFree);
            pricing.Ignore(pr => pr.MonthlyEquivalentFromAnnual);
            pricing.Ignore(pr => pr.AnnualDiscountPercentage);
        });

        // PlanLimits (owned value object)
        builder.OwnsOne(p => p.Limits, limits =>
        {
            limits.Property(l => l.MaxStudies)
                .HasColumnName("max_studies")
                .IsRequired();

            limits.Property(l => l.MaxTracesPerMonth)
                .HasColumnName("max_traces_per_month")
                .IsRequired();

            limits.Property(l => l.MaxMembersPerStudy)
                .HasColumnName("max_members_per_study")
                .IsRequired();

            // Ignore computed properties
            limits.Ignore(l => l.IsUnlimitedStudies);
            limits.Ignore(l => l.IsUnlimitedTraces);
            limits.Ignore(l => l.IsUnlimitedMembers);
        });

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(p => p.IsDefault)
            .HasColumnName("is_default")
            .IsRequired();

        builder.Property(p => p.DisplayOrder)
            .HasColumnName("display_order")
            .IsRequired();

        // Features - stored as separate table
        builder.OwnsMany(p => p.Features, features =>
        {
            features.ToTable("plan_features");

            features.WithOwner().HasForeignKey("plan_id");

            features.Property<int>("id")
                .ValueGeneratedOnAdd();

            features.HasKey("id");

            features.Property(f => f.Id)
                .HasColumnName("featuREDACTED");

            features.Property(f => f.Name)
                .HasColumnName("featuREDACTED")
                .HasMaxLength(50);
        });

        // Auditing fields
        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100);

        builder.Property(p => p.ModifiedAt)
            .HasColumnName("modified_at");

        builder.Property(p => p.ModifiedBy)
            .HasColumnName("modified_by")
            .HasMaxLength(100);

        // Ignore computed properties
        builder.Ignore(p => p.IsFree);

        // Indexes
        builder.HasIndex(p => p.IsActive);
        builder.HasIndex(p => p.IsDefault);
        builder.HasIndex(p => p.DisplayOrder);

        // Seed data
        SeedPlans(builder);
    }

    private static void SeedPlans(EntityTypeBuilder<Plan> builder)
    {
        // We'll use HasData with anonymous types for owned entities
        // This requires a different approach - we'll create migrations manually
        // or use a seeding service
    }
}
