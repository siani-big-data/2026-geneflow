using GeneFlow.ApiNet2.Domain.Identity;
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
}
