using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Domain.Utilities;

namespace GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

/// <summary>
/// Encapsulates password reset state and behavior.
/// </summary>
public sealed class PasswordReset : ValueObject
{
    private static readonly TimeSpan TokenValidityPeriod = TimeSpan.FromHours(1);

    /// <summary>Gets the reset token, if pending.</summary>
    public string? Token { get; private set; }

    /// <summary>Gets when the reset token expires.</summary>
    public DateTime? TokenExpiry { get; private set; }

    private PasswordReset() { }

    private PasswordReset(string? token, DateTime? tokenExpiry)
    {
        Token = token;
        TokenExpiry = tokenExpiry;
    }

    /// <summary>Gets an empty password reset state.</summary>
    public static PasswordReset None => new(null, null);

    /// <summary>
    /// Creates a new password reset request with token.
    /// </summary>
    /// <returns>A new password reset state.</returns>
    public static PasswordReset Request()
    {
        return new PasswordReset(
            token: TokenGenerator.GenerateUrlSafeToken(),
            tokenExpiry: DateTime.UtcNow.Add(TokenValidityPeriod));
    }

    /// <summary>Gets whether a reset is pending.</summary>
    public bool IsPending => Token is not null && DateTime.UtcNow <= TokenExpiry;

    /// <summary>
    /// Validates the reset token.
    /// </summary>
    /// <param name="token">The token to validate.</param>
    /// <returns>Success or failure result.</returns>
    public Result Validate(string token)
    {
        if (Token != token)
            return Result.Failure(UserErrors.InvalidPasswordResetToken);

        if (DateTime.UtcNow > TokenExpiry)
            return Result.Failure(UserErrors.PasswordResetTokenExpired);

        return Result.Success();
    }

    /// <summary>
    /// Clears the password reset state.
    /// </summary>
    /// <returns>An empty password reset state.</returns>
    public PasswordReset Clear() => None;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Token;
        yield return TokenExpiry;
    }
}
