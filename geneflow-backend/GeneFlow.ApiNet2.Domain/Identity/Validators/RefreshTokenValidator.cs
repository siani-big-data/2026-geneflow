using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Identity.Validators;

/// <summary>
/// Validator for RefreshToken value object.
/// </summary>
public sealed class RefreshTokenValidator
{
    /// <summary>
    /// Validates a refresh token.
    /// </summary>
    /// <param name="token">The token string.</param>
    /// <param name="expiresAt">The expiration date.</param>
    /// <returns>Success or validation error.</returns>
    public Result Validate(string? token, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Result.Failure(UserErrors.RefreshTokenRequired);

        if (expiresAt <= DateTime.UtcNow)
            return Result.Failure(UserErrors.RefreshTokenExpiryInPast);

        return Result.Success();
    }
}
