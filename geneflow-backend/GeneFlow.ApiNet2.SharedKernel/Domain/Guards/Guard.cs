namespace GeneFlow.ApiNet2.SharedKernel.Domain.Guards;

/// <summary>
/// Entry point for guard clauses.
/// Guards validate data integrity constraints and throw if violated.
/// </summary>
/// <example>
/// Guard.Against.Null(value, nameof(value));
/// Guard.Against.NullOrWhiteSpace(name, nameof(name));
/// Guard.Against.Negative(amount, nameof(amount));
/// </example>
public static class Guard
{
    /// <summary>
    /// Contains all guard clause methods.
    /// </summary>
    public static IGuardClause Against { get; } = new GuardClause();
}

/// <summary>
/// Interface for guard clauses. Extended via extension methods.
/// </summary>
public interface IGuardClause;

/// <summary>
/// Default implementation of guard clause.
/// </summary>
internal sealed class GuardClause : IGuardClause;
