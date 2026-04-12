using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

/// <summary>
/// Represents a study title.
/// </summary>
public sealed class StudyTitle : ValueObject
{
    public const int MinLength = 5;
    public const int MaxLength = 200;

    public string Value { get; }

    private StudyTitle(string value) => Value = value;

    public static Result<StudyTitle> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<StudyTitle>(StudyErrors.TitleRequired);

        var trimmed = value.Trim();

        if (trimmed.Length < MinLength)
            return Result.Failure<StudyTitle>(StudyErrors.TitleTooShort(MinLength));

        if (trimmed.Length > MaxLength)
            return Result.Failure<StudyTitle>(StudyErrors.TitleTooLong(MaxLength));

        return new StudyTitle(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(StudyTitle title) => title.Value;
}
