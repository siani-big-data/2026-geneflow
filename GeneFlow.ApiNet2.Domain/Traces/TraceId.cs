namespace GeneFlow.ApiNet2.Domain.Traces;

/// <summary>
/// Strongly-typed identifier for Trace aggregate using UUID.
/// Unlike Studies which use sequential IDs, Traces use UUIDs to:
/// - Maintain compatibility with the existing system
/// - Avoid collisions during mass uploads
/// - Not require database sequences
/// </summary>
public sealed class TraceId : IEquatable<TraceId>
{
    public Guid Value { get; }

    private TraceId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new TraceId with a new UUID.
    /// </summary>
    public static TraceId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a TraceId from an existing UUID.
    /// </summary>
    public static TraceId From(Guid value) => new(value);

    /// <summary>
    /// Parses a string representation of a UUID into a TraceId.
    /// </summary>
    public static TraceId Parse(string value) => new(Guid.Parse(value));

    /// <summary>
    /// Tries to parse a string representation into a TraceId.
    /// </summary>
    public static bool TryParse(string? value, out TraceId? result)
    {
        if (Guid.TryParse(value, out var guid))
        {
            result = new TraceId(guid);
            return true;
        }

        result = null;
        return false;
    }

    public bool Equals(TraceId? other)
    {
        if (other is null) return false;
        return Value.Equals(other.Value);
    }

    public override bool Equals(object? obj)
    {
        return obj is TraceId other && Equals(other);
    }

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();

    public static bool operator ==(TraceId? left, TraceId? right)
        => Equals(left, right);

    public static bool operator !=(TraceId? left, TraceId? right)
        => !Equals(left, right);

    public static implicit operator Guid(TraceId id) => id.Value;

    public static implicit operator string(TraceId id) => id.ToString();
}
