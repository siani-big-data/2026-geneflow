using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Entities;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneFlow.ApiNet2.Infrastructure.Pipelines.Persistence.Configurations;

/// <summary>
/// EF Core configuration for PipelineExecution entity.
/// </summary>
public sealed class PipelineExecutionConfiguration : IEntityTypeConfiguration<PipelineExecution>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PipelineExecution> builder)
    {
        builder.ToTable("pipeline_executions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => PipelineExecutionId.Parse(value));

        builder.Property(e => e.PipelineId)
            .HasColumnName("pipeline_id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => PipelineId.Parse(value))
            .IsRequired();

        builder.HasIndex(e => e.PipelineId);

        builder.Property(e => e.TraceId)
            .HasColumnName("trace_id")
            .HasColumnType("uuid")
            .HasConversion(
                id => id.Value,
                value => TraceId.From(value))
            .IsRequired();

        builder.HasIndex(e => e.TraceId);

        builder.Property(e => e.StartedBy)
            .HasColumnName("started_by")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => UserId.Parse(value))
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(
                status => status.Name,
                name => ExecutionStatus.FromName(name)!)
            .IsRequired();

        builder.HasIndex(e => e.Status);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.StartedAt)
            .HasColumnName("started_at");

        builder.Property(e => e.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(e => e.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(PipelineExecution.MaxErrorMessageLength);

        builder.Property(e => e.TotalSteps)
            .HasColumnName("total_steps")
            .IsRequired();

        builder.Property(e => e.CompletedSteps)
            .HasColumnName("completed_steps")
            .HasDefaultValue(0);

        builder.OwnsMany(e => e.StepExecutions, stepExec =>
        {
            stepExec.ToTable("pipeline_step_executions");

            stepExec.Property(se => se.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();
            stepExec.HasKey(se => se.Id);

            stepExec.WithOwner().HasForeignKey("execution_id");

            stepExec.Property(se => se.PipelineStepId)
                .HasColumnName("pipeline_step_id")
                .HasColumnType("uuid")
                .IsRequired();

            stepExec.Property(se => se.Order)
                .HasColumnName("order")
                .IsRequired();

            stepExec.Property(se => se.StepType)
                .HasColumnName("step_type")
                .HasMaxLength(30)
                .HasConversion(
                    type => type.Name,
                    name => StepType.FromName(name)!)
                .IsRequired();

            stepExec.Property(se => se.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .HasConversion(
                    status => status.Name,
                    name => StepExecutionStatus.FromName(name)!)
                .IsRequired();

            stepExec.Property(se => se.StartedAt)
                .HasColumnName("started_at");

            stepExec.Property(se => se.CompletedAt)
                .HasColumnName("completed_at");

            stepExec.Property(se => se.ErrorMessage)
                .HasColumnName("error_message")
                .HasMaxLength(StepExecution.MaxErrorMessageLength);

            stepExec.Property(se => se.ResultSummary)
                .HasColumnName("result_summary")
                .HasMaxLength(StepExecution.MaxResultSummaryLength);

            stepExec.Property(se => se.ResultData)
                .HasColumnName("result_data")
                .HasColumnType("jsonb");

            stepExec.HasIndex("execution_id", "Order").IsUnique();
        });

        builder.HasIndex(e => new { e.TraceId, e.Status });
    }
}
