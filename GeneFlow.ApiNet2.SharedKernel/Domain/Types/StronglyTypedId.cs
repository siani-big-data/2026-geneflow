namespace GeneFlow.ApiNet2.SharedKernel.Domain.Types;

/// <summary>
/// Base class for strongly-typed identifiers.
/// Prevents mixing up IDs of different entity types.
/// </summary>
/// <typeparam name="TId">The strongly-typed ID type itself.</typeparam>
/// <typeparam name="TValue">The underlying value type (usually Guid, int, or string).</typeparam>
/// <example>
/// public sealed class OrderId : StronglyTypedId&lt;OrderId, Guid&gt;
/// {
///     public OrderId(Guid value) : base(value) { }
///     public static OrderId New() => new(Guid.NewGuid());
/// }
///
/// public sealed class CustomerId : StronglyTypedId&lt;CustomerId, Guid&gt;
/// {
///     public CustomerId(Guid value) : base(value) { }
/// }
///
/// // Now you can't accidentally pass a CustomerId where an OrderId is expected!
/// </example>
public abstract class StronglyTypedId<TId, TValue> : IEquatable<StronglyTypedId<TId, TValue>>
    where TId : StronglyTypedId<TId, TValue>
    where TValue : notnull
{
    /// <summary>
    /// Gets the underlying value of the strongly-typed ID.
    /// </summary>
    public TValue Value { get; }

    /// <summary>
    /// Initializes a new instance of the strongly-typed ID with the specified value.
    /// </summary>
    /// <param name="value">The underlying value.</param>
    protected StronglyTypedId(TValue value)
    {
        Value = value;
    }

    /// <inheritdoc />
    public bool Equals(StronglyTypedId<TId, TValue>? other)
    {
        if (other is null)
            return false;
        return EqualityComparer<TValue>.Default.Equals(Value, other.Value);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is StronglyTypedId<TId, TValue> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Value.GetHashCode();

    /// <inheritdoc />
    public override string ToString() => Value.ToString() ?? string.Empty;

    /// <summary>
    /// Determines whether two strongly-typed IDs are equal.
    /// </summary>
    public static bool operator ==(StronglyTypedId<TId, TValue>? left, StronglyTypedId<TId, TValue>? right)
        => Equals(left, right);

    /// <summary>
    /// Determines whether two strongly-typed IDs are not equal.
    /// </summary>
    public static bool operator !=(StronglyTypedId<TId, TValue>? left, StronglyTypedId<TId, TValue>? right)
        => !Equals(left, right);

    /// <summary>
    /// Implicitly converts a strongly-typed ID to its underlying value.
    /// </summary>
    public static implicit operator TValue(StronglyTypedId<TId, TValue> id) => id.Value;
}

/// <summary>
/// Strongly-typed ID with Guid as underlying type.
/// </summary>
/// <typeparam name="TId">The strongly-typed ID type itself.</typeparam>
public abstract class StronglyTypedGuidId<TId> : StronglyTypedId<TId, Guid>
    where TId : StronglyTypedGuidId<TId>
{
    /// <summary>
    /// Initializes a new instance with the specified Guid value.
    /// </summary>
    /// <param name="value">The Guid value.</param>
    protected StronglyTypedGuidId(Guid value) : base(value)
    {
    }

    /// <summary>
    /// Gets a value indicating whether this ID is empty (Guid.Empty).
    /// </summary>
    public bool IsEmpty => Value == Guid.Empty;
}

/// <summary>
/// Strongly-typed ID with int as underlying type.
/// </summary>
/// <typeparam name="TId">The strongly-typed ID type itself.</typeparam>
public abstract class StronglyTypedIntId<TId> : StronglyTypedId<TId, int>
    where TId : StronglyTypedIntId<TId>
{
    /// <summary>
    /// Initializes a new instance with the specified int value.
    /// </summary>
    /// <param name="value">The int value.</param>
    protected StronglyTypedIntId(int value) : base(value)
    {
    }
}

/// <summary>
/// Strongly-typed ID with long as underlying type.
/// </summary>
/// <typeparam name="TId">The strongly-typed ID type itself.</typeparam>
public abstract class StronglyTypedLongId<TId> : StronglyTypedId<TId, long>
    where TId : StronglyTypedLongId<TId>
{
    /// <summary>
    /// Initializes a new instance with the specified long value.
    /// </summary>
    /// <param name="value">The long value.</param>
    protected StronglyTypedLongId(long value) : base(value)
    {
    }
}
