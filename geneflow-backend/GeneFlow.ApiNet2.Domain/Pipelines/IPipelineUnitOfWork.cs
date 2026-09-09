namespace GeneFlow.ApiNet2.Domain.Pipelines;

/// <summary>
/// Unit of work for Pipeline aggregate.
/// </summary>
public interface IPipelineUnitOfWork
{
    IPipelineRepository Pipelines { get; }
    IPipelineExecutionRepository Executions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
