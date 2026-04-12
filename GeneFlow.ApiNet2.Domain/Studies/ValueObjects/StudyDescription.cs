using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

/// <summary>
/// Represents a study description.
/// </summary>
public sealed class StudyDescription : ValueObject
{
    public const int MaxLength = 5000;

    public string? Value { get; }

    private StudyDescription(string? value) => Value = value;

    public static Result<StudyDescription> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new StudyDescription(null);

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            return Result.Failure<StudyDescription>(StudyErrors.DescriptionTooLong(MaxLength));

        return new StudyDescription(trimmed);
    }

    public static StudyDescription Empty => new(null);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value ?? string.Empty;

    public static implicit operator string?(StudyDescription desc) => desc.Value;
}
