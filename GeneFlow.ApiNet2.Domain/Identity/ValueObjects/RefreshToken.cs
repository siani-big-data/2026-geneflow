using GeneFlow.ApiNet2.Domain.Identity.Validators;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

/// <summary>
/// Represents a refresh token with expiration and revocation tracking.
/// </summary>
public sealed class RefreshToken : ValueObject
{
    private static readonly RefreshTokenValidator Validator = new();

    /// <summary>Gets the token string.</summary>
    public string Token { get; }

    /// <summary>Gets when the token expires.</summary>
    public DateTime ExpiresAt { get; }

    /// <summary>Gets when the token was created.</summary>
    public DateTime CreatedAt { get; }

    /// <summary>Gets whether the token has been revoked.</summary>
    public bool IsRevoked { get; }

    /// <summary>Gets when the token was revoked, if applicable.</summary>
    public DateTime? RevokedAt { get; }

    /// <summary>Gets the replacement token, if applicable.</summary>
    public string? ReplacedByToken { get; }

    private RefreshToken(
        string token,
        DateTime expiresAt,
        DateTime createdAt,
        bool isRevoked = false,
        DateTime? revokedAt = null,
        string? replacedByToken = null)
    {
        Token = token;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
        IsRevoked = isRevoked;
        RevokedAt = revokedAt;
        ReplacedByToken = replacedByToken;
    }

    /// <summary>
    /// Creates a new refresh token.
    /// </summary>
    /// <param name="token">The token string.</param>
    /// <param name="expiresAt">When the token expires.</param>
    /// <param name="createdAt">When the token was created.</param>
    /// <returns>A result containing the token or validation error.</returns>
    public static Result<RefreshToken> Create(string token, DateTime expiresAt, DateTime? createdAt = null)
    {
        return Validator.Validate(token, expiresAt)
            .Map(() => new RefreshToken(token, expiresAt, createdAt ?? DateTime.UtcNow));
    }

    internal static RefreshToken Reconstitute(
        string token,
        DateTime expiresAt,
        DateTime createdAt,
        bool isRevoked,
        DateTime? revokedAt,
        string? replacedByToken)
    {
        return new RefreshToken(token, expiresAt, createdAt, isRevoked, revokedAt, replacedByToken);
    }

    /// <summary>Gets whether the token has expired.</summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>Gets whether the token is still active (not revoked or expired).</summary>
    public bool IsActive => !IsRevoked && !IsExpired;

    /// <summary>
    /// Revokes this token.
    /// </summary>
    /// <param name="replacedByToken">Optional replacement token.</param>
    /// <returns>A new revoked token instance.</returns>
    public RefreshToken Revoke(string? replacedByToken = null)
    {
        if (IsRevoked) return this;

        return new RefreshToken(
            Token, ExpiresAt, CreatedAt,
            isRevoked: true,
            revokedAt: DateTime.UtcNow,
            replacedByToken: replacedByToken);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Token;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Token[..8]}...";
}
