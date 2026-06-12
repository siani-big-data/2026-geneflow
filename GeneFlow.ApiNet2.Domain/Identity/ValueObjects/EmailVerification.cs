using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Domain.Utilities;

namespace GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

/// <summary>
/// Encapsulates email verification state and behavior.
/// </summary>
public sealed class EmailVerification : ValueObject
{
    private static readonly TimeSpan TokenValidityPeriod = TimeSpan.FromHours(24);

    /// <summary>Gets whether the email has been verified.</summary>
    public bool IsVerified { get; private set; }

    /// <summary>Gets the verification token, if pending.</summary>
    public string? Token { get; private set; }

    /// <summary>Gets when the verification token expires.</summary>
    public DateTime? TokenExpiry { get; private set; }

    private EmailVerification() { }

    private EmailVerification(bool isVerified, string? token, DateTime? tokenExpiry)
    {
        IsVerified = isVerified;
        Token = token;
        TokenExpiry = tokenExpiry;
    }

    /// <summary>
    /// Creates a pending verification state with a new token.
    /// </summary>
    /// <returns>A new pending email verification.</returns>
    public static EmailVerification CreatePending()
    {
        return new EmailVerification(
            isVerified: false,
            token: TokenGenerator.GenerateUrlSafeToken(),
            tokenExpiry: DateTime.UtcNow.Add(TokenValidityPeriod));
    }

    /// <summary>
    /// Creates an already verified state.
    /// </summary>
    /// <returns>A verified email verification.</returns>
    public static EmailVerification CreateVerified()
    {
        return new EmailVerification(isVerified: true, token: null, tokenExpiry: null);
    }

    /// <summary>
    /// Verifies the email with the provided token.
    /// </summary>
    /// <param name="token">The verification token.</param>
    /// <returns>A result with the verified state or error.</returns>
    public Result<EmailVerification> Verify(string token)
    {
        if (IsVerified)
            return this;

        if (Token != token)
            return Result.Failure<EmailVerification>(UserErrors.InvalidVerificationToken);

        if (DateTime.UtcNow > TokenExpiry)
            return Result.Failure<EmailVerification>(UserErrors.InvalidVerificationToken);

        return new EmailVerification(isVerified: true, token: null, tokenExpiry: null);
    }

    /// <summary>
    /// Regenerates the verification token.
    /// </summary>
    /// <returns>A new verification state with fresh token.</returns>
    public EmailVerification RegenerateToken()
    {
        if (IsVerified)
            return this;

        return new EmailVerification(
            isVerified: false,
            token: TokenGenerator.GenerateUrlSafeToken(),
            tokenExpiry: DateTime.UtcNow.Add(TokenValidityPeriod));
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return IsVerified;
        yield return Token;
        yield return TokenExpiry;
    }
}
