using GeneFlow.ApiNet2.SharedKernel.Domain.Exceptions;

namespace GeneFlow.ApiNet2.SharedKernel.Domain.Guards;

/// <summary>
/// Guard clauses for null validation.
/// </summary>
public static class GuardAgainstNull
{
    /// <summary>
    /// Throws if value is null.
    /// </summary>
    /// <returns>The non-null value.</returns>
    public static T Null<T>(this IGuardClause _, T? value, string parameterName) where T : class
    {
        if (value is null)
            throw GuardException.For(parameterName, "cannot be null");

        return value;
    }

    /// <summary>
    /// Throws if nullable value type is null.
    /// </summary>
    /// <returns>The non-null value.</returns>
    public static T Null<T>(this IGuardClause _, T? value, string parameterName) where T : struct
    {
        if (!value.HasValue)
            throw GuardException.For(parameterName, "cannot be null");

        return value.Value;
    }

    /// <summary>
    /// Throws if value is null or default.
    /// </summary>
    /// <returns>The non-null, non-default value.</returns>
    public static T NullOrDefault<T>(this IGuardClause _, T? value, string parameterName) where T : class
    {
        if (value is null)
            throw GuardException.For(parameterName, "cannot be null");

        if (EqualityComparer<T>.Default.Equals(value, default!))
            throw GuardException.For(parameterName, "cannot be default value");

        return value;
    }

    /// <summary>
    /// Throws if nullable value type is default.
    /// </summary>
    /// <returns>The non-default value.</returns>
    public static T Default<T>(this IGuardClause _, T value, string parameterName) where T : struct
    {
        if (EqualityComparer<T>.Default.Equals(value, default))
            throw GuardException.For(parameterName, "cannot be default value");

        return value;
    }
}
