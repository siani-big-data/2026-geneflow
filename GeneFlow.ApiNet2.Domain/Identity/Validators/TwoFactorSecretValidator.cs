using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Identity.Validators;

/// <summary>
/// Validator for TwoFactorSecret value object.
/// </summary>
public sealed class TwoFactorSecretValidator
{
    /// <summary>
    /// Validates an encrypted TOTP secret.
    /// </summary>
    /// <param name="rawValue">The encrypted secret string.</param>
    /// <returns>The validated secret or validation error.</returns>
    public Result<string> Validate(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return Result.Failure<string>(UserErrors.TwoFactorSecretRequired);

        return rawValue;
    }
}
