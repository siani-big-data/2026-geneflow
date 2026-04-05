using GeneFlow.ApiNet2.Domain.Identity.Enumerations;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Domain;

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
}
