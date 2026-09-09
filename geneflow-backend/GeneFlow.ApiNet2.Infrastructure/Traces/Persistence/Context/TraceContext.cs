using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace GeneFlow.ApiNet2.Infrastructure.Traces.Persistence.Context;

/// <summary>
/// DbContext for Traces bounded context.
/// </summary>
public sealed class TraceContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the TraceContext.
    /// </summary>
    public TraceContext(DbContextOptions<TraceContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Traces DbSet.
    /// </summary>
    public DbSet<Trace> Traces => Set<Trace>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("traces");

        // Ignore smart enumerations - they are stored as string, not separate entities
        modelBuilder.Ignore<TraceFormat>();
        modelBuilder.Ignore<TraceStatus>();
        modelBuilder.Ignore<EditType>();
        modelBuilder.Ignore<AnnotationType>();
        modelBuilder.Ignore<AnnotationStrand>();

        // Only apply configurations from the Traces namespace
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TraceContext).Assembly,
            type => type.Namespace?.Contains("Traces") == true);
    }
}
