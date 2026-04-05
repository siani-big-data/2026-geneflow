using GeneFlow.ApiNet2.Domain.Identity;

namespace GeneFlow.ApiNet2.Application.Identity.Interfaces;

/// <summary>
/// Service for generating JWT tokens.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generates a JWT access token for a user.
    /// </summary>
    /// <param name="user">The user to generate the token for.</param>
    /// <returns>The generated JWT token and its expiration time.</returns>
    (string Token, DateTime ExpiresAt) GenerateAccessToken(User user);

    /// <summary>
    /// Generates a refresh token.
    /// </summary>
    /// <returns>The generated refresh token and its expiration time.</returns>
    (string Token, DateTime ExpiresAt) GenerateRefreshToken();
}
