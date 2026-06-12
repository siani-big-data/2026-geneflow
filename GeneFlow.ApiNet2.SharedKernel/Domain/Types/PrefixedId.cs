namespace GeneFlow.ApiNet2.SharedKernel.Domain.Types;

/// <summary>
/// Base class for strongly-typed IDs with a prefix and numeric sequence.
/// </summary>
/// <typeparam name="TId">The derived ID type.</typeparam>
public abstract class PrefixedId<TId> : IEquatable<TId>, IComparable<TId>
    where TId : PrefixedId<TId>
{
    /// <summary>
    /// Gets the prefix character for this ID type.
    /// </summary>
    protected abstract char Prefix { get; }

    /// <summary>
    /// Gets the number of digits for the numeric part.
    /// </summary>
    protected abstract int NumericLength { get; }

    /// <summary>
    /// Gets the numeric value of the ID.
    /// </summary>
    public long Value { get; }

    /// <summary>
    /// Initializes a new instance with the specified numeric value.
    /// </summary>
    protected PrefixedId(long value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "ID value must be non-negative.");

        var maxValue = (long)Math.Pow(10, NumericLength) - 1;
        if (value > maxValue)
            throw new ArgumentOutOfRangeException(nameof(value), $"ID value exceeds maximum ({maxValue}).");

        Value = value;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Prefix}{Value.ToString($"D{NumericLength}")}";

    /// <summary>
    /// Parses a string ID into the strongly-typed ID.
    /// </summary>
    protected static TId Parse(string id, Func<long, TId> factory)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID cannot be null or empty.", nameof(id));

        var instance = factory(0);
        var expectedLength = 1 + instance.NumericLength;

        if (id.Length != expectedLength)
            throw new FormatException($"ID must be {expectedLength} characters long.");

        if (id[0] != instance.Prefix)
            throw new FormatException($"ID must start with '{instance.Prefix}'.");

        if (!long.TryParse(id.AsSpan(1), out var value))
            throw new FormatException("ID must contain only digits after the prefix.");

        return factory(value);
    }

    /// <summary>
    /// Tries to parse a string ID into the strongly-typed ID.
    /// </summary>
    protected static bool TryParse(string? id, Func<long, TId> factory, out TId? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(id))
            return false;

        try
        {
            result = Parse(id, factory);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool Equals(TId? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Value == other.Value;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as TId);

    /// <inheritdoc />
    public override int GetHashCode() => Value.GetHashCode();

    /// <inheritdoc />
    public int CompareTo(TId? other)
    {
        if (other is null)
            return 1;
        return Value.CompareTo(other.Value);
    }

    /// <inheritdoc />
    public static bool operator ==(PrefixedId<TId>? left, PrefixedId<TId>? right)
    {
        if (left is null)
            return right is null;
        return left.Equals(right);
    }

    /// <inheritdoc />
    public static bool operator !=(PrefixedId<TId>? left, PrefixedId<TId>? right) => !(left == right);

    /// <inheritdoc />
    public static bool operator <(PrefixedId<TId>? left, PrefixedId<TId>? right)
        => left is null ? right is not null : left.CompareTo((TId?)right) < 0;

    /// <inheritdoc />
    public static bool operator >(PrefixedId<TId>? left, PrefixedId<TId>? right)
        => left is not null && left.CompareTo((TId?)right) > 0;

    /// <inheritdoc />
    public static bool operator <=(PrefixedId<TId>? left, PrefixedId<TId>? right)
        => left is null || left.CompareTo((TId?)right) <= 0;

    /// <inheritdoc />
    public static bool operator >=(PrefixedId<TId>? left, PrefixedId<TId>? right)
        => left is null ? right is null : left.CompareTo((TId?)right) >= 0;

    /// <summary>
    /// Implicit conversion to string.
    /// </summary>
    public static implicit operator string(PrefixedId<TId> id) => id.ToString();

    /// <summary>
    /// Implicit conversion to long.
    /// </summary>
    public static implicit operator long(PrefixedId<TId> id) => id.Value;
}
