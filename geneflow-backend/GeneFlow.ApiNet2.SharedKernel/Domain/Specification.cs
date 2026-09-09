using System.Linq.Expressions;

namespace GeneFlow.ApiNet2.SharedKernel.Domain;

/// <summary>
/// Base class for implementing specifications.
/// </summary>
/// <typeparam name="T">The type being evaluated by the specification.</typeparam>
public abstract class Specification<T> : ISpecification<T>
{
    /// <inheritdoc />
    public abstract Expression<Func<T, bool>> Criteria { get; }

    /// <inheritdoc />
    public bool IsSatisfiedBy(T entity)
    {
        return Criteria.Compile()(entity);
    }

    /// <summary>
    /// Combines two specifications with AND logic.
    /// </summary>
    public static Specification<T> operator &(Specification<T> left, Specification<T> right)
    {
        return new AndSpecification<T>(left, right);
    }

    /// <summary>
    /// Combines two specifications with OR logic.
    /// </summary>
    public static Specification<T> operator |(Specification<T> left, Specification<T> right)
    {
        return new OrSpecification<T>(left, right);
    }

    /// <summary>
    /// Negates a specification.
    /// </summary>
    public static Specification<T> operator !(Specification<T> spec)
    {
        return new NotSpecification<T>(spec);
    }
}

internal sealed class AndSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    public override Expression<Func<T, bool>> Criteria
    {
        get
        {
            var param = Expression.Parameter(typeof(T), "x");
            var body = Expression.AndAlso(
                Expression.Invoke(left.Criteria, param),
                Expression.Invoke(right.Criteria, param));
            return Expression.Lambda<Func<T, bool>>(body, param);
        }
    }
}

internal sealed class OrSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    public override Expression<Func<T, bool>> Criteria
    {
        get
        {
            var param = Expression.Parameter(typeof(T), "x");
            var body = Expression.OrElse(
                Expression.Invoke(left.Criteria, param),
                Expression.Invoke(right.Criteria, param));
            return Expression.Lambda<Func<T, bool>>(body, param);
        }
    }
}

internal sealed class NotSpecification<T>(Specification<T> spec) : Specification<T>
{
    public override Expression<Func<T, bool>> Criteria
    {
        get
        {
            var param = Expression.Parameter(typeof(T), "x");
            var body = Expression.Not(Expression.Invoke(spec.Criteria, param));
            return Expression.Lambda<Func<T, bool>>(body, param);
        }
    }
}
