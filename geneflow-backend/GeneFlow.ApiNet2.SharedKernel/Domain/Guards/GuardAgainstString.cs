using GeneFlow.ApiNet2.SharedKernel.Domain.Exceptions;

namespace GeneFlow.ApiNet2.SharedKernel.Domain.Guards;

/// <summary>
/// Guard clauses for string validation.
/// </summary>
public static class GuardAgainstString
{
    /// <summary>
    /// Throws if string is null or empty.
    /// </summary>
    /// <returns>The non-empty string.</returns>
    public static string NullOrEmpty(this IGuardClause _, string? value, string parameterName)
    {
        if (string.IsNullOrEmpty(value))
            throw GuardException.For(parameterName, "cannot be null or empty");

        return value;
    }

    /// <summary>
    /// Throws if string is null, empty, or whitespace.
    /// </summary>
    /// <returns>The non-whitespace string.</returns>
    public static string NullOrWhiteSpace(this IGuardClause _, string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw GuardException.For(parameterName, "cannot be null, empty, or whitespace");

        return value;
    }

    /// <summary>
    /// Throws if string length exceeds maximum.
    /// </summary>
    /// <returns>The validated string.</returns>
    public static string MaxLength(this IGuardClause _, string? value, int maxLength, string parameterName)
    {
        if (value is null)
            throw GuardException.For(parameterName, "cannot be null");

        if (value.Length > maxLength)
            throw GuardException.For(parameterName, $"cannot exceed {maxLength} characters (was {value.Length})");

        return value;
    }

    /// <summary>
    /// Throws if string length is less than minimum.
    /// </summary>
    /// <returns>The validated string.</returns>
    public static string MinLength(this IGuardClause _, string? value, int minLength, string parameterName)
    {
        if (value is null)
            throw GuardException.For(parameterName, "cannot be null");

        if (value.Length < minLength)
            throw GuardException.For(parameterName, $"must be at least {minLength} characters (was {value.Length})");

        return value;
    }

    /// <summary>
    /// Throws if string length is not within range.
    /// </summary>
    /// <returns>The validated string.</returns>
    public static string LengthOutOfRange(this IGuardClause _, string? value, int minLength, int maxLength, string parameterName)
    {
        if (value is null)
            throw GuardException.For(parameterName, "cannot be null");

        if (value.Length < minLength || value.Length > maxLength)
            throw GuardException.For(parameterName, $"length must be between {minLength} and {maxLength} (was {value.Length})");

        return value;
    }
}
