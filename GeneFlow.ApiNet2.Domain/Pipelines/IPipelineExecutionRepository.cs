using GeneFlow.ApiNet2.Domain.Pipelines.Entities;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

namespace GeneFlow.ApiNet2.Domain.Pipelines;

/// <summary>
/// Repository interface for PipelineExecution entity.
/// </summary>
public interface IPipelineExecutionRepository
{
    // CRUD
    Task<PipelineExecution?> GetByIdAsync(PipelineExecutionId id, CancellationToken cancellationToken = default);
    Task<PipelineExecution?> GetByIdWithStepsAsync(PipelineExecutionId id, CancellationToken cancellationToken = default);
    Task AddAsync(PipelineExecution execution, CancellationToken cancellationToken = default);
    void Update(PipelineExecution execution);

    // Query methods
    Task<PagedList<PipelineExecution>> GetByPipelineAsync(
        PipelineId pipelineId,
        int pageNumber,
        int pageSize,
        ExecutionStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<PagedList<PipelineExecution>> GetByTraceAsync(
        TraceId traceId,
        int pageNumber,
        int pageSize,
        ExecutionStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PipelineExecution>> GetRunningAsync(
        CancellationToken cancellationToken = default);

    // Existence checks
    Task<bool> HasRunningExecutionForTraceAsync(
        TraceId traceId,
        CancellationToken cancellationToken = default);

    Task<bool> HasRunningExecutionForPipelineAsync(
        PipelineId pipelineId,
        CancellationToken cancellationToken = default);

    // Counts
    Task<int> CountByPipelineAsync(
        PipelineId pipelineId,
        CancellationToken cancellationToken = default);

    Task<int> CountSuccessfulByPipelineAsync(
        PipelineId pipelineId,
        CancellationToken cancellationToken = default);
}
