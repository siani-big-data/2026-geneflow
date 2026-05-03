using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

namespace GeneFlow.ApiNet2.Domain.Traces;

/// <summary>
/// Repository interface for Trace aggregate.
/// </summary>
public interface ITraceRepository
{
    // CRUD
    Task<Trace?> GetByIdAsync(TraceId id, CancellationToken cancellationToken = default);
    Task<Trace?> GetByIdWithEditsAsync(TraceId id, CancellationToken cancellationToken = default);
    Task<Trace?> GetByIdWithAnnotationsAsync(TraceId id, CancellationToken cancellationToken = default);
    Task<Trace?> GetByIdWithAllAsync(TraceId id, CancellationToken cancellationToken = default);
    Task AddAsync(Trace trace, CancellationToken cancellationToken = default);
    void Update(Trace trace);
    void Delete(Trace trace);

    // Study traces
    Task<PagedList<Trace>> GetByStudyAsync(
        StudyId studyId,
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        TraceStatus? status = null,
        TraceFormat? format = null,
        string? sortBy = null,
        bool sortDescending = true,
        CancellationToken cancellationToken = default);

    // Counts by status
    Task<Dictionary<TraceStatus, int>> GetCountsByStatusAsync(
        StudyId studyId,
        CancellationToken cancellationToken = default);

    // Existence check
    Task<bool> ExistsAsync(TraceId id, CancellationToken cancellationToken = default);

    // Batch operations
    Task<IReadOnlyList<Trace>> GetByIdsAsync(
        IEnumerable<TraceId> ids,
        CancellationToken cancellationToken = default);

    // Get all traces for a study (with annotations for shared annotation query)
    Task<IReadOnlyList<Trace>> GetByStudyIdAsync(
        StudyId studyId,
        CancellationToken cancellationToken = default);

    // Counts for dashboard (across all studies where user is member)
    Task<(int processed, int pending)> CountByUserStudiesAsync(
        IEnumerable<string> studyIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts traces uploaded to a study in the current calendar month.
    /// Used by SubscriptionLimitBehavior for MaxTracesPerMonth validation.
    /// </summary>
    Task<int> CountByStudyInCurrentMonthAsync(
        StudyId studyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the StudyId for a trace. Used by TraceAccessBehavior for authorization.
    /// Returns null if the trace does not exist.
    /// </summary>
    Task<StudyId?> GetStudyIdAsync(TraceId traceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an annotation directly from the database.
    /// Used as a workaround for EF Core owned entity deletion issues.
    /// </summary>
    Task DeleteAnnotationAsync(TraceId traceId, Guid annotationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a trace by ID including all trim operations.
    /// </summary>
    Task<Trace?> GetByIdWithTrimsAsync(TraceId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a trim directly from the database.
    /// Used as a workaround for EF Core owned entity deletion issues.
    /// </summary>
    Task DeleteTrimAsync(TraceId traceId, Guid trimId, CancellationToken cancellationToken = default);
}
