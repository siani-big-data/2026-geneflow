using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

/// <summary>
/// Represents a trace name.
/// </summary>
public sealed class TraceName : ValueObject
{
    public const int MinLength = 3;
    public const int MaxLength = 100;

    public string Value { get; }

    private TraceName(string value) => Value = value;

    public static Result<TraceName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<TraceName>(TraceErrors.NameRequired);

        var trimmed = value.Trim();

        if (trimmed.Length < MinLength)
            return Result.Failure<TraceName>(TraceErrors.NameTooShort(MinLength));

        if (trimmed.Length > MaxLength)
            return Result.Failure<TraceName>(TraceErrors.NameTooLong(MaxLength));

        return new TraceName(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(TraceName name) => name.Value;
}
