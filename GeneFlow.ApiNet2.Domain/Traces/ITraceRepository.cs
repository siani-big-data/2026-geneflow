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
}
