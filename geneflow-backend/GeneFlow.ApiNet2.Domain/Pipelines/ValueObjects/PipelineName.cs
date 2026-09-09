using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;

/// <summary>
/// Represents a pipeline name.
/// </summary>
public sealed class PipelineName : ValueObject
{
    public const int MinLength = 3;
    public const int MaxLength = 100;

    public string Value { get; }

    private PipelineName(string value) => Value = value;

    public static Result<PipelineName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<PipelineName>(PipelineErrors.NameRequired);

        var trimmed = value.Trim();

        if (trimmed.Length < MinLength)
            return Result.Failure<PipelineName>(PipelineErrors.NameTooShort(MinLength));

        if (trimmed.Length > MaxLength)
            return Result.Failure<PipelineName>(PipelineErrors.NameTooLong(MaxLength));

        return new PipelineName(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(PipelineName name) => name.Value;
}
