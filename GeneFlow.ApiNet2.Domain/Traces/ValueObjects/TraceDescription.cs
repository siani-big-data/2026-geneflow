using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

/// <summary>
/// Represents an optional trace description.
/// </summary>
public sealed class TraceDescription : ValueObject
{
    public const int MaxLength = 500;

    public string? Value { get; }

    private TraceDescription(string? value) => Value = value;

    public static Result<TraceDescription> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new TraceDescription(null);

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            return Result.Failure<TraceDescription>(TraceErrors.DescriptionTooLong(MaxLength));

        return new TraceDescription(trimmed);
    }

    /// <summary>
    /// Creates an empty description.
    /// </summary>
    public static TraceDescription Empty => new(null);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value ?? string.Empty;

    public static implicit operator string?(TraceDescription description) => description.Value;
}
