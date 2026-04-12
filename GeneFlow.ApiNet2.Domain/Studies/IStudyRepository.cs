using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies.Entities;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

namespace GeneFlow.ApiNet2.Domain.Studies;

/// <summary>
/// Repository interface for Study aggregate.
/// </summary>
public interface IStudyRepository
{
    // CRUD
    Task<Study?> GetByIdAsync(StudyId id, CancellationToken cancellationToken = default);
    Task<Study?> GetByIdWithMembersAsync(StudyId id, CancellationToken cancellationToken = default);
    Task AddAsync(Study study, CancellationToken cancellationToken = default);
    void Update(Study study);
    void Delete(Study study);

    // User studies (where user is a member)
    Task<PagedList<Study>> GetByMemberAsync(
        UserId userId,
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        StudyStatus? status = null,
        ResearchField? researchField = null,
        CancellationToken cancellationToken = default);

    // Public (published) studies
    Task<PagedList<Study>> GetPublishedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        ResearchField? researchField = null,
        IReadOnlyList<string>? tags = null,
        string? sortBy = null,
        bool sortDescending = true,
        CancellationToken cancellationToken = default);

    // Featured studies
    Task<IReadOnlyList<Study>> GetFeaturedAsync(int limit, CancellationToken cancellationToken = default);

    // Stars
    Task<bool> IsStarredByUserAsync(StudyId studyId, UserId userId, CancellationToken cancellationToken = default);
    Task AddStarAsync(StudyId studyId, UserId userId, CancellationToken cancellationToken = default);
    Task RemoveStarAsync(StudyId studyId, UserId userId, CancellationToken cancellationToken = default);

    // Views
    Task AddViewAsync(StudyView view, CancellationToken cancellationToken = default);
    Task<bool> HasRecentViewByUserAsync(StudyId studyId, UserId userId, DateTime cutoffTime, CancellationToken cancellationToken = default);
    Task<bool> HasRecentViewByIpHashAsync(StudyId studyId, string ipHash, DateTime cutoffTime, CancellationToken cancellationToken = default);
}
