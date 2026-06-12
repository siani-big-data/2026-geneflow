namespace GeneFlow.ApiNet2.Domain.Alignments;

/// <summary>
/// Strongly-typed identifier for Alignment entities using UUID.
/// </summary>
public sealed class AlignmentId : IEquatable<AlignmentId>
{
    public Guid Value { get; }

    private AlignmentId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new AlignmentId with a new UUID.
    /// </summary>
    public static AlignmentId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates an AlignmentId from an existing UUID.
    /// </summary>
    public static AlignmentId From(Guid value) => new(value);

    /// <summary>
    /// Parses a string representation of a UUID into an AlignmentId.
    /// </summary>
    public static AlignmentId Parse(string value) => new(Guid.Parse(value));

    /// <summary>
    /// Tries to parse a string representation into an AlignmentId.
    /// </summary>
    public static bool TryParse(string? value, out AlignmentId? result)
    {
        if (Guid.TryParse(value, out var guid))
        {
            result = new AlignmentId(guid);
            return true;
        }

        result = null;
        return false;
    }

    public bool Equals(AlignmentId? other)
    {
        if (other is null)
            return false;
        return Value.Equals(other.Value);
    }

    public override bool Equals(object? obj)
    {
        return obj is AlignmentId other && Equals(other);
    }

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();

    public static bool operator ==(AlignmentId? left, AlignmentId? right)
        => Equals(left, right);

    public static bool operator !=(AlignmentId? left, AlignmentId? right)
        => !Equals(left, right);

    public static implicit operator Guid(AlignmentId id) => id.Value;

    public static implicit operator string(AlignmentId id) => id.ToString();
}
