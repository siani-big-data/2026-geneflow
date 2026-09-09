using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;

/// <summary>
/// Represents an optional pipeline description.
/// </summary>
public sealed class PipelineDescription : ValueObject
{
    public const int MaxLength = 1000;

    public string? Value { get; }

    private PipelineDescription(string? value) => Value = value;

    public static Result<PipelineDescription> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new PipelineDescription(null);

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            return Result.Failure<PipelineDescription>(PipelineErrors.DescriptionTooLong(MaxLength));

        return new PipelineDescription(trimmed);
    }

    /// <summary>
    /// Creates an empty description.
    /// </summary>
    public static PipelineDescription Empty => new(null);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value ?? string.Empty;

    public static implicit operator string?(PipelineDescription description) => description.Value;
}
