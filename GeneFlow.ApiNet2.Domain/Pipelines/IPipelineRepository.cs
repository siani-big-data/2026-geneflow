using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

namespace GeneFlow.ApiNet2.Domain.Pipelines;

/// <summary>
/// Repository interface for Pipeline aggregate.
/// </summary>
public interface IPipelineRepository
{
    Task<Pipeline?> GetByIdAsync(PipelineId id, CancellationToken cancellationToken = default);
    Task<Pipeline?> GetByIdWithStepsAsync(PipelineId id, CancellationToken cancellationToken = default);
    Task AddAsync(Pipeline pipeline, CancellationToken cancellationToken = default);
    void Update(Pipeline pipeline);
    void Delete(Pipeline pipeline);

    Task<PagedList<Pipeline>> GetByStudyAsync(
        StudyId studyId,
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        PipelineStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Pipeline>> GetActiveByStudyAsync(
        StudyId studyId,
        CancellationToken cancellationToken = default);

    Task<int> CountByStudyAsync(
        StudyId studyId,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveByStudyAsync(
        StudyId studyId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        PipelineId id,
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsInStudyAsync(
        StudyId studyId,
        string name,
        PipelineId? excludeId = null,
        CancellationToken cancellationToken = default);
}
