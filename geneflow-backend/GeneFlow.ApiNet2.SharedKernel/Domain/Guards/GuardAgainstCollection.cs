using GeneFlow.ApiNet2.SharedKernel.Domain.Exceptions;

namespace GeneFlow.ApiNet2.SharedKernel.Domain.Guards;

/// <summary>
/// Guard clauses for collection validation.
/// </summary>
public static class GuardAgainstCollection
{
    /// <summary>
    /// Throws if collection is null or empty.
    /// </summary>
    /// <returns>The non-empty collection.</returns>
    public static IEnumerable<T> NullOrEmpty<T>(this IGuardClause _, IEnumerable<T>? value, string parameterName)
    {
        if (value is null)
            throw GuardException.For(parameterName, "cannot be null");

        if (!value.Any())
            throw GuardException.For(parameterName, "cannot be empty");

        return value;
    }

    /// <summary>
    /// Throws if collection is null or empty.
    /// </summary>
    /// <returns>The non-empty collection.</returns>
    public static IReadOnlyCollection<T> NullOrEmpty<T>(this IGuardClause _, IReadOnlyCollection<T>? value, string parameterName)
    {
        if (value is null)
            throw GuardException.For(parameterName, "cannot be null");

        if (value.Count == 0)
            throw GuardException.For(parameterName, "cannot be empty");

        return value;
    }

    /// <summary>
    /// Throws if collection contains null elements.
    /// </summary>
    /// <returns>The collection without nulls.</returns>
    public static IEnumerable<T> ContainsNull<T>(this IGuardClause _, IEnumerable<T?>? value, string parameterName)
        where T : class
    {
        if (value is null)
            throw GuardException.For(parameterName, "cannot be null");

        var list = value.ToList();
        if (list.Exists(x => x is null))
            throw GuardException.For(parameterName, "cannot contain null elements");

        return list!;
    }

    /// <summary>
    /// Throws if collection count exceeds maximum.
    /// </summary>
    /// <returns>The validated collection.</returns>
    public static IReadOnlyCollection<T> MaxCount<T>(this IGuardClause _, IReadOnlyCollection<T>? value, int maxCount, string parameterName)
    {
        if (value is null)
            throw GuardException.For(parameterName, "cannot be null");

        if (value.Count > maxCount)
            throw GuardException.For(parameterName, $"cannot contain more than {maxCount} elements (was {value.Count})");

        return value;
    }
}
