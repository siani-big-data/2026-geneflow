using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Entities;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Infrastructure.Pipelines.Persistence.Context;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Infrastructure.Pipelines.Persistence.Repositories;

/// <summary>
/// Repository implementation for PipelineExecution entity.
/// </summary>
public sealed class PipelineExecutionRepository : IPipelineExecutionRepository
{
    private readonly PipelineContext _context;
    private readonly ILogger<PipelineExecutionRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the PipelineExecutionRepository.
    /// </summary>
    public PipelineExecutionRepository(PipelineContext context, ILogger<PipelineExecutionRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PipelineExecution?> GetByIdAsync(PipelineExecutionId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting execution by ID: {ExecutionId}", id);
        return await _context.PipelineExecutions
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PipelineExecution?> GetByIdWithStepsAsync(PipelineExecutionId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting execution by ID with steps: {ExecutionId}", id);
        return await _context.PipelineExecutions
            .Include(e => e.StepExecutions)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(PipelineExecution execution, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding new execution: {ExecutionId}", execution.Id);
        await _context.PipelineExecutions.AddAsync(execution, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(PipelineExecution execution)
    {
        _logger.LogDebug("Updating execution: {ExecutionId}", execution.Id);
        _context.PipelineExecutions.Update(execution);
    }

    /// <inheritdoc />
    public async Task<PagedList<PipelineExecution>> GetByPipelineAsync(
        PipelineId pipelineId,
        int pageNumber,
        int pageSize,
        ExecutionStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting executions for pipeline: {PipelineId}", pipelineId);

        var query = _context.PipelineExecutions
            .Include(e => e.StepExecutions)
            .Where(e => e.PipelineId == pipelineId);

        if (status != null)
        {
            query = query.Where(e => e.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedList<PipelineExecution>.Create(items, pageNumber, pageSize, totalCount);
    }

    /// <inheritdoc />
    public async Task<PagedList<PipelineExecution>> GetByTraceAsync(
        TraceId traceId,
        int pageNumber,
        int pageSize,
        ExecutionStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting executions for trace: {TraceId}", traceId);

        var query = _context.PipelineExecutions
            .Include(e => e.StepExecutions)
            .Where(e => e.TraceId == traceId);

        if (status != null)
        {
            query = query.Where(e => e.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedList<PipelineExecution>.Create(items, pageNumber, pageSize, totalCount);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PipelineExecution>> GetRunningAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all running executions");

        return await _context.PipelineExecutions
            .Include(e => e.StepExecutions)
            .Where(e => e.Status == ExecutionStatus.Pending || e.Status == ExecutionStatus.Running)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasRunningExecutionForTraceAsync(
        TraceId traceId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking for running execution for trace: {TraceId}", traceId);

        return await _context.PipelineExecutions
            .AnyAsync(e =>
                e.TraceId == traceId &&
                (e.Status == ExecutionStatus.Pending || e.Status == ExecutionStatus.Running),
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasRunningExecutionForPipelineAsync(
        PipelineId pipelineId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking for running execution for pipeline: {PipelineId}", pipelineId);

        return await _context.PipelineExecutions
            .AnyAsync(e =>
                e.PipelineId == pipelineId &&
                (e.Status == ExecutionStatus.Pending || e.Status == ExecutionStatus.Running),
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountByPipelineAsync(
        PipelineId pipelineId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Counting executions for pipeline: {PipelineId}", pipelineId);

        return await _context.PipelineExecutions
            .CountAsync(e => e.PipelineId == pipelineId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountSuccessfulByPipelineAsync(
        PipelineId pipelineId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Counting successful executions for pipeline: {PipelineId}", pipelineId);

        return await _context.PipelineExecutions
            .CountAsync(e => e.PipelineId == pipelineId && e.Status == ExecutionStatus.Completed, cancellationToken);
    }
}
