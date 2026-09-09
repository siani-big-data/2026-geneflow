namespace GeneFlow.ApiNet2.Domain.Activity;

/// <summary>
/// Strongly-typed identifier for <see cref="ActivityEvent"/>.
/// Uses UUID because activity events are produced by an asynchronous projector
/// from arbitrary upstream streams: there is no central sequence and uniqueness
/// must survive across replicas of the worker.
/// </summary>
public sealed class ActivityEventId : IEquatable<ActivityEventId>
{
    public Guid Value { get; }

    private ActivityEventId(Guid value)
    {
        Value = value;
    }

    public static ActivityEventId New() => new(Guid.NewGuid());

    public static ActivityEventId From(Guid value) => new(value);

    public static ActivityEventId Parse(string value) => new(Guid.Parse(value));

    public static bool TryParse(string? value, out ActivityEventId? result)
    {
        if (Guid.TryParse(value, out var guid))
        {
            result = new ActivityEventId(guid);
            return true;
        }

        result = null;
        return false;
    }

    public bool Equals(ActivityEventId? other) => other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) => obj is ActivityEventId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();

    public static bool operator ==(ActivityEventId? left, ActivityEventId? right) => Equals(left, right);

    public static bool operator !=(ActivityEventId? left, ActivityEventId? right) => !Equals(left, right);

    public static implicit operator Guid(ActivityEventId id) => id.Value;
}
