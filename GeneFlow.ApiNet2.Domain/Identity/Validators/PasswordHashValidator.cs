using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Identity.Validators;

/// <summary>
/// Validator for PasswordHash value object.
/// </summary>
public sealed class PasswordHashValidator
{
    /// <summary>
    /// Validates a password hash.
    /// </summary>
    /// <param name="rawValue">The raw hash string.</param>
    /// <returns>The hash value or validation error.</returns>
    public Result<string> Validate(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return Result.Failure<string>(UserErrors.PasswordHashRequired);

        return rawValue;
    }
}
