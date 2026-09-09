using GeneFlow.ApiNet2.SharedKernel.Domain.Exceptions;

namespace GeneFlow.ApiNet2.SharedKernel.Domain.Guards;

/// <summary>
/// Guard clauses for custom condition validation.
/// </summary>
public static class GuardAgainstCondition
{
    /// <summary>
    /// Throws if condition is true.
    /// </summary>
    public static void True(this IGuardClause _, bool condition, string parameterName, string message)
    {
        if (condition)
            throw GuardException.For(parameterName, message);
    }

    /// <summary>
    /// Throws if condition is false.
    /// </summary>
    public static void False(this IGuardClause _, bool condition, string parameterName, string message)
    {
        if (!condition)
            throw GuardException.For(parameterName, message);
    }

    /// <summary>
    /// Throws if predicate returns true for value.
    /// </summary>
    /// <returns>The validated value.</returns>
    public static T Expression<T>(this IGuardClause _, T value, Func<T, bool> predicate, string parameterName, string message)
    {
        if (predicate(value))
            throw GuardException.For(parameterName, message);

        return value;
    }
}
