using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Entities;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;
using GeneFlow.ApiNet2.Domain.Studies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneFlow.ApiNet2.Infrastructure.Pipelines.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Pipeline aggregate.
/// </summary>
public sealed class PipelineConfiguration : IEntityTypeConfiguration<Pipeline>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Pipeline> builder)
    {
        builder.ToTable("pipelines");

        builder.HasKey(p => p.Id);

        // Configure PipelineId
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => PipelineId.Parse(value));

        // Configure StudyId (reference to Study aggregate)
        builder.Property(p => p.StudyId)
            .HasColumnName("study_id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => StudyId.Parse(value))
            .IsRequired();

        builder.HasIndex(p => p.StudyId);

        // Configure OwnerId (reference to User aggregate)
        builder.Property(p => p.OwnerId)
            .HasColumnName("owner_id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => UserId.Parse(value))
            .IsRequired();

        builder.HasIndex(p => p.OwnerId);

        // PipelineName (owned value object)
        builder.OwnsOne(p => p.Name, name =>
        {
            name.Property(n => n.Value)
                .HasColumnName("name")
                .HasMaxLength(PipelineName.MaxLength)
                .IsRequired();

            // Index for study + name uniqueness (on the owned property)
            name.HasIndex(n => n.Value);
        });

        // PipelineDescription (owned value object)
        builder.OwnsOne(p => p.Description, description =>
        {
            description.Property(d => d.Value)
                .HasColumnName("description")
                .HasMaxLength(PipelineDescription.MaxLength);
        });

        // PipelineStatus (smart enumeration stored as string)
        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(
                status => status.Name,
                name => PipelineStatus.FromName(name)!)
            .IsRequired();

        builder.HasIndex(p => p.Status);

        // Audit columns
        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(50);

        builder.Property(p => p.ModifiedAt)
            .HasColumnName("modified_at");

        builder.Property(p => p.ModifiedBy)
            .HasColumnName("modified_by")
            .HasMaxLength(50);

        // Steps (owned collection)
        builder.OwnsMany(p => p.Steps, step =>
        {
            step.ToTable("pipeline_steps");

            step.Property(s => s.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();
            step.HasKey(s => s.Id);

            step.WithOwner().HasForeignKey("pipeline_id");

            step.Property(s => s.StepType)
                .HasColumnName("step_type")
                .HasMaxLength(30)
                .HasConversion(
                    type => type.Name,
                    name => StepType.FromName(name)!)
                .IsRequired();

            step.Property(s => s.Order)
                .HasColumnName("order")
                .IsRequired();

            step.Property(s => s.Label)
                .HasColumnName("label")
                .HasMaxLength(PipelineStep.MaxLabelLength);

            step.OwnsOne(s => s.Configuration, config =>
            {
                config.Property(c => c.Value)
                    .HasColumnName("configuration")
                    .HasMaxLength(StepConfiguration.MaxLength)
                    .HasDefaultValue("{}");
            });

            step.Property(s => s.IsEnabled)
                .HasColumnName("is_enabled")
                .HasDefaultValue(true);

            step.Property(s => s.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            step.HasIndex("pipeline_id", "Order").IsUnique();
        });

        // Note: Composite index for study + name uniqueness is configured via migration
        // since Name is an owned type and cannot be referenced directly in HasIndex
    }
}
