using System.Linq.Expressions;

namespace GeneFlow.ApiNet2.SharedKernel.Domain;

/// <summary>
/// Interface for the Specification pattern.
/// Specifications encapsulate query logic as reusable, composable predicates.
/// </summary>
/// <typeparam name="T">The type being evaluated by the specification.</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// The criteria expression for filtering.
    /// </summary>
    Expression<Func<T, bool>> Criteria { get; }

    /// <summary>
    /// Determines whether the specified entity satisfies this specification.
    /// </summary>
    bool IsSatisfiedBy(T entity);
}
