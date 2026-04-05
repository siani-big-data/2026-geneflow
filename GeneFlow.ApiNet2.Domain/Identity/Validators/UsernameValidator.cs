using System.Text.RegularExpressions;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Identity.Validators;

/// <summary>
/// Validator for Username value object.
/// </summary>
public sealed partial class UsernameValidator
{
    private static readonly Regex UsernameRegex = GeneratedUsernameRegex();

    /// <summary>
    /// Validates a username.
    /// </summary>
    /// <param name="rawValue">The raw username string.</param>
    /// <returns>The normalized username or validation error.</returns>
    public Result<string> Validate(string? rawValue)
    {
        var normalized = rawValue?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalized))
            return Result.Failure<string>(UserErrors.UsernameRequired);

        if (normalized.Length < Username.MinLength)
            return Result.Failure<string>(UserErrors.UsernameTooShort(Username.MinLength));

        if (normalized.Length > Username.MaxLength)
            return Result.Failure<string>(UserErrors.UsernameTooLong(Username.MaxLength));

        if (!UsernameRegex.IsMatch(normalized))
            return Result.Failure<string>(UserErrors.UsernameInvalidFormat);

        return normalized;
    }

    [GeneratedRegex(@"^[a-zA-Z0-9]+$", RegexOptions.Compiled)]
    private static partial Regex GeneratedUsernameRegex();
}
