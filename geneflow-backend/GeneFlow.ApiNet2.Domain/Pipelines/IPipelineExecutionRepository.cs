using GeneFlow.ApiNet2.Domain.Pipelines.Entities;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

namespace GeneFlow.ApiNet2.Domain.Pipelines;

/// <summary>
/// Repository interface for PipelineExecution entity.
/// </summary>
public interface IPipelineExecutionRepository
{
    Task<PipelineExecution?> GetByIdAsync(PipelineExecutionId id, CancellationToken cancellationToken = default);
    Task<PipelineExecution?> GetByIdWithStepsAsync(PipelineExecutionId id, CancellationToken cancellationToken = default);
    Task AddAsync(PipelineExecution execution, CancellationToken cancellationToken = default);
    void Update(PipelineExecution execution);

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

    /// <summary>
    /// Gets the most recent pipeline executions across the given studies.
    /// Ordered by CreatedAt descending.
    /// </summary>
    Task<IReadOnlyList<PipelineExecution>> GetRecentByStudiesAsync(
        IReadOnlyCollection<StudyId> studyIds,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PipelineExecution>> GetRunningAsync(
        CancellationToken cancellationToken = default);

    Task<bool> HasRunningExecutionForTraceAsync(
        TraceId traceId,
        CancellationToken cancellationToken = default);

    Task<bool> HasRunningExecutionForPipelineAsync(
        PipelineId pipelineId,
        CancellationToken cancellationToken = default);

    Task<int> CountByPipelineAsync(
        PipelineId pipelineId,
        CancellationToken cancellationToken = default);

    Task<int> CountSuccessfulByPipelineAsync(
        PipelineId pipelineId,
        CancellationToken cancellationToken = default);
}
