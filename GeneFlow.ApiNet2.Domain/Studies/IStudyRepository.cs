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

    // Counts for dashboard
    Task<int> CountByMemberAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<int> CountMembersInUserStudiesAsync(UserId userId, CancellationToken cancellationToken = default);

    // Methods for authorization behaviors
    /// <summary>
    /// Gets the user's role in a study, or null if the user is not a member.
    /// Used by StudyMembershipBehavior.
    /// </summary>
    Task<StudyRole?> GetMemberRoleAsync(
        StudyId studyId,
        UserId userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a study is public (Published status).
    /// Used by StudyMembershipBehavior for allowing read-only access to non-members.
    /// </summary>
    Task<bool> IsPublicStudyAsync(
        StudyId studyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the number of studies owned by a user.
    /// Used by SubscriptionLimitBehavior for MaxStudies validation.
    /// </summary>
    Task<int> CountByOwnerIdAsync(
        UserId ownerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the number of members in a study.
    /// Used by SubscriptionLimitBehavior for MaxMembersPerStudy validation.
    /// </summary>
    Task<int> CountMembersAsync(
        StudyId studyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the owner's user ID for a study.
    /// Used by SubscriptionLimitBehavior to check owner's subscription limits.
    /// </summary>
    Task<UserId?> GetOwnerIdAsync(
        StudyId studyId,
        CancellationToken cancellationToken = default);
}
