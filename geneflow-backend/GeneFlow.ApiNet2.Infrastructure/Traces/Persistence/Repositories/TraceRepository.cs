using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Infrastructure.Traces.Persistence.Context;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Infrastructure.Traces.Persistence.Repositories;

/// <summary>
/// Repository implementation for Trace aggregate.
/// </summary>
public sealed class TraceRepository : ITraceRepository
{
    private readonly TraceContext _context;
    private readonly ILogger<TraceRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the TraceRepository.
    /// </summary>
    public TraceRepository(TraceContext context, ILogger<TraceRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Trace?> GetByIdAsync(TraceId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting trace by ID: {TraceId}", id);
        return await _context.Traces
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Trace?> GetByIdWithEditsAsync(TraceId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting trace by ID with edits: {TraceId}", id);
        return await _context.Traces
            .Include(t => t.Edits)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Trace?> GetByIdWithAnnotationsAsync(TraceId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting trace by ID with annotations: {TraceId}", id);
        return await _context.Traces
            .Include(t => t.Annotations)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Trace?> GetByIdWithAllAsync(TraceId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting trace by ID with all relations: {TraceId}", id);
        return await _context.Traces
            .Include(t => t.Edits)
            .Include(t => t.Annotations)
            .Include(t => t.Trims)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Trace?> GetByIdWithTrimsAsync(TraceId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting trace by ID with trims: {TraceId}", id);
        return await _context.Traces
            .Include(t => t.Trims)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Trace trace, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding new trace: {TraceId}", trace.Id);
        await _context.Traces.AddAsync(trace, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(Trace trace)
    {
        _logger.LogDebug("Updating trace: {TraceId}", trace.Id);
        _context.Traces.Update(trace);
    }

    /// <inheritdoc />
    public void Delete(Trace trace)
    {
        _logger.LogDebug("Deleting trace: {TraceId}", trace.Id);
        _context.Traces.Remove(trace);
    }

    /// <inheritdoc />
    public async Task<PagedList<Trace>> GetByStudyAsync(
        StudyId studyId,
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        TraceStatus? status = null,
        TraceFormat? format = null,
        string? sortBy = null,
        bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting traces for study: {StudyId}", studyId);

        var query = _context.Traces
            .Where(t => t.StudyId == studyId);

        // Filter by status if provided
        if (status is not null)
        {
            query = query.Where(t => t.Status == status);
        }

        // Filter by format if provided
        if (format is not null)
        {
            query = query.Where(t => t.Format == format);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            query = query.Where(t =>
                EF.Functions.ILike(t.Name.Value, $"%{term}%") ||
                (t.Description != null && EF.Functions.ILike(t.Description.Value, $"%{term}%")));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = sortBy?.ToLowerInvariant() switch
        {
            "name" => sortDescending
                ? query.OrderByDescending(t => t.Name.Value)
                : query.OrderBy(t => t.Name.Value),
            "status" => sortDescending
                ? query.OrderByDescending(t => t.Status)
                : query.OrderBy(t => t.Status),
            "format" => sortDescending
                ? query.OrderByDescending(t => t.Format)
                : query.OrderBy(t => t.Format),
            "quality" => sortDescending
                ? query.OrderByDescending(t => t.QualityMetrics != null ? t.QualityMetrics.AverageQualityScore : 0)
                : query.OrderBy(t => t.QualityMetrics != null ? t.QualityMetrics.AverageQualityScore : 0),
            "size" => sortDescending
                ? query.OrderByDescending(t => t.File.SizeBytes)
                : query.OrderBy(t => t.File.SizeBytes),
            _ => sortDescending
                ? query.OrderByDescending(t => t.CreatedAt)
                : query.OrderBy(t => t.CreatedAt)
        };

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedList<Trace>.Create(items, pageNumber, pageSize, totalCount);
    }

    /// <inheritdoc />
    public async Task<Dictionary<TraceStatus, int>> GetCountsByStatusAsync(
        StudyId studyId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting trace counts by status for study: {StudyId}", studyId);

        var counts = await _context.Traces
            .Where(t => t.StudyId == studyId)
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.Status, x => x.Count);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(TraceId id, CancellationToken cancellationToken = default)
    {
        return await _context.Traces
            .AnyAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Trace>> GetByIdsAsync(
        IEnumerable<TraceId> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        _logger.LogDebug("Getting traces by IDs, count: {Count}", idList.Count);

        return await _context.Traces
            .Where(t => idList.Contains(t.Id))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Trace>> GetByStudyIdAsync(
        StudyId studyId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all traces for study: {StudyId}", studyId);

        return await _context.Traces
            .Include(t => t.Annotations)
            .Where(t => t.StudyId == studyId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(int processed, int pending)> CountByUserStudiesAsync(
        IEnumerable<string> studyIds,
        CancellationToken cancellationToken = default)
    {
        var idList = studyIds.ToList();
        if (idList.Count == 0)
            return (0, 0);

        _logger.LogDebug("Counting traces for user's studies, count: {Count}", idList.Count);

        var parsedIds = idList.Select(StudyId.Parse).ToList();

        var counts = await _context.Traces
            .Where(t => parsedIds.Contains(t.StudyId))
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var processed = counts
            .Where(c => c.Status == TraceStatus.Processed)
            .Sum(c => c.Count);

        var pending = counts
            .Where(c => c.Status == TraceStatus.Uploaded ||
                        c.Status == TraceStatus.Validating ||
                        c.Status == TraceStatus.Processing)
            .Sum(c => c.Count);

        return (processed, pending);
    }

    /// <inheritdoc />
    public async Task<int> CountByStudyInCurrentMonthAsync(
        StudyId studyId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Counting traces for study {StudyId} in current month", studyId);

        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return await _context.Traces
            .Where(t => t.StudyId == studyId && t.CreatedAt >= startOfMonth)
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<StudyId?> GetStudyIdAsync(TraceId traceId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting study ID for trace: {TraceId}", traceId);

        return await _context.Traces
            .Where(t => t.Id == traceId)
            .Select(t => t.StudyId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAnnotationAsync(TraceId traceId, Guid annotationId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting annotation {AnnotationId} from trace {TraceId}", annotationId, traceId);

        // Use raw SQL to delete the annotation directly
        // This avoids EF Core owned entity tracking issues
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM traces.trace_annotations WHERE id = {annotationId} AND trace_id = {traceId.Value}",
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteTrimAsync(TraceId traceId, Guid trimId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting trim {TrimId} from trace {TraceId}", trimId, traceId);

        // Use raw SQL to delete the trim directly
        // This avoids EF Core owned entity tracking issues
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM traces.trace_trims WHERE id = {trimId} AND trace_id = {traceId.Value}",
            cancellationToken);
    }
}
