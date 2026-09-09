using System.Numerics;
using GeneFlow.ApiNet2.SharedKernel.Domain.Exceptions;

namespace GeneFlow.ApiNet2.SharedKernel.Domain.Guards;

/// <summary>
/// Guard clauses for numeric validation.
/// </summary>
public static class GuardAgainstNumber
{
    /// <summary>
    /// Throws if value is negative.
    /// </summary>
    /// <returns>The non-negative value.</returns>
    public static T Negative<T>(this IGuardClause _, T value, string parameterName)
        where T : INumber<T>
    {
        if (value < T.Zero)
            throw GuardException.For(parameterName, $"cannot be negative (was {value})");

        return value;
    }

    /// <summary>
    /// Throws if value is zero.
    /// </summary>
    /// <returns>The non-zero value.</returns>
    public static T Zero<T>(this IGuardClause _, T value, string parameterName)
        where T : INumber<T>
    {
        if (value == T.Zero)
            throw GuardException.For(parameterName, "cannot be zero");

        return value;
    }

    /// <summary>
    /// Throws if value is negative or zero.
    /// </summary>
    /// <returns>The positive value.</returns>
    public static T NegativeOrZero<T>(this IGuardClause _, T value, string parameterName)
        where T : INumber<T>
    {
        if (value <= T.Zero)
            throw GuardException.For(parameterName, $"must be positive (was {value})");

        return value;
    }

    /// <summary>
    /// Throws if value is outside the specified range.
    /// </summary>
    /// <returns>The value within range.</returns>
    public static T OutOfRange<T>(this IGuardClause _, T value, T min, T max, string parameterName)
        where T : INumber<T>
    {
        if (value < min || value > max)
            throw GuardException.For(parameterName, $"must be between {min} and {max} (was {value})");

        return value;
    }

    /// <summary>
    /// Throws if value is less than minimum.
    /// </summary>
    /// <returns>The validated value.</returns>
    public static T LessThan<T>(this IGuardClause _, T value, T min, string parameterName)
        where T : INumber<T>
    {
        if (value < min)
            throw GuardException.For(parameterName, $"cannot be less than {min} (was {value})");

        return value;
    }

    /// <summary>
    /// Throws if value is greater than maximum.
    /// </summary>
    /// <returns>The validated value.</returns>
    public static T GreaterThan<T>(this IGuardClause _, T value, T max, string parameterName)
        where T : INumber<T>
    {
        if (value > max)
            throw GuardException.For(parameterName, $"cannot be greater than {max} (was {value})");

        return value;
    }
}
