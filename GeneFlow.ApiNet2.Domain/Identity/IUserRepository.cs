using GeneFlow.ApiNet2.Domain.Identity.Entities;
using GeneFlow.ApiNet2.Domain.Identity.Enumerations;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Domain;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

namespace GeneFlow.ApiNet2.Domain.Identity;

/// <summary>
/// Repository interface for User aggregate.
/// </summary>
public interface IUserRepository : IRepository<User, UserId>
{
    /// <summary>
    /// Gets a user by their email address.
    /// </summary>
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their email address string.
    /// </summary>
    Task<User?> GetByEmailStringAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their username.
    /// </summary>
    Task<User?> GetByUsernameAsync(Username username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by email or username identifier.
    /// </summary>
    Task<User?> GetByEmailOrUsernameAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user exists with the given email.
    /// </summary>
    Task<bool> ExistsWithEmailAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user exists with the given username.
    /// </summary>
    Task<bool> ExistsWithUsernameAsync(Username username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their refresh token.
    /// </summary>
    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their email verification token.
    /// </summary>
    Task<User?> GetByEmailVerificationTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their password reset token.
    /// </summary>
    Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple users by their IDs.
    /// </summary>
    Task<IReadOnlyList<User>> GetByIdsAsync(IEnumerable<UserId> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their external OAuth login.
    /// </summary>
    Task<User?> GetByExternalLoginAsync(ExternalProvider provider, string providerKey, CancellationToken cancellationToken = default);

    // ============================================================
    // Follow graph (social primitive)
    // ============================================================

    /// <summary>
    /// Checks whether <paramref name="followerId"/> currently follows <paramref name="followeeId"/>.
    /// </summary>
    Task<bool> IsFollowingAsync(UserId followerId, UserId followeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new follow edge. Does not check duplicates; the handler enforces invariants.
    /// </summary>
    Task AddFollowAsync(UserFollow follow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the follow edge between <paramref name="followerId"/> and <paramref name="followeeId"/>.
    /// </summary>
    Task RemoveFollowAsync(UserId followerId, UserId followeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the number of followers a user has.
    /// </summary>
    Task<int> CountFollowersAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the number of users a user is following.
    /// </summary>
    Task<int> CountFollowingAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated list of users following <paramref name="userId"/>.
    /// </summary>
    Task<PagedList<User>> GetFollowersAsync(
        UserId userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated list of users that <paramref name="userId"/> is following.
    /// </summary>
    Task<PagedList<User>> GetFollowingAsync(
        UserId userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the raw set of user ids that <paramref name="userId"/> follows.
    /// Used by the feed / search to compose a "people you follow" projection
    /// without paying the cost of materialising full user rows.
    /// </summary>
    Task<IReadOnlyList<UserId>> GetFollowingIdsAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}
