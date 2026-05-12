using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Entities;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace GeneFlow.ApiNet2.Infrastructure.Pipelines.Persistence.Context;

/// <summary>
/// DbContext for Pipelines bounded context.
/// </summary>
public sealed class PipelineContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the PipelineContext.
    /// </summary>
    public PipelineContext(DbContextOptions<PipelineContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Pipelines DbSet.
    /// </summary>
    public DbSet<Pipeline> Pipelines => Set<Pipeline>();

    /// <summary>
    /// Gets or sets the Pipeline Executions DbSet.
    /// </summary>
    public DbSet<PipelineExecution> PipelineExecutions => Set<PipelineExecution>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("pipelines");

        modelBuilder.Ignore<PipelineStatus>();
        modelBuilder.Ignore<StepType>();
        modelBuilder.Ignore<ExecutionStatus>();
        modelBuilder.Ignore<StepExecutionStatus>();

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(PipelineContext).Assembly,
            type => type.Namespace?.Contains("Pipelines") == true);
    }
}
