using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Infrastructure.Pipelines.Persistence.Context;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Infrastructure.Pipelines.Persistence.Repositories;

/// <summary>
/// Repository implementation for Pipeline aggregate.
/// </summary>
public sealed class PipelineRepository : IPipelineRepository
{
    private readonly PipelineContext _context;
    private readonly ILogger<PipelineRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the PipelineRepository.
    /// </summary>
    public PipelineRepository(PipelineContext context, ILogger<PipelineRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Pipeline?> GetByIdAsync(PipelineId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting pipeline by ID: {PipelineId}", id);
        return await _context.Pipelines
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Pipeline?> GetByIdWithStepsAsync(PipelineId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting pipeline by ID with steps: {PipelineId}", id);
        return await _context.Pipelines
            .Include(p => p.Steps)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Pipeline pipeline, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding new pipeline: {PipelineId}", pipeline.Id);
        await _context.Pipelines.AddAsync(pipeline, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(Pipeline pipeline)
    {
        _logger.LogDebug("Updating pipeline: {PipelineId}", pipeline.Id);
        _context.Pipelines.Update(pipeline);
    }

    /// <inheritdoc />
    public void Delete(Pipeline pipeline)
    {
        _logger.LogDebug("Deleting pipeline: {PipelineId}", pipeline.Id);
        _context.Pipelines.Remove(pipeline);
    }

    /// <inheritdoc />
    public async Task<PagedList<Pipeline>> GetByStudyAsync(
        StudyId studyId,
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        PipelineStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting pipelines for study: {StudyId}", studyId);

        var query = _context.Pipelines
            .Include(p => p.Steps)
            .Where(p => p.StudyId == studyId);

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            query = query.Where(p =>
                EF.Functions.ILike(p.Name.Value, $"%{term}%") ||
                (p.Description.Value != null && EF.Functions.ILike(p.Description.Value, $"%{term}%")));
        }

        // Apply status filter
        if (status != null)
        {
            query = query.Where(p => p.Status == status);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Get paginated items
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedList<Pipeline>.Create(items, pageNumber, pageSize, totalCount);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Pipeline>> GetActiveByStudyAsync(
        StudyId studyId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting active pipelines for study: {StudyId}", studyId);

        return await _context.Pipelines
            .Include(p => p.Steps)
            .Where(p => p.StudyId == studyId && p.Status == PipelineStatus.Active)
            .OrderBy(p => p.Name.Value)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountByStudyAsync(
        StudyId studyId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Counting pipelines for study: {StudyId}", studyId);

        return await _context.Pipelines
            .CountAsync(p => p.StudyId == studyId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountActiveByStudyAsync(
        StudyId studyId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Counting active pipelines for study: {StudyId}", studyId);

        return await _context.Pipelines
            .CountAsync(p => p.StudyId == studyId && p.Status == PipelineStatus.Active, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(
        PipelineId id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking if pipeline exists: {PipelineId}", id);

        return await _context.Pipelines
            .AnyAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> NameExistsInStudyAsync(
        StudyId studyId,
        string name,
        PipelineId? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking if pipeline name exists in study: {StudyId}, Name: {Name}", studyId, name);

        var query = _context.Pipelines
            .Where(p => p.StudyId == studyId && p.Name.Value == name);

        if (excludeId != null)
        {
            query = query.Where(p => p.Id != excludeId);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
