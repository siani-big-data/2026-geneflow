using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles.Entities;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Domain;

namespace GeneFlow.ApiNet2.Domain.Profiles;

/// <summary>
/// Repository interface for Profile aggregate.
/// </summary>
public interface IProfileRepository : IRepository<Profile, ProfileId>
{
    /// <summary>
    /// Gets a profile by user ID.
    /// </summary>
    Task<Profile?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a profile exists for a user.
    /// </summary>
    Task<bool> ExistsForUserAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple profiles by user IDs.
    /// </summary>
    Task<IReadOnlyList<Profile>> GetByUserIdsAsync(IEnumerable<UserId> userIds, CancellationToken cancellationToken = default);

    // ============================================================
    // Pinned studies (per user)
    // ============================================================

    /// <summary>
    /// Returns the user's pinned studies, ordered by <see cref="PinnedStudy.Order"/>.
    /// </summary>
    Task<IReadOnlyList<PinnedStudy>> GetPinnedStudiesAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the user's entire pinned-studies set with the provided ordered list.
    /// Returns the previous study ids (in any order) so the handler can emit
    /// unpin events for those no longer present.
    /// </summary>
    Task<IReadOnlyList<StudyId>> ReplacePinnedStudiesAsync(
        UserId userId,
        IReadOnlyList<StudyId> orderedStudyIds,
        CancellationToken cancellationToken = default);
}
