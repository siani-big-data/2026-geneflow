namespace GeneFlow.ApiNet2.SharedKernel.Domain.DDD;

/// <summary>
/// Base class for value objects that wrap a single value.
/// Useful for strongly-typed identifiers and simple wrappers.
/// </summary>
/// <typeparam name="T">The type of the wrapped value.</typeparam>
public abstract class SingleValueObject<T> : ValueObject
    where T : notnull
{
    /// <summary>
    /// Gets the wrapped value.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Initializes a new instance with the specified value.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    protected SingleValueObject(T value)
    {
        Value = value;
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString() ?? string.Empty;

    /// <summary>
    /// Implicitly converts a single value object to its underlying value.
    /// </summary>
    public static implicit operator T(SingleValueObject<T> valueObject) => valueObject.Value;
}
