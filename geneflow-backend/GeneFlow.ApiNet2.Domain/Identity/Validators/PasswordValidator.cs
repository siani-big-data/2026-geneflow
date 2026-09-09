using System.Text.RegularExpressions;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Identity.Validators;

/// <summary>
/// Validates password complexity requirements.
/// These rules are synchronized with the frontend (app/lib/validation.ts).
/// </summary>
public sealed partial class PasswordValidator
{
    [GeneratedRegex(@"[A-Z]", RegexOptions.Compiled)]
    private static partial Regex UppercasePattern();

    [GeneratedRegex(@"[a-z]", RegexOptions.Compiled)]
    private static partial Regex LowercasePattern();

    [GeneratedRegex(@"\d", RegexOptions.Compiled)]
    private static partial Regex DigitPattern();

    /// <summary>
    /// Validates a plain text password against complexity requirements.
    /// </summary>
    /// <param name="password">The plain text password to validate.</param>
    /// <returns>The validated password or a validation error.</returns>
    public Result<string> Validate(string? password)
    {
        if (string.IsNullOrEmpty(password))
            return Result.Failure<string>(UserErrors.PasswordTooShort(Password.MinLength));

        if (password.Length < Password.MinLength)
            return Result.Failure<string>(UserErrors.PasswordTooShort(Password.MinLength));

        if (password.Length > Password.MaxLength)
            return Result.Failure<string>(UserErrors.PasswordTooLong(Password.MaxLength));

        if (!UppercasePattern().IsMatch(password))
            return Result.Failure<string>(UserErrors.PasswordRequiresUppercase);

        if (!LowercasePattern().IsMatch(password))
            return Result.Failure<string>(UserErrors.PasswordRequiresLowercase);

        if (!DigitPattern().IsMatch(password))
            return Result.Failure<string>(UserErrors.PasswordRequiresDigit);

        return password;
    }
}
