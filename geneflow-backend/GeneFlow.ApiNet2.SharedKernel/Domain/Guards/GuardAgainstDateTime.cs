using GeneFlow.ApiNet2.SharedKernel.Domain.Exceptions;

namespace GeneFlow.ApiNet2.SharedKernel.Domain.Guards;

/// <summary>
/// Guard clauses for DateTime validation.
/// </summary>
public static class GuardAgainstDateTime
{
    /// <summary>
    /// Throws if date is in the past.
    /// </summary>
    /// <returns>The future or present date.</returns>
    public static DateTime Past(this IGuardClause _, DateTime value, string parameterName)
    {
        if (value < DateTime.UtcNow)
            throw GuardException.For(parameterName, "cannot be in the past");

        return value;
    }

    /// <summary>
    /// Throws if date is in the future.
    /// </summary>
    /// <returns>The past or present date.</returns>
    public static DateTime Future(this IGuardClause _, DateTime value, string parameterName)
    {
        if (value > DateTime.UtcNow)
            throw GuardException.For(parameterName, "cannot be in the future");

        return value;
    }

    /// <summary>
    /// Throws if date is default (DateTime.MinValue).
    /// </summary>
    /// <returns>The non-default date.</returns>
    public static DateTime Default(this IGuardClause _, DateTime value, string parameterName)
    {
        if (value == default)
            throw GuardException.For(parameterName, "cannot be default DateTime");

        return value;
    }

    /// <summary>
    /// Throws if date is outside the specified range.
    /// </summary>
    /// <returns>The date within range.</returns>
    public static DateTime OutOfRange(this IGuardClause _, DateTime value, DateTime min, DateTime max, string parameterName)
    {
        if (value < min || value > max)
            throw GuardException.For(parameterName, $"must be between {min:O} and {max:O}");

        return value;
    }
}
