using GeneFlow.ApiNet2.SharedKernel.Domain.Exceptions;

namespace GeneFlow.ApiNet2.SharedKernel.Domain.Guards;

/// <summary>
/// Guard clauses for Guid validation.
/// </summary>
public static class GuardAgainstGuid
{
    /// <summary>
    /// Throws if Guid is empty (Guid.Empty).
    /// </summary>
    /// <returns>The non-empty Guid.</returns>
    public static Guid Empty(this IGuardClause _, Guid value, string parameterName)
    {
        if (value == Guid.Empty)
            throw GuardException.For(parameterName, "cannot be empty Guid");

        return value;
    }

    /// <summary>
    /// Throws if Guid is null or empty.
    /// </summary>
    /// <returns>The non-empty Guid.</returns>
    public static Guid NullOrEmpty(this IGuardClause _, Guid? value, string parameterName)
    {
        if (!value.HasValue || value.Value == Guid.Empty)
            throw GuardException.For(parameterName, "cannot be null or empty Guid");

        return value.Value;
    }
}
