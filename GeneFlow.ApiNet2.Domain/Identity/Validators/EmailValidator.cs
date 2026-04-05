using System.Text.RegularExpressions;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Identity.Validators;

/// <summary>
/// Validator for Email value object.
/// </summary>
public sealed partial class EmailValidator
{
    private static readonly Regex EmailRegex = GeneratedEmailRegex();

    /// <summary>
    /// Validates an email address.
    /// </summary>
    /// <param name="rawValue">The raw email string.</param>
    /// <returns>The normalized email or validation error.</returns>
    public Result<string> Validate(string? rawValue)
    {
        var normalized = rawValue?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalized))
            return Result.Failure<string>(UserErrors.EmailRequired);

        if (normalized.Length > Email.MaxLength)
            return Result.Failure<string>(UserErrors.EmailTooLong(Email.MaxLength));

        if (!EmailRegex.IsMatch(normalized))
            return Result.Failure<string>(UserErrors.EmailInvalidFormat);

        return normalized;
    }

    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled)]
    private static partial Regex GeneratedEmailRegex();
}
